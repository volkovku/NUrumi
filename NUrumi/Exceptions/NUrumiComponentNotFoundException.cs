using System;
using System.Runtime.Serialization;

namespace NUrumi.Exceptions
{
    public class NUrumiComponentNotFoundException : NUrumiException
    {
        public NUrumiComponentNotFoundException()
        {
        }

        protected NUrumiComponentNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NUrumiComponentNotFoundException(string message) : base(message)
        {
        }

        public NUrumiComponentNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}