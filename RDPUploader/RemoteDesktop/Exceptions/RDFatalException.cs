using System;

namespace RemoteDesktop
{
    internal class RDFatalException : Exception
    {
        public RDFatalException(string message) : base(message)
        {
        }

    }
}