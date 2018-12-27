using System;
using System.Collections.Generic;

namespace RemoteDesktop
{
    internal class IsoLayer
    {
        internal static void internal_sendInput(int time, int message_type, int device_flags, uint param1, uint param2)
        {
            if (Network.ConnectionAlive || (message_type == 0))
            {
                RdpPacket packet = new RdpPacket();
                packet.WriteLittleEndian16((short) 1);
                packet.WriteLittleEndian16((short) 0);
                packet.WriteLittleEndian32(time);
                packet.WriteLittleEndian16((short) message_type);
                packet.WriteLittleEndian16((short) device_flags);
                packet.WriteLittleEndian16((ushort) param1);
                packet.WriteLittleEndian16((ushort) param2);
                sendDataPDU(packet, PDUType2.PDUTYPE2_INPUT, Secure.RDPEncrypted() ? MCS.SEC_ENCRYPT : 0);
            }
        }

        internal static void send_to_channel(RdpPacket sec_data, int flags, int channel)
        {
            sec_data.Position = 0L;
            byte[] buffer = new byte[sec_data.Length];
            sec_data.Read(buffer, 0, buffer.Length);
            if ((RDPClient.enc_count == 0x1000) && Secure.RDPEncrypted())
            {
                Secure.m_Encrypt_Key = Secure.update(Secure.m_Encrypt_Key, RDPClient._r);
                byte[] destinationArray = new byte[Secure.m_KeyLength];
                Array.Copy(Secure.m_Encrypt_Key, 0, destinationArray, 0, Secure.m_KeyLength);
                RDPClient.m_RC4_Enc.engineInitEncrypt(destinationArray);
                RDPClient.enc_count = 0;
            }
            if (Secure.RDPEncrypted())
            {
                byte[] buffer3 = Secure.sign(RDPClient.m_Sec_Sign_Key, 8, Secure.m_KeyLength, buffer, buffer.Length);
                byte[] buffer4 = RDPClient.m_RC4_Enc.crypt(buffer);
                sec_data = new RdpPacket();
                sec_data.WriteLittleEndian32(flags);
                sec_data.Write(buffer3, 0, buffer3.Length);
                sec_data.Write(buffer4, 0, buffer4.Length);
            }
            else
            {
                flags &= -9;
                sec_data = new RdpPacket();
                if (flags != 0)
                {
                    sec_data.WriteLittleEndian32(flags);
                }
                sec_data.Write(buffer, 0, buffer.Length);
            }
            SendMCS(sec_data, channel);
            RDPClient.enc_count++;
        }

        internal static void sendDataPDU(RdpPacket packet, PDUType2 type, int sec_flags)
        {
            RdpPacket packet2 = new RdpPacket();
            packet.Position = 0L;
            int num = ((int) packet.Length) + 0x12;
            packet2.WriteLittleEndian16((short) num);
            packet2.WriteLittleEndian16((short) 0x17);
            packet2.WriteLittleEndian16((short) (RDPClient.McsUserID + 0x3e9));
            packet2.WriteLittleEndian32(RDPClient.rdp_shareid);
            packet2.WriteByte(0);
            packet2.WriteByte(1);
            packet2.WriteLittleEndian16((short) (num - 14));
            packet2.WriteByte((byte) type);
            packet2.WriteByte(0);
            packet2.WriteLittleEndian16((short) 0);
            packet2.copyToByteArray(packet);
            SendMCS_GlobalChannel(packet2, sec_flags);
        }

