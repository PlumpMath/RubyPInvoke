require 'pp'
require 'erb'

# Push file names onto ARGS to populate ARGF
ARGV.push("../RubyHeaders/ruby/ruby.h")
ARGV.push("../RubyHeaders/ruby/thread.h")
ARGV.push("../RubyHeaders/ruby/intern.h")

$function_selectors_by_file = {
  "../RubyHeaders/ruby/ruby.h" => {
    "Initializing" => /(rb|ruby)[a-z_]*(init)/i,
    "Casting" => /([a-z_]2[a-z_])|rb_string_value/i,
    "Reflection" => /^rb_(class_of|type)$/,
    "Interns" => /(rb|ruby)_(intern)/i,
    "Interacting" => /(rb|ruby)_(eval|define|fun|gv|iv)/i
  },
  "../RubyHeaders/ruby/thread.h" => {
    "Threading" => /rb_.*thread|gvl/i
  },
  "../RubyHeaders/ruby/intern.h" => {
    "Arrays" => /rb_ary_.*/i,
    "Threading" => /rb_thread_create/,
    "Interacting" => /rb_(const|path2class|singleton_class$)/,
    "Strings" => /rb_str_new/
  }
}

$functions_by_category = Hash.new { |hash, key| hash[key] = [] }

def read_functions
  last_line = ""
  signature_pattern = /^(?<return>[a-z0-9_]+\s+\*?)(?<name>[a-z0-9_]+)\((?<param_list>[^;#]*)\)\s*;\s*$/im

  $function_selectors_by_file.each do |file, selectors|
    signatures = File.read(file).scan(signature_pattern)

    signatures = signatures.map { |tokens|
      tokens = tokens.map { |token| token.strip }
      return_type = tokens[0]
      name = tokens[1]
      param_list = split_parameters(tokens[2])
      csharp_return_type, csharp_param_list = convert_types(return_type, param_list)

      {
        :return_type => return_type,
        :name => name,
        :param_list => param_list,
        :csharp_return_type => csharp_return_type,
        :csharp_param_list => csharp_param_list,
        :csharp_signature => "public static extern #{csharp_return_type} #{name}(#{csharp_param_list.join(', ')})"
      }
    }

    signatures.each do |signature|
      selectors.each do |name, pattern|
        if signature[:name] =~ pattern
          $functions_by_category[name].push(signature)
          break
        end
      end
    end
  end
end

def split_parameters(param_list_string)
  param_list_string.gsub!(/\s{1,}/, " ")

  params = [""]
  paren_count = 0

  param_list_string.each_char do |c|
    if c == ',' && paren_count == 0
      params.push ""
    else
      paren_count += 1 if c.to_s == '('
      paren_count -= 1 if c.to_s == ')'
      params[params.length - 1] = params[params.length - 1].to_s + c.to_s
    end
  end

  params.map{ |param| param.strip }
end

def convert_types(return_type, param_list)
  csharp_return_type = case return_type
  when 'VALUE'
    'IntPtr'
  when 'ID'
    'IntPtr'
  when 'void *'
    'IntPtr'
  when 'SIGNED_VALUE'
    'IntPtr'
  when "LONG_LONG"
    "long"
  when "char *"
    "IntPtr"
  else
    return_type
  end

  cshap_param_list = param_list.each_with_index.map { |param, i|
    case param
    when /[a-z]+ ?\(\*\)\(ANYARGS\)/i
      "Callback callback#{i}"
    when "void *(*func)(void *)"
      "Callback callback#{i}"
    when "rb_unblock_function_t *ubf"
      "UnblockingFunction ubf"
    when "..."
      "object varargs"
    when "void"
      ""
    when "int"
      "int integer#{i}"
    when "int*"
      "ref int intRef#{i}"
    when /double( [a-z0-9_]*)?/
      "double double#{i}"
    when /long( [a-z0-9_]*)?/
      "long longInteger#{i}"
    when 'LONG_LONG'
      "long longInteger#{i}"
    when /^VALUE( [a-z0-9_]+)?$/
      "IntPtr value#{i}"
    when /ID( [a-z0-9_]*)?/
      "IntPtr id#{i}"
    when /void ?\* ?[a-z0-9_]*/
      "IntPtr voidPtr#{i}"
    when 'SIGNED_VALUE'
      "IntPtr signedValue#{i}"
    when /unsigned LONG_LONG/
      "ulong ulong#{i}"
    when /struct rb_global_variable \*[a-z0-9_]*/
      "IntPtr globalVariable#{i}"
    when 'const char*'
      "[MarshalAs(UnmanagedType.LPStr)] string string#{i}"
    when "volatile VALUE*"
      "IntPtr valuePtr"
    when 'int *argc'
      "ref int argc"
    when "char ***argv"
      "string[] argv"
    when /^(const)? ?VALUE ?\* ?[a-z0-9_]*$/
      "IntPtr value#{i}"
    else
      "UNDEFINED"
    end
  }

  return csharp_return_type, cshap_param_list
end

def print_wrapper
  puts ERB.new(File.read("#{File.dirname __FILE__}/RubyWrapper.erb"), nil, "-").result(binding)
end

read_functions
#pp $functions_by_category
print_wrapper
