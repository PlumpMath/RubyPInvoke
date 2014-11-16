using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using RubyPInvoke;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Test.RubyPInvoke
{
   [TestFixture()]
   public class TestRuby
   {
      [Test]
      public void Eval_GivenOne_ReturnsValueOfOne()
      {
         Ruby.Init();
         var result = Ruby.Eval("1");
         Assert.AreEqual(1, result.ToInt());
      }

      [Test]
      public void Eval_GivenTwo_ReturnsTwo()
      {
         Ruby.Init();
         var result = Ruby.Eval("2");
         Assert.AreEqual(2, result.ToInt());
      }

      [Test]
      public void WithoutGvl_RunsCallback(){
         Ruby.Init();
         bool ranCallback = false;
         Ruby.WithoutGvl(() => {ranCallback = true; });
         Assert.IsTrue(ranCallback);
      }

      [Test]
      public void Thread_RunsThread() {
         Ruby.Init();
         bool ranThread = false;

         Value thread = Ruby.Thread(() => {
            ranThread = true;
         });

         thread.Call("join");

         Assert.IsTrue(ranThread);
      }

      [Test]
      public void Thread_WithoutJoinCall_DoesntRunThread() {
         Ruby.Init();
         bool ranThread = false;

         Value thread = Ruby.Thread(() => {
            ranThread = true;
         });

         Thread.Sleep(TimeSpan.FromMilliseconds(200));

         Assert.IsFalse(ranThread);
      }

      [Test]
      public void Thread_GvlReleased_WithoutJoinCall_RunsThread() {
         Ruby.Init();
         bool ranWithoutGvlCallback = false;
         bool ranThread = false;

         Ruby.WithoutGvl(() => {
            ranWithoutGvlCallback = true;
            Value thread = Ruby.Thread(() => {
               ranThread = true;
            });

            // Have to perform the sleep before leaving the WithoutGvl
            // callback, or else we re-obtaint the GVL before
            // Ruby has a chance to schedule the other thread
            Thread.Sleep(TimeSpan.FromMilliseconds(200));
         });

         Assert.IsTrue(ranWithoutGvlCallback);
         Assert.IsTrue(ranThread);
      }

      [Test]
      public void ReleasingTheGvlTwice_DoesNotCrash() {
         Ruby.Init();

         Value array = Ruby.Eval("[1]");
         Value length = null;

         // Make sure WithoutGvl is reentrant
         Ruby.WithoutGvl(() => {
            Ruby.WithoutGvl(() => {
               length = array.Call("length");
            });
         });

         Assert.AreEqual(1, length.ToInt());
      }

      [Test]
      public void WithGvl_AcquiresGvl() {
         // TODO: How to test?
      }

      [Test]
      public void Test_ReturnsFalseOnlyForNilAndFalse() {
         Ruby.Init();
         Value nil = Ruby.Eval("nil");
         Value zero = Ruby.Eval("0");
         Value rbTrue = Ruby.Eval("true");
         Value rbFalse = Ruby.Eval("false");
         Value one = Ruby.Eval("1");
         Value rbObject = Ruby.Eval("Object.new");

         Assert.IsFalse(Ruby.Test(nil));
         Assert.IsFalse(Ruby.Test(rbFalse));
         Assert.IsTrue(Ruby.Test(rbTrue));
         Assert.IsTrue(Ruby.Test(one));
         Assert.IsTrue(Ruby.Test(zero));
         Assert.IsTrue(Ruby.Test(rbObject));
      }

      [Test]
      public void ObjectClass_RefersToRubyObjectClass() {
         Assert.AreEqual("Object", Ruby.ObjectClass.Call("name").ToString());
      }

      [Test]
      public void GetConstant_RetrievesAGlobalConstant() {
         Ruby.Init();
         Value stringClass = Ruby.GetConstant("String");
         Assert.AreEqual("String", (string)stringClass.Call("name"));
      }

      [Test]
      public void Protect_ThrowsRubyExceptionIfRubyThrows() {
         Assert.Catch(typeof(RubyException), () => {
            Ruby.Protect(() => Ruby.ObjectClass.Call("this_method_doesnt_exist"));
         });
      }

      [Test]
      public void Protect_DoesntThrowIfRubyDoesntThrow() {
         Assert.DoesNotThrow(() =>
            Ruby.Protect(() => Ruby.ObjectClass.Call("name"))
         );
      }
   }
}
