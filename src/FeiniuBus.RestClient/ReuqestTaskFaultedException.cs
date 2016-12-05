using System;

namespace FeiniuBus.RestClient
{
    public class ReuqestTaskFaultedException : Exception
    {
        public ReuqestTaskFaultedException()
        {
        }

        public ReuqestTaskFaultedException(string message) : base(message)
        {
        }

        public ReuqestTaskFaultedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
