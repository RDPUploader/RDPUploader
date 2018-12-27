using System;
using System.Collections.Generic;

namespace RdpUploadClient
{
    internal class ControlFlow
    {
        private const int _aa = 0x30;
        private const int _b = 40;
        private const int _m = 13;
        private const int _n = 0x9c;
        private const int _o = 0x13;
        private const int _y = 2;
        private const ushort ALLOW_CACHE_WAITING_LIST_FLAG = 2;
        private static readonly byte[] RDP_SOURCE = new byte[] { 0x4d, 0x53, 0x54, 0x53, 0x43, 0 };
        private static uint BMPCACHE2_FLAG_PERSIST = BitConverter.ToUInt32(BitConverter.GetBytes(-2147483648), 0);
        internal static bool m_bInitialised = false;
        internal static bool m_bServerSupportsCacheV2 = false;
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
        internal static int rdp_shareid;

        internal static void processDemandActive(RdpPacket data)
        {
            int num3;
            rdp_shareid = data.ReadLittleEndian32();
            int num = data.ReadLittleEndian16();
            data.ReadLittleEndian16();
            data.Position += num;
            int numCaps = data.ReadLittleEndian16();
            data.ReadLittleEndian16();
            processServerCapabilities(data, numCaps);
            sendConfirmActive();
            sendSynchronize();
            sendControl(4);
            sendControl(1);
            ISO.Secure_receive(out num3);
            ISO.Secure_receive(out num3);
            ISO.Secure_receive(out num3);

            if (Options.persistentBmpCache && !m_bInitialised)
            {
                sendPersistKeyList();
            }

            List<Rdp.InputInfo> inputToSend = new List<Rdp.InputInfo> 
            {
                new Rdp.InputInfo(0, Rdp.InputType.INPUT_EVENT_SYNC, 0, 0, 0)
            };

            //if (m_bInitialised)
            //{
            //    Options.OnInitialise();
            //}
            m_bInitialised = true;

            IsoLayer.FastSendInput(inputToSend);
            sendFontList();
            ISO.Secure_receive(out num3);
            resetOrderState();            
        }

        internal static void processRedirection(RdpPacket data, bool bStdRedirect)
        {
            if (!bStdRedirect)
            {
                data.ReadLittleEndian16();
            }

            data.ReadLittleEndian16();
            data.ReadLittleEndian16();
            Options.sessionID = data.ReadLittleEndian32();
            int num = data.ReadLittleEndian32();

            if ((num & 1) != 0)
            {
                Options.Host = data.ReadUnicodeString();
            }

            byte[] buffer = null;

            if ((num & 2) != 0)
            {
                int count = data.ReadLittleEndian32();
                buffer = new byte[count];
                data.Read(buffer, 0, count);
            }

            if ((num & 4) != 0)
            {
                Options.Username = data.ReadUnicodeString();
            }

            if ((num & 8) != 0)
            {
                Options.Domain = data.ReadUnicodeString();
            }

            if ((num & 0x10) != 0)
            {
                Options.Password = data.ReadUnicodeString();
            }

            if ((num & 0x200) != 0)
            {
                Options.Host = data.ReadUnicodeString();
            }

            if ((num & 0x100) != 0)
            {
                Options.Host = data.ReadUnicodeString();
            }

            if (!string.IsNullOrEmpty(Options.Domain))
            {
                Options.DomainAndUsername = Options.Domain + @"\" + Options.Username;
            }
            else
            {
                Options.DomainAndUsername = Options.Username;
            }

            if ((num & 0x80) == 0)
            {
                Network.Close();
                Licence.Reset();
                Network.Connect(Options.Host, Options.Port);
                MCS.sendСonnectionRequest(buffer, false);
            }
        }

