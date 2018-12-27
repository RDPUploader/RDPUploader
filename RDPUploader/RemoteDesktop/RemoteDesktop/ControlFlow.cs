using System;
using System.Collections.Generic;

namespace RemoteDesktop
{
    internal class ControlFlow
    {
        private static readonly byte[] RDP_SOURCE = new byte[] { 0x4d, 0x53, 0x54, 0x53, 0x43, 0 };
        private const int _aa = 0x30;
        private const int _b = 40;
        private const int _m = 13;
        private const int _n = 0x9c;
        private const int _o = 0x13;
        private const int _y = 2;
        private const ushort ALLOW_CACHE_WAITING_LIST_FLAG = 2;
        private const ushort PERSISTENT_KEYS_EXPECTED_FLAG = 1;
        private const int RDP_CAPLEN_ACTIVATE = 12;
        private const int RDP_CAPLEN_BITMAP = 0x1c;
        private const int RDP_CAPLEN_BMPCACHE = 40;
        private const int RDP_CAPLEN_BMPCACHE_HOSTSUPPORT = 8;
        private const int RDP_CAPLEN_BMPCACHE_V2 = 40;
        private const int RDP_CAPLEN_COLCACHE = 8;
        private const int RDP_CAPLEN_CONTROL = 12;
        private const int RDP_CAPLEN_FONT = 8;
        private const int RDP_CAPLEN_GENERAL = 0x18;
        private const int RDP_CAPLEN_GLYPHCACHE = 0x34;
        private const int RDP_CAPLEN_INPUT = 0x58;
        private const int RDP_CAPLEN_ORDER = 0x58;
        private const int RDP_CAPLEN_POINTER = 10;
        private const int RDP_CAPLEN_SHARE = 8;
        private const int RDP_CAPLEN_SOUND = 8;
        private const int RDP_CAPLEN_VIRTUALCHANNEL = 12;
        private const int RDP_CAPSET_ACTIVATE = 7;
        private const int RDP_CAPSET_BITMAP = 2;
        private const int RDP_CAPSET_BITMAP_CODECS = 0x1d;
        private const int RDP_CAPSET_BMPCACHE = 4;
        private const int RDP_CAPSET_BMPCACHE_HOSTSUPPORT = 0x12;
        private const int RDP_CAPSET_BMPCACHE_V2 = 0x13;
        private const int RDP_CAPSET_COLCACHE = 10;
        private const int RDP_CAPSET_COMPDESK = 0x19;
        private const int RDP_CAPSET_CONTROL = 5;
        private const int RDP_CAPSET_FONT = 14;
        private const int RDP_CAPSET_GENERAL = 1;
        private const int RDP_CAPSET_GLYPHCACHE = 0x10;
        private const int RDP_CAPSET_INPUT = 13;
        private const int RDP_CAPSET_LARGE_POINTER = 0x1b;
        private const int RDP_CAPSET_MULTIFRAGMENTUPDATE = 0x1a;
        private const int RDP_CAPSET_ORDER = 3;
        private const int RDP_CAPSET_POINTER = 8;
        private const int RDP_CAPSET_SHARE = 9;
        private const int RDP_CAPSET_SOUND = 12;
        private const int RDP_CAPSET_SURFACE_COMMANDS = 0x1c;
        private const int RDP_CAPSET_VIRTUALCHANNEL = 20;
        private const int RDP_CTL_COOPERATE = 4;
        private const int RDP_CTL_REQUEST_CONTROL = 1;
        private const int RDP_INPUT_SYNCHRONIZE = 0;
        private const int RDP_PDU_CONFIRM_ACTIVE = 3;

