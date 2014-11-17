using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using RubyPInvoke;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Test.RubyPInvoke
{

   public class TestRubyWrapper
   {
      [Test]
      public unsafe void rb_protect_DoesntSuck() {
         int i = 0;
         RubyWrapper.rb_protect((ptr) => { Console.WriteLine("In test"); Thread.Sleep(TimeSpan.FromMilliseconds(500)); Console.Out.Flush(); return Ruby.Eval("nil"); }, Ruby.Nil, ref i);
      }
	}
}