        internal static void processServerCapabilities(RdpPacket data, int numCaps)
        {
            m_bServerSupportsCacheV2 = false;

            while (numCaps-- > 0)
            {
                int num = data.ReadLittleEndian16();
                int num2 = data.ReadLittleEndian16();

                switch ((Capstype)num)
                {
                    // RDP_CAPSET_GENERAL
                    case Capstype.CAPSTYPE_GENERAL:
                    {
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadByte();
                        num2--;
                        int num3 = data.ReadByte();
                        num2--;
                        Options.suppress_output_supported = num3 > 0;
                        break;
                    }

                    // RDP_CAPSET_BITMAP
                    case Capstype.CAPSTYPE_BITMAP:
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        data.ReadLittleEndian16();
                        num2 -= 2;
                        break;

                    // RDP_CAPSET_ORDER
                    case Capstype.CAPSTYPE_ORDER:
                        break;

                    // RDP_CAPSET_BMPCACHE
                    case Capstype.CAPSTYPE_BITMAPCACHE:
                        break;

                    // RDP_CAPSET_CONTROL
                    case Capstype.CAPSTYPE_CONTROL:
                        break;

                    // RDP_CAPSET_POINTER
                    case Capstype.CAPSTYPE_POINTER:
                        break;

                    // RDP_CAPSET_SHARE
                    case Capstype.CAPSTYPE_SHARE:
                        break;

                    // RDP_CAPSET_COLCACHE
                    case Capstype.CAPSTYPE_COLCACHE:
                        break;

                    // RDP_CAPSET_INPUT
                    case Capstype.CAPSTYPE_INPUT:
                    {
                        int num4 = data.ReadLittleEndian16(); // inputFlags
                        num2 -= 2;

                        //Options.use_fastpath_input = false; // Полностью отключаем FastPath Input
                        //if ((num4 & 0x20) != 0)
                        //{
                        //    Options.use_fastpath_input = true;
                        //}
                        //if ((num4 & 8) != 0)
                        //{
                        //    Options.use_fastpath_input = true;
                        //}
                        break;
                    }

                    // RDP_CAPSET_FONT
                    case Capstype.CAPSTYPE_FONT:
                        break;

                    // RDP_CAPSET_BMPCACHE_HOSTSUPPORT
                    case Capstype.CAPSTYPE_BITMAPCACHE_HOSTSUPPORT:
                        m_bServerSupportsCacheV2 = true;
                        break;

                    // RDP_CAPSET_VIRTUALCHANNEL
                    case Capstype.CAPSTYPE_VIRTUALCHANNEL:
                        break;

                    // RDP_CAPSET_COMPDESK
                    case Capstype.CAPSETTYPE_COMPDESK:
                        break;

                    // RDP_CAPSET_MULTIFRAGMENTUPDATE
                    case Capstype.CAPSETTYPE_MULTIFRAGMENTUPDATE:
                        break;

                    // RDP_CAPSET_LARGE_POINTER
                    case Capstype.CAPSETTYPE_LARGE_POINTER:
                        break;

                    // RDP_CAPSET_SURFACE_COMMANDS
                    case Capstype.CAPSETTYPE_SURFACE_COMMANDS:
                        break;

                    // RDP_CAPSET_BITMAP_CODECS
                    case Capstype.CAPSETTYPE_BITMAP_CODECS:
                        break;

                    // RDP_CAPSET unknown
                    default:
                        break;
                }

                data.Position += num2 - 4;
            }

            if (!m_bServerSupportsCacheV2)
            {
                Options.persistentBmpCache = false;
            }
        }

        internal static void resetOrderState()
        {
            Orders.Reset();
            SurfaceClip.Reset();
            DestBltOrder.Reset();
            MultiDestBltOrder.Reset();
            PatBltOrder.Reset();
            MultiPatBltOrder.Reset();
            ScreenBltOrder.Reset();
            MultiScreenBltOrder.Reset();
            MemBltOrder.Reset();
            TriBltOrder.Reset();
            ScreenBltOrder.Reset();
            LineOrder.Reset();
            PolylineOrder.Reset();
            RectangleOrder.Reset();
            MultiRectangleOrder.Reset();
            DeskSaveOrder.Reset();
            Glyph.Reset();
            Cache.Reset(!m_bInitialised);
            Text2Order.Reset();
            Options.BoundsTop = Options.BoundsLeft = 0;
            Options.BoundsBottom = Options.height - 1;
            Options.BoundsRight = Options.width - 1;
        }

