using System;

namespace RdpUploadClient
{
    internal class RPCCallException : Exception
    {
        public uint CallId;
        public int ErrorCode;

        public RPCCallException(int ErrorCode, uint CallId)
        {
            this.ErrorCode = ErrorCode;
            this.CallId = CallId;
        }

    }
}