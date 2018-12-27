using System;

namespace RemoteDesktop
{    
    internal class EndOfTransmissionException : Exception
    {
        public EndOfTransmissionException(string data) : base(data)
        {
        }

    }
}