        internal static void processDemandActive(RdpPacket data)
        {
            int num3;
            RDPClient.rdp_shareid = data.getLittleEndian32();
            int num = data.getLittleEndian16();
            data.getLittleEndian16();
            data.Position += num;
            int numCaps = data.getLittleEndian16();
            data.getLittleEndian16();
            processServerCapabilities(data, numCaps);
            sendConfirmActive();
            sendSynchronize();
            sendControl(4);
            sendControl(1);
            ISO.Secure_Receive(out num3);
            ISO.Secure_Receive(out num3);
            ISO.Secure_Receive(out num3);
            if (!RDPClient.m_bInitialised)
            {
                sendPersistKeyList();
            }
            List<Rdp.InputInfo> inputToSend = new List<Rdp.InputInfo> 
            {
                new Rdp.InputInfo(0, Rdp.InputType.INPUT_EVENT_SYNC, 0, 0, 0)
            };
            IsoLayer.SendInput(inputToSend);
            sendFontList();
            ISO.Secure_Receive(out num3);
            if (RDPClient.m_bInitialised)
            {
                RDPClient.OnInitialise();
            }
            RDPClient.m_bInitialised = true;
            resetOrderState();
        }

        internal static void processRedirection(RdpPacket data, bool bStdRedirect)
        {
            if (!bStdRedirect)
            {
                data.getLittleEndian16();
            }
            data.getLittleEndian16();
            data.getLittleEndian16();
            RDPClient.sessionID = data.getLittleEndian32();
            int num = data.getLittleEndian32();
            if ((num & 1) != 0)
            {
                RDPClient.Host = data.ReadUnicodeString();
            }
            byte[] buffer = null;
            if ((num & 2) != 0)
            {
                int count = data.getLittleEndian32();
                buffer = new byte[count];
                data.Read(buffer, 0, count);
            }
            if ((num & 4) != 0)
            {
                RDPClient.Username = data.ReadUnicodeString();
            }
            if ((num & 8) != 0)
            {
                RDPClient.Domain = data.ReadUnicodeString();
            }
            if ((num & 0x10) != 0)
            {
                RDPClient.Password = data.ReadUnicodeString();
            }
            if ((num & 0x200) != 0)
            {
                RDPClient.Host = data.ReadUnicodeString();
            }
            if ((num & 0x100) != 0)
            {
                RDPClient.Host = data.ReadUnicodeString();
            }
            if (!string.IsNullOrEmpty(RDPClient.Domain))
            {
                RDPClient.DomainAndUsername = RDPClient.Domain + @"\" + RDPClient.Username;
            }
            else
            {
                RDPClient.DomainAndUsername = RDPClient.Username;
            }
            if ((num & 0x80) == 0)
            {
                Network.Close();
                Licence.Reset();
                Network.Connect(RDPClient.Host, RDPClient.Port);
                MCS.send_connection_request(buffer, false);
            }
        }

        internal static void processServerCapabilities(RdpPacket data, int numCaps)
        {
            RDPClient.m_bServerSupportsCacheV2 = false;
            while (numCaps-- > 0)
            {
                int num = data.getLittleEndian16();
                int num2 = data.getLittleEndian16();

                switch (num)
                {
                    case 1:
                    {
                        // RDP_CAPSET_GENERAL
                        data.getLittleEndian16(); // osMajorType
                        num2 -= 2;
                        data.getLittleEndian16(); // osMinorType
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.ReadByte();
                        num2--;
                        int num3 = data.ReadByte();
                        num2--;
                        RDPClient.suppress_output_supported = num3 > 0;
                        break;
                    }
                    case 2:
                        // RDP_CAPSET_BITMAP
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        data.getLittleEndian16();
                        num2 -= 2;
                        break;

                    case 3:
                        // RDP_CAPSET_ORDER
                        break;

                    case 4:
                        // RDP_CAPSET_BMPCACHE
                        break;

                    case 5:
                        // RDP_CAPSET_CONTROL
                        break;

                    case 8:
                        // RDP_CAPSET_POINTER
                        break;

                    case 9:
                        // RDP_CAPSET_SHARE
                        break;

                    case 10:
                        // RDP_CAPSET_COLCACHE
                        break;

                    case 13:
                    {
                        // RDP_CAPSET_INPUT
                        int num4 = data.getLittleEndian16();
                        num2 -= 2;
                        RDPClient.use_fastpath_input = false;
                        if ((num4 & 0x20) != 0)
                        {
                            RDPClient.use_fastpath_input = true;
                        }
                        if ((num4 & 8) != 0)
                        {
                            RDPClient.use_fastpath_input = true;
                        }
                        break;
                    }
                    case 14:
                        // RDP_CAPSET_FONT
                        break;

                    case 0x12:
                        // RDP_CAPSET_BMPCACHE_HOSTSUPPORT
                        RDPClient.m_bServerSupportsCacheV2 = true;
                        break;

                    case 20:
                        // RDP_CAPSET_VIRTUALCHANNEL
                        break;

                    case 0x19:
                        // RDP_CAPSET_COMPDESK
                        break;

                    case 0x1a:
                        // RDP_CAPSET_MULTIFRAGMENTUPDATE
                        break;

                    case 0x1b:
                        // RDP_CAPSET_LARGE_POINTER
                        break;

                    case 0x1c:
                        // RDP_CAPSET_SURFACE_COMMANDS
                        break;

                    case 0x1d:
                        // RDP_CAPSET_BITMAP_CODECS
                        break;

                    default:
                        // RDP_CAPSET unknown
                        break;
                }
                data.Position += num2 - 4;
            }
        }

