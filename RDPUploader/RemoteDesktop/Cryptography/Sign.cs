using System;
using System.IO;

namespace RemoteDesktop
{
    internal class Sign
    {
        internal static byte[] hash16(byte[] inData, byte[] salt1, byte[] salt2, int inPos)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(inData, inPos, 0x10);
            stream.Write(salt1, 0, 0x20);
            stream.Write(salt2, 0, 0x20);
            stream.Position = 0L;
            return MD5.ComputeHash(stream);
        }

        internal static byte[] hash48(byte[] inData, byte[] salt1, byte[] salt2, int salt)
        {
            byte[] buffer = new byte[20];
            byte[] buffer2 = new byte[4];
            byte[] destinationArray = new byte[0x30];
            int num = 0;
            for (num = 0; num < 3; num++)
            {
                for (int i = 0; i <= num; i++)
                {
                    buffer2[i] = (byte) (salt + num);
                }
                MemoryStream stream = new MemoryStream();
                stream.Write(buffer2, 0, num + 1);
                stream.Write(inData, 0, 0x30);
                stream.Write(salt1, 0, 0x20);
                stream.Write(salt2, 0, 0x20);
                stream.Position = 0L;
                buffer = SHA1.ComputeHash(stream);
                stream = new MemoryStream();
                stream.Write(inData, 0, 0x30);
                stream.Write(buffer, 0, 20);
                stream.Position = 0L;
                Array.Copy(MD5.ComputeHash(stream), 0, destinationArray, num * 0x10, 0x10);
            }
            return destinationArray;
        }

        internal static void make40bit(byte[] key)
        {
            key[0] = 0xd1;
            key[1] = 0x26;
            key[2] = 0x9e;
        }

    }
}