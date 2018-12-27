using System;

namespace RdpUploadClient
{
    internal class RDException : Exception
    {
        public RDException(string message) : base(message)
        {
        }

    }
}