        private static void sendActivateCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short)7);
            data.WriteLittleEndian16((short)12);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
        }

        private static void sendBitmapcacheCaps(RdpPacket packet)
        {
            if (Options.cache_bitmaps && m_bServerSupportsCacheV2)
            {
                packet.WriteLittleEndian16((short)0x13);
                packet.WriteLittleEndian16((short)40);
                packet.WriteLittleEndian16(Options.persistentBmpCache ? ((ushort)1) : ((ushort)0));
                packet.WriteByte(0);
                packet.WriteByte(3);
                uint num = Options.persistentBmpCache ? BMPCACHE2_FLAG_PERSIST : 0;
                packet.WriteLittleEndian32(120);
                packet.WriteLittleEndian32((uint)(120 | num));
                packet.WriteLittleEndian32((uint)(0x400 | num));
                packet.WriteLittleEndian32(0);
                packet.WriteLittleEndian32(0);
                packet.Position += 12L;
            }
            else
            {
                packet.WriteLittleEndian16((short)4);
                packet.WriteLittleEndian16((short)40);
                packet.Position += 0x18L;
                packet.WriteLittleEndian16((short)120);
                packet.WriteLittleEndian16((short)0x300);
                packet.WriteLittleEndian16((short)120);
                packet.WriteLittleEndian16((short)0xc00);
                packet.WriteLittleEndian16((short)0x400);
                packet.WriteLittleEndian16((short)0x2000);
            }
        }

        private static void sendBitmapCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short)2);
            data.WriteLittleEndian16((short)0x1c);
            data.WriteLittleEndian16((short)Options.server_bpp);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)Options.width);
            data.WriteLittleEndian16((short)Options.height);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)1);
            data.WriteByte(0);
            data.WriteByte(0);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)0);
        }

        private static void sendColorcacheCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short)10);
            data.WriteLittleEndian16((short)8);
            data.WriteLittleEndian16((short)6);
            data.WriteLittleEndian16((short)0);
        }

        private static void sendConfirmActive()
        {
            int num = 390;
            int num2 = 0;

            if (Secure.RDPEncrypted())
            {
                num2 |= (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT);
            }

            RdpPacket data = new RdpPacket();
            data.WriteLittleEndian16((short)((0x10 + num) + RDP_SOURCE.Length));
            data.WriteLittleEndian16((short)0x13);
            data.WriteLittleEndian16((short)(MCS.McsUserID + 0x3e9));
            data.WriteLittleEndian32(rdp_shareid);
            data.WriteLittleEndian16((short)0x3ea);
            data.WriteLittleEndian16((short)RDP_SOURCE.Length);
            data.WriteLittleEndian16((short)num);
            data.Write(RDP_SOURCE, 0, RDP_SOURCE.Length);
            data.WriteLittleEndian16((short)13);
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

            IsoLayer.SendToGlobalChannel(data, num2);
        }

        private static void sendControl(int action)
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)action);
            packet.WriteLittleEndian16((short)0);
            packet.WriteLittleEndian32(0);

            IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_CONTROL, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
        }

        private static void sendControlCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short)5);
            packet.WriteLittleEndian16((short)12);
            packet.WriteLittleEndian16((short)0);
            packet.WriteLittleEndian16((short)0);
            packet.WriteLittleEndian16((short)2);
            packet.WriteLittleEndian16((short)2);
        }

        private static void sendFontCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short)14);
            packet.WriteLittleEndian16((short)8);
            packet.WriteLittleEndian16((short)1);
            packet.WriteLittleEndian16((short)0);
        }

        private static void sendFontList()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian16((short) 3);
            packet.WriteLittleEndian16((short) 50);
            IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_FONTLIST, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
        }

        private static void sendGeneralCaps(RdpPacket data)
        {
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)0x18);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)3);
            data.WriteLittleEndian16((short)0x200);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);

            ExtraFlags flags =
                ExtraFlags.NO_BITMAP_COMPRESSION_HDR |
                ExtraFlags.ENC_SALTED_CHECKSUM |
                ExtraFlags.AUTORECONNECT_SUPPORTED |
                ExtraFlags.LONG_CREDENTIALS_SUPPORTED;

            if (Options.enableFastPathOutput)
            {
                flags |= ExtraFlags.FASTPATH_OUTPUT_SUPPORTED;
            }

            data.WriteLittleEndian16((short)flags);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)0);
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

        // Input Capability Set (TS_INPUT_CAPABILITYSET)
        private static void sendInputCaps(RdpPacket packet)
        {
            packet.WriteLittleEndian16((short)Capstype.CAPSTYPE_INPUT);
            packet.WriteLittleEndian16((short)0x58);
            packet.WriteLittleEndian16((short)(
                InputFlags.INPUT_FLAG_FASTPATH_INPUT | 
                InputFlags.INPUT_FLAG_FASTPATH_INPUT2 |
                InputFlags.INPUT_FLAG_SCANCODES | 
                InputFlags.INPUT_FLAG_UNICODE));
            packet.WriteLittleEndian16((short) 0);
            packet.WriteLittleEndian32(Options.Keyboard); // Клавиатура
            packet.WriteLittleEndian32(0x00000004); // IBM enhanced (101- or 102-key) keyboard
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian32(12); // Функциональные клавиши (F1-F12)
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
            Cache.TotalBitmapCache(out num, out num2, out num3, out num4, out num5);
            int offset = 0;
            while ((((num6 < num) || (num7 < num2)) || ((num8 < num3) || (num9 < num4))) || (num10 < num5))
            {
                int num12 = 0;
                int num13 = 0;
                int num14 = 0;
                int num15 = 0;
                int num16 = 0;
                bool bMoreKeys = false;
                List<ulong> list = Cache.GetBitmapCache(offset, 0xff, out num12, out num13, out num14, out num15, out num16, out bMoreKeys);
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
                foreach (ulong num18 in list)
                {
                    packet.Write(BitConverter.GetBytes(num18), 0, 8);
                }
                IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_BITMAPCACHE_PERSISTENT_LIST, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
                offset += list.Count;
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

        private static void sendSupressOutput(bool bAllowDisplayUpdates)
        {
            if (Options.suppress_output_supported)
            {
                RdpPacket packet = new RdpPacket();
                packet.WriteByte(bAllowDisplayUpdates ? ((byte) 1) : ((byte) 0));
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteByte(0);

                if (bAllowDisplayUpdates)
                {
                    packet.WriteLittleEndian16((short) 0);
                    packet.WriteLittleEndian16((short) 0);
                    packet.WriteLittleEndian16((ushort) Options.width);
                    packet.WriteLittleEndian16((ushort) Options.height);
                }

                IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_SUPPRESS_OUTPUT, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
            }
        }

        private static void sendSynchronize()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 0x3ea);

            IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_SYNCHRONIZE, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
        }

        // Битовые флаги
        [Flags]
        private enum Capstype
        {
            CAPSTYPE_GENERAL = 1,
            CAPSTYPE_BITMAP = 2,
            CAPSTYPE_ORDER = 3,
            CAPSTYPE_BITMAPCACHE = 4,
            CAPSTYPE_CONTROL = 5,
            CAPSTYPE_ACTIVATION = 7,
            CAPSTYPE_POINTER = 8,
            CAPSTYPE_SHARE = 9,
            CAPSTYPE_COLCACHE = 10,
            CAPSTYPE_SOUND = 12,
            CAPSTYPE_INPUT = 13,
            CAPSTYPE_FONT = 14,
            CAPSTYPE_BRUSH = 15,
            CAPSTYPE_GLYPHCACHE = 16,
            CAPSTYPE_OFFSCREENCACHE = 17,
            CAPSTYPE_BITMAPCACHE_HOSTSUPPORT = 18,
            CAPSTYPE_BITMAPCACHE_REV2 = 19,
            CAPSTYPE_VIRTUALCHANNEL = 20,
            CAPSETTYPE_MULTIFRAGMENTUPDATE = 26,
            CAPSETTYPE_LARGE_POINTER = 27,
            CAPSETTYPE_COMPDESK = 0x0019,
            CAPSETTYPE_SURFACE_COMMANDS = 0x001C,
            CAPSETTYPE_BITMAP_CODECS = 0x001D,
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
        private enum GlyphSupportLevel
        {
            GLYPH_SUPPORT_NONE,
            GLYPH_SUPPORT_PARTIAL,
            GLYPH_SUPPORT_FULL,
            GLYPH_SUPPORT_ENCODE
        }

        [Flags]
        private enum InputFlags
        {
            INPUT_FLAG_FASTPATH_INPUT = 0x0008,
            INPUT_FLAG_FASTPATH_INPUT2 = 0x0020,
            INPUT_FLAG_MOUSEX = 0x0004,
            INPUT_FLAG_SCANCODES = 0x0001,
            INPUT_FLAG_UNICODE = 0x0010,
            TS_INPUT_FLAG_MOUSE_HWHEEL = 0x0100
        }

        [Flags]
        private enum OrderFlagIndex
        {
            TS_NEG_DSTBLT_INDEX,
            TS_NEG_PATBLT_INDEX,
            TS_NEG_SCRBLT_INDEX,
            TS_NEG_MEMBLT_INDEX,
            TS_NEG_MEM3BLT_INDEX,
            UnusedIndex1,
            UnusedIndex2,
            TS_NEG_DRAWNINEGRID_INDEX,
            TS_NEG_LINETO_INDEX,
            TS_NEG_MULTI_DRAWNINEGRID_INDEX,
            UnusedIndex3,
            TS_NEG_SAVEBITMAP_INDEX,
            UnusedIndex4,
            UnusedIndex5,
            UnusedIndex6,
            TS_NEG_MULTIDSTBLT_INDEX,
            TS_NEG_MULTIPATBLT_INDEX,
            TS_NEG_MULTISCRBLT_INDEX,
            TS_NEG_MULTIOPAQUERECT_INDEX,
            TS_NEG_FAST_INDEX_INDEX,
            TS_NEG_POLYGON_SC_INDEX,
            TS_NEG_POLYGON_CB_INDEX,
            TS_NEG_POLYLINE_INDEX,
            UnusedIndex7,
            TS_NEG_FAST_GLYPH_INDEX,
            TS_NEG_ELLIPSE_SC_INDEX,
            TS_NEG_ELLIPSE_CB_INDEX,
            TS_NEG_INDEX_INDEX,
            UnusedIndex8,
            UnusedIndex9,
            UnusedIndex10,
            UnusedIndex11
        }

        [Flags]
        private enum OrderFlags
        {
            COLORINDEXSUPPORT = 0x20,
            NEGOTIATEORDERSUPPORT = 2,
            ORDERFLAGS_EXTRA_FLAGS = 0x80,
            SOLIDPATTERNBRUSHONLY = 0x40,
            ZEROBOUNDSDELTASSUPPORT = 8
        }

        [Flags]
        private enum OrderSupportExFlags
        {
            ORDERFLAGS_EX_ALTSEC_FRAME_MARKER_SUPPORT = 4,
            ORDERFLAGS_EX_CACHE_BITMAP_REV3_SUPPORT = 2
        }

        [Flags]
        private enum SoundFlags
        {
            SOUND_BEEPS_FLAG = 1
        }

    }
}