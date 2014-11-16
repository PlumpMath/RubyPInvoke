using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using RubyPInvoke;
using System.Runtime.ExceptionServices;

namespace Test.RubyPInvoke
{
   [TestFixture()]
   public class TestValue
   {
      [Test]
      public void ToString_OnARubyString_ReturnsTheContentsAsAManagedString()
      {
         Ruby.Init();
         Value result = Ruby.Eval("'test'");
         var contents = result.ToString();
         Assert.AreEqual("test", contents);
      }

      [Test]
      public void ToString_OnARubyObject_ReturnsTheResultOfToSAsAManagedString()
      {
         Ruby.Init();
         Value result = Ruby.Eval(@"
            class Test
               def to_s
                 'stub'
               end
            end
            Test.new
         ");
         var contents = result.ToString();
         Assert.AreEqual("stub", contents);
      }

      [Test]
      public void Call_WithNoArguments_ExecutesTheMethod()
      {
         Ruby.Init();
         Value array = Ruby.Eval("[1,2,3]");
         Value length = array.Call("length");
         Assert.AreEqual(3, length.ToInt());
      }

      [Test]
      public void Call_WithOneArgument_ExecutesTheMethod()
      {
         Ruby.Init();
         Value array = Ruby.Eval("[1,2,3]");
         array.Call("push", 2);
         Value length = array.Call("length");
         Assert.AreEqual(4, length.ToInt());
      }

      // Maybe...?
//      [Test]
//      public void TryGetMember_CallsTheAppropriateGetterOnTheRubyObject() {
//         Ruby.Init();
//         dynamic array = Ruby.Eval("[1, 2, 3]");
//         var length = array.length;
//         Assert.AreEqual(3, length.ToInt());
//      }

      // Maybe...?
//      [Test]
//      public void TryGetMember_GivenFunctionThatDoesntExist() {
//         Ruby.Init();
//         dynamic array = Ruby.Eval("[1, 2, 3]");
//         var length = array.doesNotExist;
//      }

      // TODO
      // Reflection
      //  - methods, instance variables, class

      [Test]
      public void GetVariable_GetsTheValueOfAnInstanceVariable() {
         Ruby.Init();
         Value test = Ruby.Eval(@"
            class Test
               def initialize
                  @my_var = 'my value'
               end
            end
            Test.new
         ");
         var variable = test.GetVariable("@my_var");
         Assert.AreEqual("my value", variable.ToString());
      }

      [Test]
      public void SetVariable_SetsTheValueOfAnInstanceVariable() {
         Ruby.Init();
         Value test = Ruby.Eval(@"
            class Test
               def initialize
                  @my_var = 'my value'
               end
            end
            Test.new
         ");
         test.SetVariable("@my_var", "my new value");
         Value variable = test.GetVariable("@my_var");
         Assert.AreEqual("my new value", variable.ToString());
      }

      [Test]
      public void SingletonClass_GetsTheSingletonClassObject() {
         Ruby.Init();
         Value test = Ruby.Eval(@"
            o = Object.new
            class << o
               @singleton_class_variable = 'singleton class value'
            end
            o
         ");
         Value singletonClass = test.SingletonClass;
         Value variable = singletonClass.GetVariable("@singleton_class_variable");
         Assert.AreEqual("singleton class value", variable.ToString());
      }

      [Test]
      public void Class_GetsTheClassObject() {
         Ruby.Init();
         Value test = Ruby.Eval(@"
            class Test
               @class_variable = 'class value'
            end
            Test.new
         ");
         Value rbClass = test.Class;
         Value variable = rbClass.GetVariable("@class_variable");
         Assert.AreEqual("class value", variable.ToString());
      }

      [Test]
      public void StringCast_CreatesAValueFromAString() {
         Ruby.Init();
         Value testString = (Value)"1234567890";
         Assert.AreEqual(10, testString.Call("length"));
      }

      [Test]
      public void GetConstant_RetrievesConstantDefinedInThisValue() {
         Ruby.Init();
         Value pi = Ruby.GetConstant("Math").GetConstant("PI");
         Assert.IsTrue(pi.ToString().Contains("3.14"));
      }

      [Test]
      public void Call_ThrowsRubyException_IfRubyThrows() {
         Assert.Catch(typeof(RubyException), () => {
            ((Value)"ruby string").Call("not_a_real_method");
         });
      }
   }
}
