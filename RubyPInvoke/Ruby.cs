using System;
using System.Runtime.InteropServices;

namespace RubyPInvoke
{
   public partial class Ruby
   {
      // Internal State
      // --------------

      private static bool _initialized = false;
      private static bool gvlIsReleased = false;
      private static int rbNil;

      // Ruby Constants
      // --------------

      private static Value _objectClass = null;
      public static Value ObjectClass {
         get {
            if (null == _objectClass) {
               _objectClass = Eval("Object");
            }
            return _objectClass;
         }
      }

      public static Value Nil {
         get {
            return new Value((IntPtr)rbNil);
         }
      }

      // Initialization
      // --------------

      /// <summary>
      /// Inits the Ruby Interpreter
      /// </summary>
      /// <param name="args">Arguments.</param>
      public static void Init(string[] args = null)
      {
         if (_initialized) return;

         _initialized = true;

         if (args == null) {
            args = new string[]{ };
         }


         var argCount = args.Length;
         RubyWrapper.ruby_sysinit(ref argCount, args);
         unsafe {
            RubyWrapper.ruby_init_stack(new IntPtr(&argCount));
         }
         RubyWrapper.ruby_init();
         RubyWrapper.ruby_init_loadpath();

         ExamineRuntime();
      }

      /// <summary>
      /// Determines various characteristics about the Ruby
      /// runtime dynmically. Ex: Which bit is set for nil?
      /// </summary>
      private static void ExamineRuntime() {
         rbNil = Eval("nil").Pointer.ToInt32();
      }

      // Basic Interactions
      // ------------------

      /// <summary>
      /// Returns false if value is nil or false, otherwise returns true
      /// (This is the standard boolean test in Ruby)
      /// </summary>
      /// <param name="value">Value.</param>
      public static bool Test(Value value) {
         // From the C code...
         //   #define RTEST(v) !(( (VALUE)(v) & ~Qnil) == 0)
         // Only nil & false should be consider "falsy"
         // Since nil is usually set to 4 (or some very low number)
         // we know we won't be getting pointers for any Ruby objects
         // with that exact value. Also, since integers all have the LSB
         // set (to indicate they're an immediate value, and not a pointer),
         // they will always pass this test, evaluating to true.
         return (value.Pointer.ToInt64() & ~rbNil) != 0;  
      }

      public static Value Eval(string script){
         return RubyWrapper.rb_eval_string(script);
      }

      public static unsafe void Protect(Action callback) {
         int status = 0;
         RubyWrapper.rb_protect((IntPtr ptr) => {
            callback();
            return Ruby.Nil;
         }, Ruby.Nil, ref status);

         if (status != 0) {
            var ex = new RubyException("Ruby threw within protected call. See ErrorStatus member.");
            ex.ErrorStatus = status;
            throw ex;
         }
      }

      public static Value GetConstant(string constName) {
         var constId = RubyWrapper.rb_intern(constName);
         return RubyWrapper.rb_const_get(Ruby.ObjectClass, constId);
      }

      // GVL Methods
      // -----------

      public static void WithoutGvl(Action callback, Action unblock) {
         if (gvlIsReleased) {
            callback();
            return;
         }

         gvlIsReleased = true;

         RubyWrapper.rb_thread_call_without_gvl(
            (ptr) => { callback(); return new IntPtr(0); },
            new IntPtr(0),
            (ptr) => unblock(),
            new IntPtr(0)
         );

         gvlIsReleased = false;
      }

      public static void WithoutGvl(Action callback) {
         WithoutGvl(callback, () => {});
      }

      public static void WithGvl(Action callback) {
         if (!gvlIsReleased) {
            callback();
            return;
         }

         gvlIsReleased = false;

         RubyWrapper.rb_thread_call_with_gvl(
            (ptr) => { callback(); return new IntPtr(0); },
            new IntPtr(0)
         );

         gvlIsReleased = true;
      }

      // Threading Methods
      // -----------------

      public static Value Thread(Action callback) {
         return RubyWrapper.rb_thread_create(
            (ptr) => { callback(); return (IntPtr)0; },
            (IntPtr)0
         );
      }
   }
}

