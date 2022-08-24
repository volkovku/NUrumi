using System;
using System.Runtime.Serialization;

namespace NUrumi
{
    public class NUrumiException : Exception
    {
        public NUrumiException()
        {
        }

        protected NUrumiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NUrumiException(string message) : base(message)
        {
        }

        public NUrumiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}