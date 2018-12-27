using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace RemoteDesktop
{
    public class NTLM
    {
        private byte[] ClientSealingKey;
        private byte[] ClientSigningKey;
        private bool m_bNTLMv2 = true;
        private byte[] m_ChallengeMsg;
        private RC4 m_ClientSealingRC4;
        private byte[] m_NegotiateMsg;
        private uint m_ReceiveSequenceNum;
        private string m_sDomain;
        private RC4 m_ServerSealingRC4;
        private uint m_SigningSequenceNum;
        private NetworkSocket m_Socket;
        private string m_sPassword;
        private string m_sUsername;
        private string m_sWorkstation;
        private const int NTLMSSP_AUTHENTICATE = 3;
        private const int NTLMSSP_CHALLENGE = 2;
        private const int NTLMSSP_NEGOTIATE = 1;
        private const uint NTLMSSP_NEGOTIATE_128 = 0x20000000;
        private const uint NTLMSSP_NEGOTIATE_56 = 0x80000000;
        private const uint NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000;
        private const uint NTLMSSP_NEGOTIATE_TARGET_INFO = 0x800000;
        private const uint NTLMSSP_NEGOTIATE_VERSION = 0x2000000;
        private byte[] ServerSealingKey;
        private byte[] ServerSigningKey;

        public NTLM(NetworkSocket socket, string sUsername, string sPassword, string sDomain)
        {
            m_Socket = socket;
            m_sUsername = sUsername;
            m_sPassword = sPassword;
            m_sDomain = sDomain;
            m_bNTLMv2 = false;

            if (string.IsNullOrWhiteSpace(m_sWorkstation))
                m_sWorkstation = RDPClient.ClientName;
        }

        private byte[] Authenticate(byte[] lmChallengeResponse, byte[] ntChallengeResponse, string sDomainName, string sUser, string sWorkstation, byte[] EncryptedRandomSessionKey, byte[] ExportedSessionKey, bool bGenerateMIC)
        {
            RdpPacket packet = new RdpPacket();
            uint flags = ((((((0xe2800000 | RDPClient.NTLMSSP_NEGOTIATE_EXTENDED_SESSION_SECURITY) | RDPClient.NTLMSSP_NEGOTIATE_ALWAYS_SIGN) | RDPClient.NTLMSSP_NEGOTIATE_NTLM) | RDPClient.NTLMSSP_NEGOTIATE_SEAL) | RDPClient.NTLMSSP_NEGOTIATE_SIGN) | RDPClient.NTLMSSP_REQUEST_TARGET) | RDPClient.NTLMSSP_NEGOTIATE_UNICODE;
            DumpFlags(flags);
            int position = (int) packet.Position;
            packet.WriteString("NTLMSSP", false);
            packet.WriteByte(0);
            packet.WriteLittleEndian32(3);
            int num3 = ((int) packet.Position) - position;
            num3 += 8;
            num3 += 8;
            num3 += 8;
            num3 += 8;
            num3 += 8;
            num3 += 8;
            num3 += 4;
            if ((flags & 0x2000000) != 0)
            {
                num3 += 8;
            }
            if (bGenerateMIC)
            {
                num3 += 0x10;
            }
            byte[] bytes = Encoding.Unicode.GetBytes(sDomainName);
            byte[] buffer = Encoding.Unicode.GetBytes(sUser);
            byte[] buffer3 = Encoding.Unicode.GetBytes(sWorkstation);
            int num4 = num3;
            int num5 = num4 + bytes.Length;
            int num6 = num5 + buffer.Length;
            int num7 = num6 + buffer3.Length;
            int num8 = num7 + lmChallengeResponse.Length;
            int num9 = num8 + ntChallengeResponse.Length;
            packet.WriteLittleEndian16((ushort) lmChallengeResponse.Length);
            packet.WriteLittleEndian16((ushort) lmChallengeResponse.Length);
            packet.WriteLittleEndian32(num7);
            num3 += lmChallengeResponse.Length;
            packet.WriteLittleEndian16((ushort) ntChallengeResponse.Length);
            packet.WriteLittleEndian16((ushort) ntChallengeResponse.Length);
            packet.WriteLittleEndian32(num8);
            num3 += ntChallengeResponse.Length;
            packet.WriteLittleEndian16((ushort) bytes.Length);
            packet.WriteLittleEndian16((ushort) bytes.Length);
            packet.WriteLittleEndian32(num4);
            num3 += bytes.Length;
            packet.WriteLittleEndian16((ushort) buffer.Length);
            packet.WriteLittleEndian16((ushort) buffer.Length);
            packet.WriteLittleEndian32(num5);
            num3 += buffer.Length;
            packet.WriteLittleEndian16((ushort) buffer3.Length);
            packet.WriteLittleEndian16((ushort) buffer3.Length);
            packet.WriteLittleEndian32(num6);
            num3 += buffer3.Length;
            packet.WriteLittleEndian16((ushort) EncryptedRandomSessionKey.Length);
            packet.WriteLittleEndian16((ushort) EncryptedRandomSessionKey.Length);
            packet.WriteLittleEndian32(num9);
            num3 += EncryptedRandomSessionKey.Length;
            packet.WriteLittleEndian32(flags);
            if ((flags & 0x2000000) != 0)
            {
                this.WriteVersion(packet);
            }
            long num10 = packet.Position;
            if (bGenerateMIC)
            {
                packet.WritePadding(0x10);
            }
            packet.Write(bytes, 0, bytes.Length);
            packet.Write(buffer, 0, buffer.Length);
            packet.Write(buffer3, 0, buffer3.Length);
            packet.Write(lmChallengeResponse, 0, lmChallengeResponse.Length);
            packet.Write(ntChallengeResponse, 0, ntChallengeResponse.Length);
            packet.Write(EncryptedRandomSessionKey, 0, EncryptedRandomSessionKey.Length);
            if (bGenerateMIC)
            {
                packet.Position = 0L;
                byte[] buffer4 = new byte[packet.Length];
                packet.Read(buffer4, 0, buffer4.Length);
                HMACT64 hmact = new HMACT64(ExportedSessionKey);
                hmact.update(this.m_NegotiateMsg);
                hmact.update(this.m_ChallengeMsg);
                hmact.update(buffer4);
                byte[] buffer5 = hmact.digest();
                packet.Position = num10;
                packet.Write(buffer5, 0, buffer5.Length);
            }
            packet.Position = 0L;
            byte[] buffer6 = new byte[packet.Length];
            packet.Read(buffer6, 0, buffer6.Length);
            return buffer6;
        }

        public static bool CompareArray(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static byte[] computeResponse(byte[] responseKey, byte[] serverChallenge, byte[] clientData, int offset, int length, out byte[] keyExchangeKey)
        {
            HMACT64 hmact = new HMACT64(responseKey);
            hmact.update(serverChallenge);
            hmact.update(clientData, offset, length);
            byte[] sourceArray = hmact.digest();
            byte[] destinationArray = new byte[sourceArray.Length + clientData.Length];
            Array.Copy(sourceArray, 0, destinationArray, 0, sourceArray.Length);
            Array.Copy(clientData, 0, destinationArray, sourceArray.Length, clientData.Length);
            hmact = new HMACT64(responseKey);
            hmact.update(sourceArray);
            keyExchangeKey = hmact.digest();
            return destinationArray;
        }

        public byte[] DecryptMessage(byte[] cryptmessage)
        {
            byte[] destinationArray = new byte[0x10];
            Array.Copy(cryptmessage, 0, destinationArray, 0, 0x10);
            byte[] message = this.m_ServerSealingRC4.crypt(cryptmessage, 0x10, cryptmessage.Length - 0x10);
            this.VerifySignature(message, destinationArray);
            return message;
        }

        private static void DumpFlags(uint flags)
        {
        }

        public byte[] EncryptMessage(byte[] message)
        {
            byte[] collection = this.m_ClientSealingRC4.crypt(message);
            List<byte> list = new List<byte>();
            list.AddRange(MakeSignature(this.m_ClientSealingRC4, this.ClientSigningKey, message, ref this.m_SigningSequenceNum));
            list.AddRange(collection);
            return list.ToArray();
        }

        private static byte[] GenerateSealKey(byte[] exportedSessionKey, byte[] constant)
        {
            List<byte> list = new List<byte>();
            list.AddRange(exportedSessionKey);
            list.AddRange(constant);
            return global::MD5.ComputeHash(list.ToArray());
        }

        private static byte[] GenerateSignKey(byte[] exportedSessionKey, byte[] constant)
        {
            List<byte> list = new List<byte>();
            list.AddRange(exportedSessionKey);
            list.AddRange(constant);
            return global::MD5.ComputeHash(list.ToArray());
        }

        private static byte[] getLMv2Response(byte[] responseKeyNT, byte[] serverChallenge, byte[] clientChallenge)
        {
            byte[] buf = new byte[0x18];
            HMACT64 hmact = new HMACT64(responseKeyNT);
            hmact.update(serverChallenge);
            hmact.update(clientChallenge);
            hmact.digest(buf, 0, 0x10);
            Array.Copy(clientChallenge, 0, buf, 0x10, 8);
            return buf;
        }

        private static byte[] getNTLMv2Response(byte[] responseKeyNT, byte[] serverChallenge, byte[] clientChallenge, byte[] nanos1601, byte[] av_pairs, out byte[] keyExchangeKey)
        {
            List<byte> list = new List<byte> { 1, 1, 0, 0, 0, 0, 0, 0 };
            list.AddRange(nanos1601);
            list.AddRange(clientChallenge);
            list.Add(0);
            list.Add(0);
            list.Add(0);
            list.Add(0);
            list.AddRange(av_pairs);
            return computeResponse(responseKeyNT, serverChallenge, list.ToArray(), 0, list.Count, out keyExchangeKey);
        }

        private void InitSignKeys(byte[] exportedSessionKey)
        {
            byte[] bytes = ASCIIEncoding.GetBytes("session key to client-to-server signing key magic constant\0");
            byte[] constant = ASCIIEncoding.GetBytes("session key to client-to-server sealing key magic constant\0");
            byte[] buffer3 = ASCIIEncoding.GetBytes("session key to server-to-client signing key magic constant\0");
            byte[] buffer4 = ASCIIEncoding.GetBytes("session key to server-to-client sealing key magic constant\0");
            this.ClientSigningKey = GenerateSignKey(exportedSessionKey, bytes);
            this.ServerSigningKey = GenerateSignKey(exportedSessionKey, buffer3);
            this.ClientSealingKey = GenerateSealKey(exportedSessionKey, constant);
            this.ServerSealingKey = GenerateSealKey(exportedSessionKey, buffer4);
            this.m_ClientSealingRC4 = new RC4();
            this.m_ClientSealingRC4.engineInitEncrypt(this.ClientSealingKey);
            this.m_ServerSealingRC4 = new RC4();
            this.m_ServerSealingRC4.engineInitDecrypt(this.ServerSealingKey);
            this.m_SigningSequenceNum = this.m_ReceiveSequenceNum = 0;
        }

        private static byte[] MakeSignature(RC4 SealKey, byte[] SignKey, byte[] message, ref uint sequenceNum)
        {
            HMACT64 hmact = new HMACT64(SignKey);
            byte[] bytes = BitConverter.GetBytes(sequenceNum++);
            hmact.update(bytes);
            hmact.update(message);
            byte[] data = hmact.digest();
            byte[] collection = SealKey.crypt(data, 0, 8);
            List<byte> list = new List<byte> { 1, 0, 0, 0 };
            list.AddRange(collection);
            list.AddRange(bytes);
            return list.ToArray();
        }

        public byte[] Negotiate()
        {
            RdpPacket packet = new RdpPacket();
            uint num = (((((((0xe2000000 | RDPClient.NTLMSSP_NEGOTIATE_EXTENDED_SESSION_SECURITY) | RDPClient.NTLMSSP_NEGOTIATE_ALWAYS_SIGN) | RDPClient.NTLMSSP_NEGOTIATE_NTLM) | RDPClient.NTLMSSP_NEGOTIATE_SEAL) | RDPClient.NTLMSSP_NEGOTIATE_SIGN) | RDPClient.NTLMSSP_REQUEST_TARGET) | RDPClient.NTLMSSP_NEGOTIATE_OEM) | RDPClient.NTLMSSP_NEGOTIATE_UNICODE;
            int position = (int) packet.Position;
            packet.WriteString("NTLMSSP", false);
            packet.WriteByte(0);
            packet.WriteLittleEndian32(1);
            packet.WriteLittleEndian32(num);
            int num3 = ((int) packet.Position) - position;
            num3 += 8;
            num3 += 8;
            if ((num & 0x2000000) != 0)
            {
                num3 += 8;
            }
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian32(0);
            if ((num & 0x2000000) != 0)
            {
                this.WriteVersion(packet);
            }
            packet.Position = 0L;
            this.m_NegotiateMsg = new byte[packet.Length];
            packet.Read(this.m_NegotiateMsg, 0, this.m_NegotiateMsg.Length);
            return this.m_NegotiateMsg;
        }

        private static byte[] nTOWFv1(string password)
        {
            if (password == null)
            {
                throw new Exception("Password parameter is required");
            }
            return MD4.ComputeHash(Encoding.Unicode.GetBytes(password));
        }

        private static byte[] nTOWFv2(string domain, string username, string password)
        {
            HMACT64 hmact = new HMACT64(nTOWFv1(password));
            hmact.update(Encoding.Unicode.GetBytes(username.ToUpper()));
            hmact.update(Encoding.Unicode.GetBytes(domain));
            return hmact.digest();
        }

        public byte[] ProcessChallenge(byte[] Challenge)
        {
            byte[] bytes;
            RdpPacket packet = new RdpPacket();
            this.m_ChallengeMsg = Challenge;
            packet.Write(Challenge, 0, Challenge.Length);
            packet.Position = 0L;
            long position = packet.Position;
            if (packet.ReadString(8) != "NTLMSSP\0")
            {
                throw new Exception("Invalid negotiation token!");
            }
            if (packet.getLittleEndian32() != 2)
            {
                throw new Exception("Expected challenge!");
            }
            int count = packet.getLittleEndian16();
            packet.getLittleEndian16();
            int num4 = packet.getLittleEndian32();
            uint flags = (uint) packet.getLittleEndian32();
            DumpFlags(flags);
            byte[] buffer = new byte[8];
            packet.Read(buffer, 0, 8);
            byte[] buffer2 = new byte[8];
            packet.Read(buffer2, 0, 8);
            int num5 = packet.getLittleEndian16();
            packet.getLittleEndian16();
            int num6 = packet.getLittleEndian32();
            if ((flags & 0x2000000) != 0)
            {
                byte[] buffer3 = new byte[8];
                packet.Read(buffer3, 0, 8);
            }
            if ((flags & 0x20000000) == 0)
            {
                throw new Exception("Strong Encryption not supported by server");
            }
            byte[] buffer4 = null;
            if (count > 0)
            {
                buffer4 = new byte[count];
                packet.Position = position + num4;
                packet.Read(buffer4, 0, count);
                Encoding.Unicode.GetString(buffer4, 0, buffer4.Length);
            }
            AV_PAIRS av_pairs = new AV_PAIRS();
            byte[] buffer5 = null;
            if (num5 <= 0)
            {
                throw new Exception("No TargetInfo!");
            }
            packet.Position = position + num6;
            buffer5 = new byte[num5];
            packet.Read(buffer5, 0, num5);
            packet = new RdpPacket();
            packet.Write(buffer5, 0, buffer5.Length);
            packet.Position = 0L;
            av_pairs.Parse(packet);
            byte[] data = nTOWFv2(this.m_sDomain, this.m_sUsername, this.m_sPassword);
            m_Socket.AddBlob(PacketLogger.PacketType.NTLM_ResponseKeyNT, data);
            byte[] blob = new byte[8];
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            provider.GetBytes(blob);
            m_Socket.AddBlob(PacketLogger.PacketType.NTLM_ClientChallenge, blob);
            byte[] buffer8 = getLMv2Response(data, buffer, blob);
            if (this.m_bNTLMv2)
            {
                Array.Clear(buffer8, 0, buffer8.Length);
            }
            bool bGenerateMIC = false;
            if ((av_pairs.Timestamp.length <= 0) || !this.m_bNTLMv2)
            {
                bytes = BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc());
            }
            else
            {
                bytes = av_pairs.Timestamp.value;
                bGenerateMIC = true;
                av_pairs.ProcessForNTLMv2();
                buffer5 = av_pairs.Serialise();
            }
            byte[] keyExchangeKey = null;
            byte[] buffer11 = getNTLMv2Response(data, buffer, blob, bytes, buffer5, out keyExchangeKey);
            m_Socket.AddBlob(PacketLogger.PacketType.NTLM_KeyExchangeKey, keyExchangeKey);
            byte[] encryptedRandomSessionKey = null;
            byte[] buffer13 = null;
            buffer13 = new byte[0x10];
            provider.GetBytes(buffer13);
            m_Socket.AddBlob(PacketLogger.PacketType.NTLM_ExportedSessionKey, buffer13);
            encryptedRandomSessionKey = new byte[0x10];
            RC4 rc = new RC4();
            rc.engineInitEncrypt(keyExchangeKey);
            encryptedRandomSessionKey = rc.crypt(buffer13);
            if ((flags & 0x40000000) == 0)
            {
                encryptedRandomSessionKey = new byte[0];
                buffer13 = keyExchangeKey;
            }
            this.InitSignKeys(buffer13);
            return this.Authenticate(buffer8, buffer11, this.m_sDomain, this.m_sUsername, this.m_sWorkstation, encryptedRandomSessionKey, buffer13, bGenerateMIC);
        }

        public void VerifySignature(byte[] message, byte[] signature)
        {
            if (!CompareArray(MakeSignature(this.m_ServerSealingRC4, this.ServerSigningKey, message, ref this.m_ReceiveSequenceNum), signature))
            {
                throw new Exception("Unable to verify received message signature!");
            }
        }

        private void WriteVersion(RdpPacket packet)
        {
            packet.WriteByte(6);
            packet.WriteByte(1);
            packet.WriteByte(0xb0);
            packet.WriteByte(0x1d);
            packet.WriteByte(0);
            packet.WriteByte(0);
            packet.WriteByte(0);
            packet.WriteByte(15);
        }

        [Flags]
        private enum AV_ID
        {
            MsvAvEOL,
            MsvAvNbComputerName,
            MsvAvNbDomainName,
            MsvAvDnsComputerName,
            MsvAvDnsDomainName,
            MsvAvDnsTreeName,
            MsvAvFlags,
            MsvAvTimestamp,
            MsvAvRestrictions,
            MsvAvTargetName,
            MsvChannelBindings
        }

        private class AV_PAIR
        {
            public int length;
            public byte[] value;
        }

        private class AV_PAIRS
        {
            public NTLM.AV_PAIR ChannelBindings = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR DnsComputerName = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR DnsDomainName = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR DnsTreeName = new NTLM.AV_PAIR();
            public int Flags;
            public NTLM.AV_PAIR NbComputerName = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR NbDomainName = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR Restrictions = new NTLM.AV_PAIR();
            public string sDnsComputerName;
            public string sDnsDomainName;
            public string sNbComputerName;
            public string sNbDomainName;
            public NTLM.AV_PAIR TargetName = new NTLM.AV_PAIR();
            public NTLM.AV_PAIR Timestamp = new NTLM.AV_PAIR();

            public void Parse(RdpPacket packet)
            {
                NTLM.AV_ID av_id;
                byte[] buffer = null;
                do
                {
                    av_id = (NTLM.AV_ID) packet.getLittleEndian16();
                    int count = packet.getLittleEndian16();
                    if (count > 0)
                    {
                        if (av_id != NTLM.AV_ID.MsvAvFlags)
                        {
                            buffer = new byte[count];
                            packet.Read(buffer, 0, count);
                        }
                        else
                        {
                            this.Flags = packet.getLittleEndian32();
                        }
                    }
                    switch (av_id)
                    {
                        case NTLM.AV_ID.MsvAvNbComputerName:
                            this.NbComputerName.length = count;
                            this.NbComputerName.value = buffer;
                            this.sNbComputerName = Encoding.Unicode.GetString(this.NbComputerName.value, 0, this.NbComputerName.value.Length);
                            break;

                        case NTLM.AV_ID.MsvAvNbDomainName:
                            this.NbDomainName.length = count;
                            this.NbDomainName.value = buffer;
                            this.sNbDomainName = Encoding.Unicode.GetString(this.NbDomainName.value, 0, this.NbDomainName.value.Length);
                            break;

                        case NTLM.AV_ID.MsvAvDnsComputerName:
                            this.DnsComputerName.length = count;
                            this.DnsComputerName.value = buffer;
                            this.sDnsComputerName = Encoding.Unicode.GetString(this.DnsComputerName.value, 0, this.DnsComputerName.value.Length);
                            break;

                        case NTLM.AV_ID.MsvAvDnsDomainName:
                            this.DnsDomainName.length = count;
                            this.DnsDomainName.value = buffer;
                            this.sDnsDomainName = Encoding.Unicode.GetString(this.DnsDomainName.value, 0, this.DnsDomainName.value.Length);
                            break;

                        case NTLM.AV_ID.MsvAvDnsTreeName:
                            this.DnsTreeName.length = count;
                            this.DnsTreeName.value = buffer;
                            break;

                        case NTLM.AV_ID.MsvAvTimestamp:
                            this.Timestamp.length = count;
                            this.Timestamp.value = buffer;
                            break;

                        case NTLM.AV_ID.MsvAvRestrictions:
                            this.Restrictions.length = count;
                            this.Restrictions.value = buffer;
                            break;

                        case NTLM.AV_ID.MsvAvTargetName:
                            this.TargetName.length = count;
                            this.TargetName.value = buffer;
                            break;

                        case NTLM.AV_ID.MsvChannelBindings:
                            this.ChannelBindings.length = count;
                            this.ChannelBindings.value = buffer;
                            break;
                    }
                }
                while (av_id != NTLM.AV_ID.MsvAvEOL);
            }

            public void ProcessForNTLMv2()
            {
                this.Flags = 2;
                this.ChannelBindings.length = 0x10;
                this.ChannelBindings.value = new byte[0x10];
                string s = "";
                byte[] bytes = Encoding.Unicode.GetBytes(s);
                this.TargetName.length = bytes.Length;
                this.TargetName.value = bytes;
                byte[] buffer = new byte[] { 
                    0x5c, 0xca, 250, 0x4d, 0x40, 0x41, 0xc5, 0x8b, 0x43, 0x93, 0x16, 0x88, 0xce, 0x3b, 0x94, 0x63, 
                    0xf1, 0xc5, 0x61, 0xf4, 0xe1, 0xde, 0xda, 0x7a, 0x43, 0xb8, 0xd6, 200, 0x9e, 80, 0x3f, 0x42
                 };
                this.Restrictions.length = 0x30;
                RdpPacket packet = new RdpPacket();
                packet.WriteLittleEndian32(0x30);
                packet.WritePadding(4);
                packet.WriteByte(1);
                packet.WritePadding(3);
                packet.WriteLittleEndian32(0x2000);
                packet.Write(buffer, 0, 0x20);
                this.Restrictions.value = packet.ToArray();
                if (this.Restrictions.value.Length != this.Restrictions.length)
                {
                    throw new Exception("Restrictions invalid!");
                }
            }

            public byte[] Serialise()
            {
                RdpPacket packet = new RdpPacket();
                if (this.NbDomainName.length > 0)
                {
                    packet.WriteLittleEndian16((short) 2);
                    packet.WriteLittleEndian16((short) this.NbDomainName.length);
                    packet.Write(this.NbDomainName.value, 0, this.NbDomainName.length);
                }
                if (this.NbComputerName.length > 0)
                {
                    packet.WriteLittleEndian16((short) 1);
                    packet.WriteLittleEndian16((short) this.NbComputerName.length);
                    packet.Write(this.NbComputerName.value, 0, this.NbComputerName.length);
                }
                if (this.DnsDomainName.length > 0)
                {
                    packet.WriteLittleEndian16((short) 4);
                    packet.WriteLittleEndian16((short) this.DnsDomainName.length);
                    packet.Write(this.DnsDomainName.value, 0, this.DnsDomainName.length);
                }
                if (this.DnsComputerName.length > 0)
                {
                    packet.WriteLittleEndian16((short) 3);
                    packet.WriteLittleEndian16((short) this.DnsComputerName.length);
                    packet.Write(this.DnsComputerName.value, 0, this.DnsComputerName.length);
                }
                if (this.DnsTreeName.length > 0)
                {
                    packet.WriteLittleEndian16((short) 5);
                    packet.WriteLittleEndian16((short) this.DnsTreeName.length);
                    packet.Write(this.DnsTreeName.value, 0, this.DnsTreeName.length);
                }
                if (this.Timestamp.length > 0)
                {
                    packet.WriteLittleEndian16((short) 7);
                    packet.WriteLittleEndian16((short) this.Timestamp.length);
                    packet.Write(this.Timestamp.value, 0, this.Timestamp.length);
                }
                if (this.Flags != 0)
                {
                    packet.WriteLittleEndian16((short) 6);
                    packet.WriteLittleEndian16((short) 4);
                    packet.WriteLittleEndian32(this.Flags);
                }
                if (this.Restrictions.length > 0)
                {
                    packet.WriteLittleEndian16((short) 8);
                    packet.WriteLittleEndian16((short) this.Restrictions.length);
                    packet.Write(this.Restrictions.value, 0, this.Restrictions.length);
                }
                if (this.ChannelBindings.length > 0)
                {
                    packet.WriteLittleEndian16((short) 10);
                    packet.WriteLittleEndian16((short) this.ChannelBindings.length);
                    packet.Write(this.ChannelBindings.value, 0, this.ChannelBindings.length);
                }
                if (this.TargetName.value != null)
                {
                    packet.WriteLittleEndian16((short) 9);
                    packet.WriteLittleEndian16((short) this.TargetName.length);
                    packet.Write(this.TargetName.value, 0, this.TargetName.length);
                }
                packet.WriteLittleEndian16((short) 0);
                packet.WriteLittleEndian16((short) 0);
                packet.WritePadding(8);
                byte[] buffer = new byte[packet.Length];
                packet.Position = 0L;
                packet.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

    }
}