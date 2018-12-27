using System;

namespace RdpUploadClient
{
    internal class RDFatalException : Exception
    {
        public RDFatalException(string message) : base(message)
        {
        }

    }
}