using System;
using RubyPInvoke;

public class MainClass
{
   public static void Main(string[] args)
   {
      Ruby.Init();
      int i = 0;
      RubyWrapper.rb_protect((pty) => { return new IntPtr(0); }, Ruby.Nil, ref i);
   }
}

