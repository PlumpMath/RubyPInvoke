using System;

namespace RubyPInvoke
{
   public class RubyException : Exception
   {
      public int ErrorStatus { get; set;}

      public RubyException(string message, Exception innerException = null)
         :base(message, innerException)
      {}
   }
}

