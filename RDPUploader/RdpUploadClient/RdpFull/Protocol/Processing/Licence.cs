using System;

namespace RdpUploadClient
{
    internal class Licence
    {
        private const int LICENCE_HWID_SIZE = 20;
        private const int LICENCE_SIGNATURE_SIZE = 0x10;
        private const int LICENCE_TAG_AUTHREQ = 2;
        private const int LICENCE_TAG_AUTHRESP = 0x15;
        private const int LICENCE_TAG_DEMAND = 1;
        private const int LICENCE_TAG_HOST = 0x10;
        private const int LICENCE_TAG_ISSUE = 3;
        private const int LICENCE_TAG_PRESENT = 0x12;
        private const int LICENCE_TAG_REISSUE = 4;
        private const int LICENCE_TAG_REQUEST = 0x13;
        private const int LICENCE_TAG_RESULT = 0xff;
        private const int LICENCE_TAG_USER = 15;
        private const int LICENCE_TOKEN_SIZE = 10;
        private static bool m_bLicensed = false;
        private static byte[] m_In_Sig;
        private static byte[] m_In_Token;
        private static byte[] m_Licence_Sign_Key = new byte[0x10];
        private static byte[] m_LicenceKey = new byte[0x10];
        private static byte[] m_Server_Random = new byte[0x20];
        internal const int SEC_LICENCE_NEG = 0x80;

        internal static byte[] generate_hwid()
        {
            byte[] destinationArray = new byte[20];
            destinationArray[0] = 2;
            byte[] bytes = ASCIIEncoding.GetBytes(Options.hostname, true);

            if (bytes.Length > 0x10)
            {
                Array.Copy(bytes, 0, destinationArray, 4, 0x10);
                return destinationArray;
            }

            Array.Copy(bytes, 0, destinationArray, 4, bytes.Length);

            return destinationArray;
        }

        internal static void generate_keys(byte[] client_key, byte[] server_key, byte[] client_rsa)
        {
            byte[] sourceArray = new byte[0x30];
            sourceArray = Sign.hash48(Sign.hash48(client_rsa, client_key, server_key, 0x41), server_key, client_key, 0x41);
            Array.Copy(sourceArray, 0, m_Licence_Sign_Key, 0, 0x10);
            m_LicenceKey = Sign.hash16(sourceArray, client_key, server_key, 0x10);
        }

        public static bool IsLicensePacket(RdpPacket packet)
        {
            if (m_bLicensed)
            {
                return false;
            }

            int num = packet.ReadLittleEndian32();
            packet.Position -= 4L;

            return ((num & 0x80) != 0);
        }

        internal static bool parse_authreq(RdpPacket data)
        {
            int count = 0;
            data.Position += 6L;
            count = data.ReadLittleEndian16();

            if (count != 10)
            {
                throw new RDFatalException("Illegal length of license token!");
            }

            m_In_Token = new byte[count];
            data.Read(m_In_Token, 0, count);
            m_In_Sig = new byte[0x10];
            data.Read(m_In_Sig, 0, 0x10);

            return (data.Position == data.Length);
        }

        internal static void process(RdpPacket data)
        {
            int num = 0;
            num = data.ReadByte();
            data.ReadByte();
            data.ReadLittleEndian16();

            switch (num)
            {
                case 1:
                    process_demand(data);
                    return;

                case 2:
                    process_authreq(data);
                    return;

                case 3:
                    process_issue(data);
                    m_bLicensed = true;
                    return;

                case 4:
                    m_bLicensed = true;
                    return;

                case 0xff:
                    data.ReadLittleEndian32();
                    data.ReadLittleEndian32();
                    data.ReadLittleEndian16();
                    data.ReadLittleEndian16();
                    m_bLicensed = true;
                    return;
            }
        }