        internal static void SendInput(List<Rdp.InputInfo> InputToSend)
        {
            if (RDPClient.use_fastpath_input)
            {
                RdpPacket packet = new RdpPacket();
                ushort num = 1;
                int count = InputToSend.Count;
                if (count < 0x10)
                {
                    packet.WriteByte((byte) (count << 2));
                }
                else
                {
                    packet.WriteByte(0);
                    num = (ushort) (num + 1);
                }
                foreach (Rdp.InputInfo info in InputToSend)
                {
                    switch (info.Message_Type)
                    {
                        case Rdp.InputType.INPUT_EVENT_SCANCODE:
                            num = (ushort) (num + 2);
                            break;

                        case Rdp.InputType.INPUT_EVENT_UNICODE:
                            num = (ushort) (num + 3);
                            break;

                        case Rdp.InputType.INPUT_EVENT_MOUSE:
                            num = (ushort) (num + 7);
                            break;

                        case Rdp.InputType.INPUT_EVENT_SYNC:
                            num = (ushort) (num + 1);
                            break;
                    }
                }
                num = (ushort) (num + 1);
                if (num > 0x7f)
                {
                    num = (ushort) (num + 1);
                }
                packet.WriteEncodedUnsigned16(num);
                if (count >= 0x10)
                {
                    packet.WriteByte((byte) count);
                }
                foreach (Rdp.InputInfo info2 in InputToSend)
                {
                    int num3 = 0;
                    switch (info2.Message_Type)
                    {
                        case Rdp.InputType.INPUT_EVENT_SCANCODE:
                            if ((info2.Device_Flags & 0x8000) != 0)
                            {
                                num3 |= 1;
                            }
                            if ((info2.Device_Flags & 0x100) != 0)
                            {
                                num3 |= 2;
                            }
                            packet.WriteByte((byte) num3);
                            packet.WriteByte((byte) info2.Param1);
                            break;

                        case Rdp.InputType.INPUT_EVENT_UNICODE:
                            if ((info2.Device_Flags & 0x8000) != 0)
                            {
                                num3 |= 1;
                            }
                            packet.WriteByte((byte) (0x80 | num3));
                            packet.WriteLittleEndian16((ushort) info2.Param1);
                            break;

                        case Rdp.InputType.INPUT_EVENT_MOUSE:
                            packet.WriteByte(0x20);
                            packet.WriteLittleEndian16((ushort) info2.Device_Flags);
                            packet.WriteLittleEndian16((ushort) info2.Param1);
                            packet.WriteLittleEndian16((ushort) info2.Param2);
                            break;

                        case Rdp.InputType.INPUT_EVENT_SYNC:
                            packet.WriteByte(0x60);
                            break;
                    }
                }
                Write(packet);
            }
            else
            {
                foreach (Rdp.InputInfo info3 in InputToSend)
                {
                    internal_sendInput(info3.Time, (int) info3.Message_Type, info3.Device_Flags, info3.Param1, info3.Param2);
                }
            }
        }

        internal static void SendMCS(RdpPacket packet, int channel)
        {
            int length = (int) packet.Length;
            length |= 0x8000;
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte) (MCS.SDRQ << 2));
            data.WriteBigEndian16((short)RDPClient.McsUserID);
            data.WriteBigEndian16((short) channel);
            data.WriteByte(0x70);
            data.WriteBigEndian16((short) length);
            data.copyToByteArray(packet);
            SendTPKT(data);
        }

        internal static void SendMCS_GlobalChannel(RdpPacket sec_data, int sec_flags)
        {
            send_to_channel(sec_data, sec_flags, MCS.MSC_GLOBAL_CHANNEL);
        }

        public static void SendTPKT(RdpPacket data)
        {
            short num = (short) (data.Length + 7L);
            data.Position = 0L;
            RdpPacket packet = new RdpPacket();
            packet.WriteByte(3);
            packet.WriteByte(0);
            packet.WriteBigEndian16(num);
            packet.WriteByte(2);
            packet.WriteByte(240);
            packet.WriteByte(0x80);
            packet.copyToByteArray(data);
            Write(packet);
        }

        internal static void Write(RdpPacket data)
        {
            data.Position = 0L;
            byte[] buffer = new byte[data.Length];
            data.Read(buffer, 0, (int) data.Length);
            Network.Send(buffer);
        }

        [Flags]
        public enum PDUType2
        {
            PDUTYPE2_ARC_STATUS_PDU = 50,
            PDUTYPE2_BITMAPCACHE_ERROR_PDU = 0x2c,
            PDUTYPE2_BITMAPCACHE_PERSISTENT_LIST = 0x2b,
            PDUTYPE2_CONTROL = 20,
            PDUTYPE2_DRAWGDIPLUS_ERROR_PDU = 0x31,
            PDUTYPE2_DRAWNINEGRID_ERROR_PDU = 0x30,
            PDUTYPE2_FONTLIST = 0x27,
            PDUTYPE2_FONTMAP = 40,
            PDUTYPE2_INPUT = 0x1c,
            PDUTYPE2_MONITOR_LAYOUT_PDU = 0x37,
            PDUTYPE2_OFFSCRCACHE_ERROR_PDU = 0x2e,
            PDUTYPE2_PLAY_SOUND = 0x22,
            PDUTYPE2_POINTER = 0x1b,
            PDUTYPE2_REFRESH_RECT = 0x21,
            PDUTYPE2_SAVE_SESSION_INFO = 0x26,
            PDUTYPE2_SET_ERROR_INFO_PDU = 0x2f,
            PDUTYPE2_SET_KEYBOARD_IME_STATUS = 0x2d,
            PDUTYPE2_SET_KEYBOARD_INDICATORS = 0x29,
            PDUTYPE2_SHUTDOWN_DENIED = 0x25,
            PDUTYPE2_SHUTDOWN_REQUEST = 0x24,
            PDUTYPE2_STATUS_INFO_PDU = 0x36,
            PDUTYPE2_SUPPRESS_OUTPUT = 0x23,
            PDUTYPE2_SYNCHRONIZE = 0x1f,
            PDUTYPE2_UPDATE = 2
        }

    }
}