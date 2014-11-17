desc "Generate the RubyWrapperVERSION.cs files"
task :wrappers do
  Dir.chdir("Scripts") do
    sh "ruby ruby_wrapper.rb 200 > ../RubyPInvoke/RubyWrapper200.cs"
    sh "ruby ruby_wrapper.rb 210 > ../RubyPInvoke/RubyWrapper210.cs"
  end
end
