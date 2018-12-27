using System;
using System.Threading;

namespace RemoteDesktop
{    
    internal class ISO
    {
        public static void Disconnect()
        {
            RDPClient.m_Packet = null;
            RDPClient.next_packet = 0;

            if (Network.Connected)
            {
                try
                {
                    MCS.Disconnect();
                }
                catch { }
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
            return packet;
        }

        internal static RdpPacket ReceiveTPKTOrFastPath(out int type)
        {
            int num = 0;
            int num2 = 0;
            RdpPacket p = Tcp_Receive(null, 4);
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
                if ((num & 0x80) != 0)
                {
                    num &= -129;
                    num = (num << 8) + p.ReadByte();
                }
                FastPath_EncryptionFlags flags = (FastPath_EncryptionFlags) (num2 >> 6);
                if ((flags & FastPath_EncryptionFlags.FASTPATH_OUTPUT_ENCRYPTED) != ((FastPath_EncryptionFlags) 0))
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
            p = Tcp_Receive(p, num - 4);
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

        internal static RdpPacket Secure_Receive(out bool bFastPath)
        {
            int num;
            int num2;
            int num3 = 0;
            RdpPacket packet = null;
        Label_0004:
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
                num3 = packet.getLittleEndian32();
                if ((num3 & MCS.SEC_ENCRYPT) != 0)
                {
                    packet = Secure.DecryptPacket(packet);
                }
                if ((num3 & 0x80) != 0)
                {
                    Licence.process(packet);
                    goto Label_0004;
                }
                if ((num3 & MCS.SEC_REDIRECTION_PKT) != 0)
                {
                    ControlFlow.processRedirection(packet, true);
                    goto Label_0004;
                }
            }
            if (num != MCS.MSC_GLOBAL_CHANNEL)
            {
                Channels.channel_process(num, packet);
                goto Label_0004;
            }
            return packet;
        }

        internal static RdpPacket Secure_Receive(out int type)
        {
            int num = 0;
            if ((RDPClient.m_Packet == null) || (RDPClient.next_packet >= RDPClient.m_Packet.Length))
            {
                RDPClient.lastchannel = 0;
                bool bFastPath = false;
                RDPClient.m_Packet = Secure_Receive(out bFastPath);
                if (RDPClient.m_Packet == null)
                {
                    type = 0;
                    return null;
                }
                if (bFastPath)
                {
                    type = 0xff;
                    RDPClient.next_packet = (int)RDPClient.m_Packet.Length;
                    return RDPClient.m_Packet;
                }
                RDPClient.next_packet = (int)RDPClient.m_Packet.Position;
            }
            else
            {
                RDPClient.m_Packet.Position = RDPClient.next_packet;
            }
            num = RDPClient.m_Packet.getLittleEndian16();
            switch (num)
            {
                case 0x8000:
                    RDPClient.next_packet += 8;
                    type = 0;
                    return RDPClient.m_Packet;

                case 0:
                    throw new Exception("Invalid Data packet length!");
            }
            type = RDPClient.m_Packet.getLittleEndian16() & 15;
            if (RDPClient.m_Packet.Position != RDPClient.m_Packet.Length)
            {
                RDPClient.m_Packet.Position += 2L;
            }
            RDPClient.next_packet += num;
            RDPClient.lastchannel = type;
            return RDPClient.m_Packet;
        }

        internal static RdpPacket Tcp_Receive(RdpPacket p, int length)
        {
            byte[] buffer = new byte[length];
            if (p == null)
            {
                p = new RdpPacket();
            }
            int num = 0;
            while (true)
            {
                int count = Network.Receive(buffer, length - num);
                num += count;
                p.Write(buffer, 0, count);
                if (num == length)
                {
                    return p;
                }
                Thread.Sleep(10);
            }
        }

        [Flags]
        private enum FastPath_EncryptionFlags
        {
            FASTPATH_OUTPUT_ENCRYPTED = 2,
            FASTPATH_OUTPUT_SECURE_CHECKSUM = 1
        }

    }
}