        internal static void resetOrderState()
        {
            RDPClient.BoundsTop = RDPClient.BoundsLeft = 0;
            RDPClient.BoundsBottom = RDPClient.height - 1;
            RDPClient.BoundsRight = RDPClient.width - 1;
        }

        private static void sendActivateCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short) 7);
            data.WriteLittleEndian16((short) 12);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
        }

        private static void sendBitmapcacheCaps(RdpPacket packet)
        {
            if (RDPClient.m_bServerSupportsCacheV2)
            {
                packet.WriteLittleEndian16((short) 0x13);
                packet.WriteLittleEndian16((short) 40);
                packet.WriteLittleEndian16(false ? ((ushort) 1) : ((ushort) 0));
                packet.WriteByte(0);
                packet.WriteByte(3);
                uint num = 0;
                packet.WriteLittleEndian32(120);
                packet.WriteLittleEndian32((uint) (120 | num));
                packet.WriteLittleEndian32((uint) (0x400 | num));
                packet.WriteLittleEndian32(0);
                packet.WriteLittleEndian32(0);
                packet.Position += 12L;
            }
            else
            {
                packet.WriteLittleEndian16((short) 4);
                packet.WriteLittleEndian16((short) 40);
                packet.Position += 0x18L;
                packet.WriteLittleEndian16((short) 120);
                packet.WriteLittleEndian16((short) 0x300);
                packet.WriteLittleEndian16((short) 120);
                packet.WriteLittleEndian16((short) 0xc00);
                packet.WriteLittleEndian16((short) 0x400);
                packet.WriteLittleEndian16((short) 0x2000);
            }
        }

        private static void sendBitmapCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short) 2);
            data.WriteLittleEndian16((short) 0x1c);
            data.WriteLittleEndian16((short) RDPClient.server_bpp);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) RDPClient.width);
            data.WriteLittleEndian16((short) RDPClient.height);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 1);
            data.WriteByte(0);
            data.WriteByte(0);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 0);
        }

        private static void sendColorcacheCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short) 10);
            data.WriteLittleEndian16((short) 8);
            data.WriteLittleEndian16((short) 6);
            data.WriteLittleEndian16((short) 0);
        }

        private static void sendConfirmActive()
        {
            int num = 390;
            int num2 = 0;
            if (Secure.RDPEncrypted())
            {
                num2 |= MCS.SEC_ENCRYPT;
            }
            RdpPacket data = new RdpPacket();
            data.WriteLittleEndian16((short) ((0x10 + num) + RDP_SOURCE.Length));
            data.WriteLittleEndian16((short) 0x13);
            data.WriteLittleEndian16((short)(RDPClient.McsUserID + 0x3e9));
            data.WriteLittleEndian32(RDPClient.rdp_shareid);
            data.WriteLittleEndian16((short) 0x3ea);
            data.WriteLittleEndian16((short) RDP_SOURCE.Length);
            data.WriteLittleEndian16((short) num);
            data.Write(RDP_SOURCE, 0, RDP_SOURCE.Length);
            data.WriteLittleEndian16((short) 13);
            data.Position += 2L;
            sendGeneralCaps(data);
            sendBitmapCaps(data);
            sendOrderCaps(data);
            sendBitmapcacheCaps(data);
            sendColorcacheCaps(data);
            sendActivateCaps(data);
            sendControlCaps(data);
            sendPointerCaps(data);
            sendShareCaps(data);
            sendInputCaps(data);
            sendSoundCaps(data);
            sendFontCaps(data);
            sendGlyphCacheCaps(data);
            IsoLayer.SendMCS_GlobalChannel(data, num2);
        }

        private static void sendControl(int action)
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) action);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian32(0);
            IsoLayer.sendDataPDU(packet, IsoLayer.PDUType2.PDUTYPE2_CONTROL, Secure.RDPEncrypted() ? MCS.SEC_ENCRYPT : 0);
        }

        private static void sendControlCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 5);
            packet.WriteLittleEndian16((short) 12);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 2);
            packet.WriteLittleEndian16((short) 2);
        }

        private static void sendFontCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 14);
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 0);
        }

        private static void sendFontList()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 3);
            packet.WriteLittleEndian16((short) 50);
            IsoLayer.sendDataPDU(packet, IsoLayer.PDUType2.PDUTYPE2_FONTLIST, Secure.RDPEncrypted() ? MCS.SEC_ENCRYPT : 0);
        }

        private static void sendGeneralCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 0x18);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 3);
            data.WriteLittleEndian16((short) 0x200);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
            ExtraFlags flags = ExtraFlags.NO_BITMAP_COMPRESSION_HDR | ExtraFlags.ENC_SALTED_CHECKSUM | ExtraFlags.AUTORECONNECT_SUPPORTED | ExtraFlags.LONG_CREDENTIALS_SUPPORTED;
            if (RDPClient.enableFastPathOutput)
            {
                flags |= ExtraFlags.FASTPATH_OUTPUT_SUPPORTED;
            }
            data.WriteLittleEndian16((short) flags);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 0);
        }

        private static void sendGlyphCacheCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 0x10);
            packet.WriteLittleEndian16((short) 0x34);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 4);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 4);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 0x10);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 0x20);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 0x40);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 0x80);
            packet.WriteLittleEndian16((short) 0xfe);
            packet.WriteLittleEndian16((short) 0x100);
            packet.WriteLittleEndian16((short) 0x40);
            packet.WriteLittleEndian16((short) 0x800);
            packet.WriteLittleEndian32(0x10000);
            packet.WriteLittleEndian16((short) 3);
            packet.WriteLittleEndian16((short) 0);
        }

        private static void sendInputCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 13);
            packet.WriteLittleEndian16((short) 0x58);
            packet.WriteLittleEndian16((short) 0x11);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian32(0x409);
            packet.WriteLittleEndian32(4);
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian32(12);
            packet.Position += 0x40L;
        }

        private static void sendOrderCaps(RdpPacket data)
        {
            byte[] buffer = new byte[0x20];
            buffer[0] = 1;
            buffer[1] = 1;
            buffer[2] = 1;
            buffer[3] = 1;
            buffer[4] = 1;
            buffer[7] = 0;
            buffer[8] = 1;
            buffer[9] = 0;
            buffer[11] = 1;
            buffer[15] = 0;
            buffer[0x10] = 1;
            buffer[0x11] = 0;
            buffer[0x12] = 1;
            buffer[0x13] = 0;
            buffer[20] = 0;
            buffer[0x15] = 0;
            buffer[0x16] = 1;
            buffer[0x18] = 0;
            buffer[0x19] = 0;
            buffer[0x1a] = 0;
            buffer[0x1b] = 1;
            data.WriteLittleEndian16((short) 3);
            data.WriteLittleEndian16((short) 0x58);
            data.Position += 20L;
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 20);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 1);
            data.WriteLittleEndian16((short) 0);
            data.WriteLittleEndian16((short) 170);
            data.Write(buffer, 0, 0x20);
            data.WriteLittleEndian16((short) 0x6a1);
            data.WriteLittleEndian16((short) 2);
            data.Position += 4L;
            data.WriteLittleEndian32(0x38400);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian16((short) 0x4e4);
            data.WriteLittleEndian16((short) 0);
        }

        private static void sendPersistKeyList()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = 0;
            int num6 = 0;
            int num7 = 0;
            int num8 = 0;
            int num9 = 0;
            int num10 = 0;
            int offset = 0;
            while ((((num6 < num) || (num7 < num2)) || ((num8 < num3) || (num9 < num4))) || (num10 < num5))
            {
                int num12 = 0;
                int num13 = 0;
                int num14 = 0;
                int num15 = 0;
                int num16 = 0;
                bool bMoreKeys = false;
                RdpPacket packet = new RdpPacket();
                packet.WriteLittleEndian16((ushort) num12);
                packet.WriteLittleEndian16((ushort) num13);
                packet.WriteLittleEndian16((ushort) num14);
                packet.WriteLittleEndian16((ushort) num15);
                packet.WriteLittleEndian16((ushort) num16);
                packet.WriteLittleEndian16((ushort) num);
                packet.WriteLittleEndian16((ushort) num2);
                packet.WriteLittleEndian16((ushort) num3);
                packet.WriteLittleEndian16((ushort) num4);
                packet.WriteLittleEndian16((ushort) num5);
                byte num17 = 0;
                if (offset == 0)
                {
                    num17 = (byte) (num17 | 1);
                }
                if (!bMoreKeys)
                {
                    num17 = (byte) (num17 | 2);
                }
                packet.WriteByte(num17);
                packet.WriteByte(0);
                packet.WriteLittleEndian16((short) 0);

                IsoLayer.sendDataPDU(packet, IsoLayer.PDUType2.PDUTYPE2_BITMAPCACHE_PERSISTENT_LIST, Secure.RDPEncrypted() ? MCS.SEC_ENCRYPT : 0);
                num6 += num12;
                num7 += num13;
                num8 += num14;
                num9 += num15;
                num10 += num16;
            }
        }

        private static void sendPointerCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 10);
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 20);
            packet.WriteLittleEndian16((short) 0x15);
        }

        private static void sendShareCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 9);
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
        }

        private static void sendSoundCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short) 12);
            packet.WriteLittleEndian16((short) 8);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
        }

        private static void sendSynchronize()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 0x3ea);
            IsoLayer.sendDataPDU(packet, IsoLayer.PDUType2.PDUTYPE2_SYNCHRONIZE, Secure.RDPEncrypted() ? MCS.SEC_ENCRYPT : 0);
        }

        [Flags]
        private enum ExtraFlags
        {
            AUTORECONNECT_SUPPORTED = 8,
            ENC_SALTED_CHECKSUM = 0x10,
            FASTPATH_OUTPUT_SUPPORTED = 1,
            LONG_CREDENTIALS_SUPPORTED = 4,
            NO_BITMAP_COMPRESSION_HDR = 0x400
        }

        [Flags]
        private enum OsMajorTypes
        {
            UNSPECIFIED = 0x0000,
            WINDOWS = 0x0001,
            OS2 = 0x0002,
            MACINTOSH = 0x0003,
            UNIX = 0x0004,
            IOS = 0x0005,
            OSX = 0x0006,
            ANDROID = 0x0007
        }

        [Flags]
        private enum OsMinorTypes
        {
            UNSPECIFIED = 0x0000,
            WINDOWS_31X = 0x0001,
            WINDOWS_95 = 0x0002,
            WINDOWS_NT = 0x0003,
            OS2_V21 = 0x0004,
            POWER_PC = 0x0005,
            MACINTOSH = 0x0006,
            NATIVE_XSERVER = 0x0007,
            PSEUDO_XSERVER = 0x0008,
            WINDOWS_RT = 0x0009
        }

    }
}