using System;

namespace APIKSA.Models
{
    public class SystemException : Exception
    {
        public int ErrorSource { get; }

        public SystemException(string message, int errorSource, Exception innerException)
            : base(message, innerException)
        {
            ErrorSource = errorSource;
        }

    }
}