        internal static void process_authreq(RdpPacket data)
        {
            byte[] destinationArray = new byte[10];
            byte[] outData = new byte[10];
            byte[] buffer3 = new byte[20];
            byte[] buffer4 = new byte[30];
            byte[] signature = new byte[0x10];
            RC4 rc = new RC4();
            byte[] buffer6 = null;

            if (!parse_authreq(data))
            {
                throw new RDFatalException("Authentication request is incorrect!");
            }

            Array.Copy(m_In_Token, 0, destinationArray, 0, 10);
            buffer6 = new byte[m_LicenceKey.Length];
            Array.Copy(m_LicenceKey, 0, buffer6, 0, m_LicenceKey.Length);
            rc.engineInitDecrypt(buffer6);
            rc.crypt(m_In_Token, 0, 10, outData, 0);
            byte[] sourceArray = generate_hwid();
            Array.Copy(outData, 0, buffer4, 0, 10);
            Array.Copy(sourceArray, 0, buffer4, 10, 20);
            signature = Secure.sign(m_Licence_Sign_Key, 0x10, 0x10, buffer4, buffer4.Length);
            Array.Copy(m_LicenceKey, 0, buffer6, 0, m_LicenceKey.Length);
            rc.engineInitEncrypt(buffer6);
            rc.crypt(sourceArray, 0, 20, buffer3, 0);

            send_authresp(destinationArray, buffer3, signature);
        }

        internal static void process_demand(RdpPacket data)
        {
            byte[] buffer = new byte[Secure.modulus_size];
            byte[] bytes = ASCIIEncoding.GetBytes(Options.hostname, true);
            byte[] username = ASCIIEncoding.GetBytes(Options.Username, true);
            data.Read(m_Server_Random, 0, m_Server_Random.Length);
            generate_keys(buffer, m_Server_Random, buffer);

            send_request(buffer, buffer, username, bytes);
        }

        internal static void process_issue(RdpPacket data)
        {
            int count = 0;
            RC4 rc = new RC4();
            byte[] destinationArray = new byte[m_LicenceKey.Length];
            Array.Copy(m_LicenceKey, 0, destinationArray, 0, m_LicenceKey.Length);
            data.ReadLittleEndian16();
            count = data.ReadLittleEndian16();

            if ((data.Position + count) <= data.Length)
            {
                rc.engineInitDecrypt(destinationArray);
                byte[] buffer = new byte[count];
                data.Read(buffer, 0, count);
                rc.crypt(buffer, 0, count, buffer, 0);
            }
        }

        public static void Reset()
        {
            m_bLicensed = false;
        }

        internal static void send_authresp(byte[] token, byte[] crypt_hwid, byte[] signature)
        {
            int num = 0x80;
            int num2 = 0x3a;
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian32(num);
            packet.WriteByte(0x15);
            packet.WriteByte(2);
            packet.WriteLittleEndian16((short) num2);
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 10);
            packet.Write(token, 0, 10);
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 20);
            packet.Write(crypt_hwid, 0, 20);
            packet.Write(signature, 0, 0x10);

            IsoLayer.SendMCS(packet, MCS.MSC_GLOBAL_CHANNEL);
        }

        internal static void send_request(byte[] client_random, byte[] rsa_data, byte[] username, byte[] host)
        {
            int num = 0x80;
            int num2 = (username.Length == 0) ? 0 : (username.Length + 1);
            int num3 = (host.Length == 0) ? 0 : (host.Length + 1);
            int num4 = (0x80 + num2) + num3;
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian32(num);
            packet.WriteByte(0x13);
            packet.WriteByte(2);
            packet.WriteLittleEndian16((short) num4);
            packet.WriteLittleEndian32(1);
            packet.WriteLittleEndianU32(0xff010000);
            packet.Write(client_random, 0, 0x20);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) (Secure.modulus_size + 8));
            packet.Write(rsa_data, 0, Secure.modulus_size);
            packet.Position += 8L;
            packet.WriteLittleEndian16((short) 15);
            packet.WriteLittleEndian16((short) num2);

            if (num2 != 0)
            {
                packet.Write(username, 0, num2 - 1);
                packet.WriteByte(0);
            }

            packet.WriteLittleEndian16((short) 0x10);
            packet.WriteLittleEndian16((short) num3);

            if (num3 != 0)
            {
                packet.Write(host, 0, num3 - 1);
                packet.WriteByte(0);
            }

            IsoLayer.SendMCS(packet, MCS.MSC_GLOBAL_CHANNEL);
        }

    }
}