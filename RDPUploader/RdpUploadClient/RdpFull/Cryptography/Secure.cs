using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RdpUploadClient
{
    internal class Secure
    {
        internal static byte[] _r = new byte[0x10];
        internal static int dec_count = 0;
        internal static int enc_count = 0;
        private static byte[] m_Client_Random = new byte[] { 
            0, 0xff, 30, 0x11, 0x4d, 0x16, 0xd4, 0x22, 0x12, 0x2d, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
         };
        internal static byte[] m_Decrypt_Key;
        internal static byte[] m_Encrypt_Key;
        private static byte[] m_Exponent;
        internal static int m_KeyLength;
        private static byte[] m_Modulus;
        private static readonly byte[] m_Pad_54 = new byte[] { 
            0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 
            0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 
            0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36, 0x36
         };
        private static readonly byte[] m_Pad_92 = new byte[] { 
            0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 
            0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 
            0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c, 0x5c
         };
        internal static RC4 m_RC4_Dec = new RC4();
        internal static RC4 m_RC4_Enc = new RC4();
        private static byte[] m_Sec_Crypted_Random = new byte[0x40];
        internal static byte[] m_Sec_Decrypt_Update_Key = new byte[0x10];
        internal static byte[] m_Sec_Sign_Key = new byte[0x10];
        private static byte[] m_Server_Public_Key;
        internal static byte[] m_Server_Random;
        internal static int modulus_size = 0x40;
        internal static int RC4_Key_Size;
        private static bool readCert = false;
        internal const int SEC_CLIENT_RANDOM = 1;
        internal const int SEC_ENCRYPT = 8;
        internal const int SEC_EXPONENT_SIZE = 4;
        internal const int SEC_LOGON_INFO = 0x40;
        internal const int SEC_PADDING_SIZE = 8;
        internal const int SEC_RANDOM_SIZE = 0x20;
        internal const int SEC_RSA_MAGIC = 0x31415352;
        internal const int SEC_TAG_KEYSIG = 8;
        internal const int SEC_TAG_PUBKEY = 6;

        public static RdpPacket DecryptPacket(RdpPacket packet)
        {
            packet.Position += 8L;
            byte[] buffer = new byte[packet.Length - packet.Position];
            packet.Read(buffer, 0, buffer.Length);

            if (dec_count == 0x1000)
            {
                m_Decrypt_Key = update(m_Decrypt_Key, m_Sec_Decrypt_Update_Key);
                byte[] destinationArray = new byte[m_KeyLength];
                Array.Copy(m_Decrypt_Key, 0, destinationArray, 0, m_KeyLength);
                m_RC4_Dec.engineInitDecrypt(destinationArray);
                dec_count = 0;
            }

            dec_count++;
            byte[] buffer3 = m_RC4_Dec.crypt(buffer);
            packet = new RdpPacket();
            packet.Write(buffer3, 0, buffer3.Length);
            packet.Position = 0L;

            return packet;
        }

        internal static RdpPacket establishKey()
        {
            int num = modulus_size + 8;
            int num2 = 1;
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian32(num2);
            packet.WriteLittleEndian32(num);
            packet.Write(m_Sec_Crypted_Random, 0, modulus_size);
            packet.WritePadding(8);

            return packet;
        }

        private static void generate_keys(int rc4_key_size)
        {
            byte[] sourceArray = new byte[0x30];
            byte[] destinationArray = new byte[0x30];
            Array.Copy(m_Client_Random, 0, destinationArray, 0, 0x18);
            Array.Copy(m_Server_Random, 0, destinationArray, 0x18, 0x18);
            sourceArray = Sign.hash48(Sign.hash48(destinationArray, m_Client_Random, m_Server_Random, 0x41), m_Client_Random, m_Server_Random, 0x58);
            Array.Copy(sourceArray, 0, m_Sec_Sign_Key, 0, 0x10);
            m_Decrypt_Key = Sign.hash16(sourceArray, m_Client_Random, m_Server_Random, Main.SecureValue2);
            m_Encrypt_Key = Sign.hash16(sourceArray, m_Client_Random, m_Server_Random, 0x20);

            if (rc4_key_size == 1)
            {
                Sign.make40bit(m_Sec_Sign_Key);
                Sign.make40bit(m_Decrypt_Key);
                Sign.make40bit(m_Encrypt_Key);
                m_KeyLength = 8;
            }
            else
            {
                m_KeyLength = 0x10;
            }

            Array.Copy(m_Decrypt_Key, 0, m_Sec_Decrypt_Update_Key, 0, Main.SecureValue2);
            Array.Copy(m_Encrypt_Key, 0, _r, 0, Main.SecureValue2);
            byte[] buffer3 = new byte[m_KeyLength];
            Array.Copy(m_Encrypt_Key, 0, buffer3, 0, m_KeyLength);
            m_RC4_Enc.engineInitEncrypt(buffer3);
            Array.Copy(m_Decrypt_Key, 0, buffer3, 0, m_KeyLength);
            m_RC4_Dec.engineInitDecrypt(buffer3);
        }

        private static void generateRandom()
        {
            new RNGCryptoServiceProvider().GetBytes(m_Client_Random);

            if (Network.Logger != null)
            {
                if (Network.Logger.Reading)
                {
                    m_Client_Random = Network.GetBlob(PacketLogger.PacketType.ClientRandom);
                }
                else
                {
                    Network.AddBlob(PacketLogger.PacketType.ClientRandom, m_Client_Random);
                }
            }
        }

        public static byte[] GetClentRandom()
        {
            if ((m_Client_Random != null) && RDPEncrypted())
            {
                return m_Client_Random;
            }

            return new byte[Main.SecureValue2];
        }
        
        private static bool parsePublicKey(RdpPacket data)
        {
            int num = 0;
            int num2 = 0;
            num = data.ReadLittleEndian32();

            if (num != 0x31415352)
            {
                int num3 = 0x31415352;
                throw new RDFatalException("Bad magic header ! Need " + num3.ToString() + " but got " + num.ToString());
            }

            num2 = data.ReadLittleEndian32();

            if (num2 != (modulus_size + 8))
            {
                modulus_size = num2 - 8;
            }

            data.Position += 8L;
            m_Exponent = new byte[4];
            data.Read(m_Exponent, 0, 4);
            m_Modulus = new byte[modulus_size];
            data.Read(m_Modulus, 0, modulus_size);

            return (data.Position <= data.Length);
        }

        internal static void processCryptInfo(RdpPacket data)
        {
            int num = 0;
            num = parseCryptInfo(data);
            RC4_Key_Size = num;

            if (num != 0)
            {
                if (readCert)
                {
                    m_Modulus = m_Server_Public_Key;
                    Array.Reverse(m_Server_Public_Key);
                    byte[] buffer = new byte[3];
                    buffer[0] = 1;
                    buffer[2] = 1;
                    m_Exponent = buffer;
                    Array.Reverse(m_Exponent);
                    RSAEncrypt(0x20);
                }
                else
                {
                    generateRandom();
                    RSAEncrypt(0x20);
                }

                generate_keys(num);
            }
        }

        internal static int parseCryptInfo(RdpPacket data)
        {
            int num2, num3, num4, num5, num6, num7, num8 = 0;
            num7 = data.ReadLittleEndian32(); // ENCRYPTION_METHOD
            num8 = data.ReadLittleEndian32(); // ENCRYPTION_LEVEL

            //System.Windows.Forms.MessageBox.Show(
            //    "Encryption Method : " + ((MCS.EncryptionMethod)num7).ToString() +
            //    "\r\nEncryption Level: " + ((MCS.EncryptionLevel)num8).ToString());

            if (num8 == 0)
            {
                if (!Network.SSLConnection)
                {
                    throw new RDFatalException("Server does not support encrypted connections!");
                }

                return 0;
            }

            int count = data.ReadLittleEndian32();
            num2 = data.ReadLittleEndian32();

            if (count != 0x20)
            {
                string[] strArray = new string[] { "Wrong size of random key size! Accepted ", count.ToString(), " but ", 0x20.ToString(), "!" };
                throw new RDFatalException(string.Concat(strArray));
            }

            m_Server_Random = new byte[count];
            data.Read(m_Server_Random, 0, count);
            num6 = ((int)data.Position) + num2;

            if (num6 > data.Length)
            {
                throw new RDFatalException("Crypt Info too short!");
            }

            if ((data.ReadLittleEndian32() & 1) != 0)
            {
                data.Position += 8L;

                while (data.Position < data.Length)
                {
                    num3 = data.ReadLittleEndian16();
                    num4 = data.ReadLittleEndian16();
                    num5 = ((int)data.Position) + num4;

                    switch (num3)
                    {
                        case 6:
                            if (parsePublicKey(data))
                            {
                                break;
                            }
                            return 0;
                    }

                    data.Position = num5;
                }

                return num7;
            }

            int num9 = data.ReadLittleEndian32();
            int num10 = 0;

            if (num9 < 2)
            {
                throw new RDFatalException("Illegal number of certificates " + num9.ToString());
            }

            while (num9 > 2)
            {
                num10 = data.ReadLittleEndian32();
                data.Position += num10;
                num9--;
            }

            byte[] buffer = new byte[data.ReadLittleEndian32()];
            data.Read(buffer, 0, buffer.Length);
            X509Certificate certificate = new X509Certificate(buffer);
            m_Server_Public_Key = new byte[Main.SecureValue1];
            Array.Copy(certificate.GetPublicKey(), 5, m_Server_Public_Key, 0, Main.SecureValue1);
            byte[] buffer2 = new byte[data.ReadLittleEndian32()];
            data.Read(buffer2, 0, buffer2.Length);
            X509Certificate certificate2 = new X509Certificate(buffer2);
            m_Server_Public_Key = new byte[Main.SecureValue1];
            Array.Copy(certificate2.GetPublicKey(), 5, m_Server_Public_Key, 0, Main.SecureValue1);
            readCert = true;

            return num7;
        }

        public static bool RDPEncrypted()
        {
            return (RC4_Key_Size > 0);
        }

        private static void RSAEncrypt(int length)
        {
            byte[] destinationArray = new byte[length];
            BigInt n = null;
            BigInt exp = null;
            BigInt integer3 = null;
            Array.Reverse(m_Exponent);
            Array.Reverse(m_Modulus);
            Array.Copy(m_Client_Random, 0, destinationArray, 0, length);
            Array.Reverse(destinationArray);

            if ((m_Modulus[0] & 0x80) != 0)
            {
                byte[] buffer2 = new byte[m_Modulus.Length + 1];
                Array.Copy(m_Modulus, 0, buffer2, 1, m_Modulus.Length);
                buffer2[0] = 0;
                n = new BigInt(buffer2);
            }
            else
            {
                n = new BigInt(m_Modulus);
            }

            if ((m_Exponent[0] & 0x80) != 0)
            {
                byte[] buffer3 = new byte[m_Exponent.Length + 1];
                Array.Copy(m_Exponent, 0, buffer3, 1, m_Exponent.Length);
                buffer3[0] = 0;
                exp = new BigInt(buffer3);
            }
            else
            {
                exp = new BigInt(m_Exponent);
            }

            if ((destinationArray[0] & 0x80) != 0)
            {
                byte[] buffer4 = new byte[destinationArray.Length + 1];
                Array.Copy(destinationArray, 0, buffer4, 1, destinationArray.Length);
                buffer4[0] = 0;
                integer3 = new BigInt(buffer4);
            }
            else
            {
                integer3 = new BigInt(destinationArray);
            }

            m_Sec_Crypted_Random = integer3.modPow(exp, n).getBytes();

            if (m_Sec_Crypted_Random.Length > modulus_size)
            {
                // sec_crypted_random too big!
            }

            Array.Reverse(m_Sec_Crypted_Random);

            if (m_Sec_Crypted_Random.Length < modulus_size)
            {
                byte[] buffer5 = new byte[modulus_size];
                Array.Copy(m_Sec_Crypted_Random, 0, buffer5, 0, m_Sec_Crypted_Random.Length);
                m_Sec_Crypted_Random = buffer5;
            }
        }

        internal static byte[] sign(byte[] session_key, int length, int keylen, byte[] data, int datalength)
        {
            byte[] buffer = new byte[20];
            byte[] bytes = new byte[4];
            byte[] destinationArray = new byte[length];
            bytes = BitConverter.GetBytes(datalength);
            MemoryStream stream = new MemoryStream();
            stream.Write(session_key, 0, keylen);
            stream.Write(m_Pad_54, 0, m_Pad_54.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(data, 0, datalength);
            stream.Position = 0L;
            buffer = SHA1.ComputeHash(stream);
            stream = new MemoryStream();
            stream.Write(session_key, 0, keylen);
            stream.Write(m_Pad_92, 0, 0x30);
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0L;
            Array.Copy(MD5.ComputeHash(stream), 0, destinationArray, 0, length);

            return destinationArray;
        }

        internal static byte[] update(byte[] key, byte[] update_key)
        {
            byte[] buffer = new byte[20];
            byte[] destinationArray = new byte[m_KeyLength];
            byte[] sourceArray = new byte[key.Length];
            MemoryStream stream = new MemoryStream();
            stream.Write(update_key, 0, m_KeyLength);
            stream.Write(m_Pad_54, 0, 40);
            stream.Write(key, 0, m_KeyLength);
            stream.Position = 0L;
            buffer = SHA1.ComputeHash(stream);
            stream = new MemoryStream();
            stream.Write(update_key, 0, m_KeyLength);
            stream.Write(m_Pad_92, 0, 0x30);
            stream.Write(buffer, 0, 20);
            stream.Position = 0L;
            sourceArray = MD5.ComputeHash(stream);
            Array.Copy(sourceArray, 0, destinationArray, 0, m_KeyLength);
            RC4 rc = new RC4();
            rc.engineInitDecrypt(destinationArray);
            sourceArray = rc.crypt(sourceArray, 0, m_KeyLength);
            if (m_KeyLength == 8)
            {
                Sign.make40bit(sourceArray);
            }
            return sourceArray;
        }

    }
}