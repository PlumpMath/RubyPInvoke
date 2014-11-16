using System;
using System.Dynamic;
using System.Runtime.InteropServices;

namespace RubyPInvoke
{
   public class Value : DynamicObject
   {
      // The native VALUE object (which is a pointer)
      public IntPtr Pointer;

      public Value(IntPtr cPtr) {
         Pointer = cPtr;
      }

      public unsafe Value Call(string methodName, params Value[] args) {
         Value result = Ruby.Nil;
         Ruby.Protect(() => result = CallUnprotected(methodName, args));
         return result;
      }

      private unsafe Value CallUnprotected(string methodName, params Value[] args) {
         // rb_funcall expects a native array of VALUE objects, so we have to allocated this manually
         IntPtr argv = Marshal.AllocHGlobal(sizeof(uint*) * args.Length);
         uint** argvPtr = (uint**)argv;

         for (var i = 0; i < args.Length; ++i) {
            *(argvPtr + i) = (uint*)(args[i].Pointer);
         }

         IntPtr resultPtr = RubyWrapper.rb_funcall2(Pointer, RubyWrapper.rb_intern(methodName), args.Length, argv);
         Value result = new Value(resultPtr);
         // Free the native array we created
         Marshal.FreeHGlobal(argv);
         return result;
      }

      public Value GetVariable(string name) {
         return new Value(RubyWrapper.rb_iv_get(Pointer, name));
      }

      public void SetVariable(string name, Value value) {
         RubyWrapper.rb_iv_set(Pointer, name, value.Pointer);
      }

      private Value _singletonClass = null;
      public Value SingletonClass {
         get {
            if (_singletonClass == null) {
               _singletonClass = new Value(RubyWrapper.rb_singleton_class(Pointer));
            }
            return _singletonClass;
         }
      }

      private Value _rbClass = null;
      public Value Class {
         get {
            if (_rbClass == null) {
               _rbClass = Call("class");
            }
            return _rbClass;
         }
      }

      public Value GetConstant(string constName) {
         return RubyWrapper.rb_const_get(this, RubyWrapper.rb_intern(constName));
      }

      // Conversions
      // -----------

      public int ToInt() {
         return (int)RubyWrapper.rb_num2int(this.Pointer);
      }

      /// <summary>
      /// Returns to result of calling `to_s` on the ruby object, as a C# string
      /// </summary>
      /// <returns>A string that represents the current object.</returns>
      /// <filterpriority>2</filterpriority>
      public override unsafe string ToString() {
         var asString = this.Call("to_s");
         // rb_string_value_cstr expects a VALUE*, so we need a pointer to our pointer (remember, VALUE is stored as a pointer)
         fixed(void* ptrptr = &(asString.Pointer)) {
            var result = RubyWrapper.rb_string_value_cstr(new IntPtr(ptrptr));
            return Marshal.PtrToStringAnsi(result);
         }
      }

      // Casts
      // -----

      // From Value to int
      static public implicit operator int(Value self) {
         return self.ToInt();
      }

      // From int to Value 
      static public implicit operator Value(int value) {
         // TODO: If we know this is going to be an immediate,
         //       we can construct it directly without Eval'ing.
         return Ruby.Eval(value.ToString());
      }

      // From Value to string
      static public implicit operator string(Value value) {
         return value.ToString();
      }

      // From string to Value 
      static public implicit operator Value(string value) {
         return RubyWrapper.rb_str_new_cstr(value);
      }

      // From IntPtr to Value
      static public implicit operator Value(IntPtr pointer) {
         return new Value(pointer);
      }

      // From Value to IntPtr
      static public implicit operator IntPtr(Value value) {
         return value.Pointer;
      }
   }
}

