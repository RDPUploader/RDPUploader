using System;
using System.IO;
using System.Security.Cryptography;

namespace RdpUploadClient
{
    internal class SHA1
    {
        public static byte[] ComputeHash(Stream stream)
        {
            SHA1Managed managed = new SHA1Managed();
            return managed.ComputeHash(stream);
        }

    }
}