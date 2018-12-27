using System;

namespace RdpUploadClient
{
    internal class EndOfTransmissionException : Exception
    {
        public EndOfTransmissionException(string message) : base(message)
        {

        }
    }
}