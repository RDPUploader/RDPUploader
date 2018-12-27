using System;
using System.Threading;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class ISO
    {
        private static int lastchannel = 0;
        private static RdpPacket m_Packet;
        internal static int next_packet;

        public static void Disconnect()
        {
            m_Packet = null;
            next_packet = 0;

            if (Network.Connected)
            {
                try
                {
                    MCS.Disconnect();
                }
                catch
                {
                }

                Network.Close();
            }
        }

        internal static RdpPacket Receive()
        {
            byte[] buffer = new byte[0x3000];
            int count = Network.Receive(buffer);
            RdpPacket packet = new RdpPacket();
            packet.Write(buffer, 0, count);
            packet.Position = 0L;
            int num2 = 0;

            if (packet.ReadByte() == 3)
            {
                packet.ReadByte();
                num2 = packet.ReadBigEndian16();
                long position = packet.Position;

                while (num2 > count)
                {
                    int num4 = Network.Receive(buffer);
                    packet.Position = count;
                    packet.Write(buffer, 0, num4);
                    count += num4;
                }

                packet.Position = position;

                return packet;
            }
            num2 = packet.ReadByte();

            if ((num2 & 0x80) != 0)
            {
                num2 &= -129;
                num2 = num2 << (8 + packet.ReadByte());
            }

            return packet;
        }

        internal static RdpPacket ReceiveMCS(out int channel, out int type)
        {
            int num = 0;
            int num2 = 0;
            RdpPacket packet = ReceiveTPKTOrFastPath(out type);

            //Debug.WriteLine("ReceiveTPKTO Type: " + type.ToString());

            if ((type == 0xff) || (type == 0xfe))
            {
                channel = MCS.MSC_GLOBAL_CHANNEL;
                return packet;
            }

            if (type != 240)
            {
                throw new RDFatalException("Illegal data type " + ((int) type).ToString());
            }

            if (packet == null)
            {
                channel = -1;
                return null;
            }

            num = packet.ReadByte();
            num2 = num >> 2;

            if (num2 != MCS.SDIN)
            {
                if (num2 != MCS.DPUM)
                {
                    throw new RDFatalException("Illegal data opcode " + num.ToString());
                }

                throw new EndOfTransmissionException("End of transmission!");
            }

            packet.Position += 2L;
            channel = packet.ReadBigEndian16();
            packet.ReadByte();

            if ((packet.ReadByte() & 0x80) != 0)
            {
                packet.Position += 1L;
            }

            //Debug.WriteLine("Packet recived. Channel: " + channel);

            return packet;
        }

        internal static RdpPacket ReceiveTPKTOrFastPath(out int type)
        {
            int num = 0;
            int num2 = 0;
            RdpPacket p = Tcp_recv(null, 4);
            p.Position = 0L;

            if (p.Length == 0L)
            {
                type = -1;
                return null;
            }

            num2 = p.ReadByte();

            if (num2 == 3)
            {
                p.ReadByte();
                num = p.ReadBigEndian16();
                type = 0;
            }
            else
            {
                num = p.ReadByte();

                if ((num & Main.SecureValue5) != 0)
                {
                    num &= -129;
                    num = (num << 8) + p.ReadByte();
                }

                var flags = (FastPath_EncryptionFlags)(num2 >> 6);

                if (flags.HasFlag(FastPath_EncryptionFlags.FASTPATH_OUTPUT_ENCRYPTED))
                {
                    type = 0xfe;
                }
                else
                {
                    type = 0xff;
                }
            }

            long position = p.Position;
            p.Position = 4L;
            p = Tcp_recv(p, num - Main.SecureValue6);
            p.Position = position;

            if ((type != 0xff) && (type != 0xfe))
            {
                p.Position = 4L;
                p.ReadByte();
                type = p.ReadByte();

                if (type == 240)
                {
                    p.ReadByte();
                    return p;
                }

                p.ReadBigEndian16();
                p.ReadBigEndian16();
                p.ReadByte();
            }

            return p;
        }

        internal static RdpPacket Secure_receive(out bool bFastPath)
        {
            int num, num2;
            MCS.TS_SECURITY_HEADER num3;
            RdpPacket packet = null;

        Label_0001:
            bFastPath = false;
            packet = ReceiveMCS(out num, out num2);

            if (packet == null)
            {
                return null;
            }

            switch (num2)
            {
                case 0xff:
                    bFastPath = true;
                    return packet;

                case 0xfe:
                    packet = Secure.DecryptPacket(packet);
                    bFastPath = true;
                    return packet;
            }

            if (Secure.RDPEncrypted() || Licence.IsLicensePacket(packet))
            {
                num3 = (MCS.TS_SECURITY_HEADER)packet.ReadLittleEndian32();

                if (num3.HasFlag(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT))
                {
                    packet = Secure.DecryptPacket(packet);
                }

                if (num3.HasFlag(MCS.TS_SECURITY_HEADER.SEC_LICENSE_PKT))
                {
                    Licence.process(packet);
                    goto Label_0001;
                }

                if (num3.HasFlag(MCS.TS_SECURITY_HEADER.SEC_REDIRECTION_PKT))
                {
                    ControlFlow.processRedirection(packet, true);
                    goto Label_0001;
                }
            }

            if (num != MCS.MSC_GLOBAL_CHANNEL)
            {
                Channels.channel_process(num, packet);
                goto Label_0001;
            }
            
            return packet;
        }

        internal static RdpPacket Secure_receive(out int type)
        {
            int num = 0;

            if ((m_Packet == null) || (next_packet >= m_Packet.Length))
            {
                lastchannel = 0;
                bool bFastPath = false;
                m_Packet = Secure_receive(out bFastPath);

                if (m_Packet == null)
                {
                    type = 0;
                    return null;
                }

                if (bFastPath)
                {
                    type = 0xff;
                    next_packet = (int) m_Packet.Length;
                    return m_Packet;
                }

                next_packet = (int) m_Packet.Position;
            }
            else
            {
                m_Packet.Position = next_packet;
            }

            num = m_Packet.ReadLittleEndian16();

            switch (num)
            {
                case 0x8000:
                    next_packet += 8;
                    type = 0;
                    return m_Packet;

                case 0:
                    throw new Exception("Invalid Data packet length!");
            }

            type = m_Packet.ReadLittleEndian16() & 15;

            if (m_Packet.Position != m_Packet.Length)
            {
                m_Packet.Position += 2L;
            }

            next_packet += num;
            lastchannel = type;

            //Debug.WriteLine("Packet type: " + type);

            return m_Packet;
        }

        internal static RdpPacket Tcp_recv(RdpPacket packet, int length)
        {
            byte[] buffer = new byte[length];

            if (packet == null)
            {
                packet = new RdpPacket();
            }

            int num = 0;

            while (true)
            {
                int count = Network.Receive(buffer, length - num);
                num += count;
                packet.Write(buffer, 0, count);

                if (num == length)
                {
                    return packet;
                }

                Thread.Sleep(10);
            }
        }

        // Битовые флаги
        [Flags]
        private enum FastPath_EncryptionFlags
        {
            FASTPATH_OUTPUT_ENCRYPTED = 2,
            FASTPATH_OUTPUT_SECURE_CHECKSUM = 1
        }

    }
}