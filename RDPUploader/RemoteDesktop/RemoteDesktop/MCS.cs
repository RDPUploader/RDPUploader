using System;

namespace RemoteDesktop
{
    internal class MCS
    {
        public static readonly int _l = 0x33;
        public static readonly int _p = 0x40;
        private static readonly int AUCF = 11;
        private static readonly int AURQ = 10;
        private static readonly int CJCF = 15;
        private static readonly int CJRQ = 14;
        private const int CONNECTION_REQUEST = 0xe0;
        internal static readonly int DPUM = 8;
        private static readonly int EDRQ = 1;
        private static readonly int MCS_USERCHANNEL_BASE = 0x3e9;
        internal static readonly int MSC_GLOBAL_CHANNEL = 0x3eb;
        private const int PROTOCOL_VERSION = 3;
        internal static readonly int SDIN = 0x1a;
        internal static readonly int SDRQ = 0x19;
        public static readonly int SEC_ENCRYPT = 8;
        public static readonly int SEC_REDIRECTION_PKT = 0x400;
        private const int SEC_TAG_CLI_CHANNELS = 0xc003;
        private const int SEC_TAG_CLI_CLUSTER = 0xc004;
        private const int SEC_TAG_CLI_CRYPT = 0xc002;
        private const int SEC_TAG_CLI_INFO = 0xc001;
        private const int SEC_TAG_CLI_MONITOR = 0xc005;
        private const int SEC_TAG_CLI_MSGCHANNEL = 0xc006;
        private const int TYPE_RDP_NEG_FAILURE = 3;
        private const int TYPE_RDP_NEG_REQ = 1;
        private const int TYPE_RDP_NEG_RSP = 2;

        private static int BERIntSize(int data)
        {
            if (data > 0xff)
            {
                return 4;
            }
            return 3;
        }

        private static int berParseHeader(RdpPacket data, BER_Header eTagVal)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = (int) eTagVal;
            if (num4 > 0xff)
            {
                num = data.ReadBigEndian16();
            }
            else
            {
                num = data.ReadByte();
            }
            if (num != num4)
            {
                throw new RDFatalException("Bad tag " + num.ToString() + " but need " + eTagVal.ToString());
            }
            num3 = data.ReadByte();
            if (num3 <= 0x80)
            {
                return num3;
            }
            num3 -= 0x80;
            num2 = 0;
            while (num3-- != 0)
            {
                num2 = (num2 << 8) + data.ReadByte();
            }
            return num2;
        }

        private static int c(int data0, int data1)
        {
            int num = 0;
            if (data0 > 0xff)
            {
                num += 2;
            }
            else
            {
                num++;
            }
            if (data1 >= 0x80)
            {
                return (num + 3);
            }
            num++;
            return num;
        }

        public static void Disconnect()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 0x3ea);
            IsoLayer.sendDataPDU(packet, IsoLayer.PDUType2.PDUTYPE2_SHUTDOWN_REQUEST, Secure.RDPEncrypted() ? SEC_ENCRYPT : 0);
            packet = new RdpPacket();
            packet.WriteByte((byte) (DPUM << 2));
            packet.WriteByte(3);
            IsoLayer.SendTPKT(packet);
        }

        internal static void send_connection_request(byte[] loadBalanceToken, bool bAutoReconnect)
        {
            int num;
            RDPClient.dec_count = 0;
            RDPClient.enc_count = 0;
            Network.ConnectionStage = RDPClient.eConnectionStage.Negotiating;
            if (RDPClient.enableNLA)
            {
                sendConnectNegotiation(3, loadBalanceToken);
                num = receiveConnectNegotiation();

                if (num == 0x10000000)
                {
                    Network.Close();
                    Network.Connect(RDPClient.Host, RDPClient.Port);
                    sendConnectNegotiation(0, loadBalanceToken);
                    num = receiveConnectNegotiation();

                    if (num != 0)
                    {
                        throw new RDFatalException("Security negotiation failed!");
                    }
                }
                else
                {
                    if (((num & 1) != 0) || ((num & 2) != 0))
                    {
                        Network.ConnectionStage = RDPClient.eConnectionStage.Securing;
                        Network.ConnectSSL();
                    }
                    if ((num & 2) != 0)
                    {
                        Network.ConnectionStage = RDPClient.eConnectionStage.Authenticating;
                        CredSSP.Negotiate(Network.GetSSLPublicKey());
                    }
                }
            }
            else
            {
                sendConnectNegotiation(0, loadBalanceToken);
                num = receiveConnectNegotiation();
                if (num != 0)
                {
                    throw new RDFatalException("Security negotiation failed!");
                }
            }

            Network.ConnectionStage = RDPClient.eConnectionStage.Establishing;
            IsoLayer.SendTPKT(sendConnectInitial(sendMcsData(true, Channels.RegisteredChannels.Count, num)));
            receiveConnectResponse();
            send_ErectDomainRequest();
            send_AttachUserRequest();
            RDPClient.McsUserID = receive_AttachUserConfirm();
            send_ChannelJoinRequest(RDPClient.McsUserID + MCS_USERCHANNEL_BASE);
            receive_ChannelJoinConfirm();
            send_ChannelJoinRequest(MSC_GLOBAL_CHANNEL);
            receive_ChannelJoinConfirm();
            foreach (IVirtualChannel channel in Channels.RegisteredChannels)
            {
                send_ChannelJoinRequest(channel.ChannelID);
                receive_ChannelJoinConfirm();
            }
            int num2 = 0x40;
            if (Secure.RDPEncrypted())
            {
                Network.ConnectionStage = RDPClient.eConnectionStage.SecureAndLogin;
                RdpPacket packet = Secure.establishKey();
                packet.Position = 0L;
                IsoLayer.SendMCS(packet, MSC_GLOBAL_CHANNEL);
                num2 |= SEC_ENCRYPT;
            }
            else
            {
                Network.ConnectionStage = RDPClient.eConnectionStage.Login;
            }
            IsoLayer.SendMCS_GlobalChannel(getLoginInfo(RDPClient.Domain, RDPClient.Username, RDPClient.Password, "", "", bAutoReconnect), num2);
        }

        internal static int send_connection_request_for_check(byte[] loadBalanceToken, bool bAutoReconnect)
        {
            sendConnectNegotiation(3, loadBalanceToken);
            return receiveConnectNegotiation();
        }

        internal static RdpPacket sendMcsData(bool use_rdp5, int num_channels, int serverSelectedProtocol)
        {
            RdpPacket packet = new RdpPacket();
            string clientName = RDPClient.ClientName;
            if (clientName.Length > 15)
            {
                clientName = clientName.Substring(0, 15);
            }
            int num = 2 * clientName.Length;
            int num2 = 0x9e;
            if (use_rdp5)
            {
                num2 += 0x60;
            }
            if (use_rdp5 && (num_channels > 0))
            {
                num2 += (num_channels * 12) + 8;
            }
            if ((RDPClient.serverNegotiateFlags & NegotiationFlags.EXTENDED_CLIENT_DATA_SUPPORTED) != ((NegotiationFlags)0))
            {
                num2 += 8;
            }
            packet.WriteBigEndian16((short)5);
            packet.WriteBigEndian16((short)20);
            packet.WriteByte(0x7c);
            packet.WriteBigEndian16((short)1);
            packet.WriteBigEndian16((short)(num2 | 0x8000));
            packet.WriteBigEndian16((short)8);
            packet.WriteBigEndian16((short)0x10);
            packet.WriteByte(0);
            packet.WriteLittleEndian16((ushort)0xc001);
            packet.WriteByte(0);
            packet.WriteLittleEndian32(0x61637544);
            packet.WriteBigEndian16((short)((num2 - 14) | 0x8000));
            packet.WriteLittleEndian16((ushort)0xc001);
            packet.WriteLittleEndian16(use_rdp5 ? ((short)0xd8) : ((short)0x88));
            packet.WriteLittleEndian16(use_rdp5 ? ((short)4) : ((short)1));
            packet.WriteLittleEndian16((short)8);
            packet.WriteLittleEndian16((short)RDPClient.width);
            packet.WriteLittleEndian16((short)RDPClient.height);
            packet.WriteLittleEndian16((ushort)0xca01);
            packet.WriteLittleEndian16((ushort)0xaa03);
            packet.WriteLittleEndian32(0x409);
            packet.WriteLittleEndian32(use_rdp5 ? 0xa28 : 0x1a3);
            packet.WriteUnicodeString(clientName);
            packet.Position += 30 - num;
            packet.WriteLittleEndian32(4);
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian32(12);
            packet.Position += 0x40L;
            packet.WriteLittleEndian16((ushort)0xca01);
            packet.WriteLittleEndian16(use_rdp5 ? ((short)1) : ((short)0));
            if (use_rdp5)
            {
                packet.WriteLittleEndian32(0);
                packet.WriteLittleEndian16((short)((byte)RDPClient.server_bpp));
                packet.WriteLittleEndian16((short)7);
                packet.WriteLittleEndian16((short)1);
                packet.Position += 0x40L;
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteLittleEndian32(serverSelectedProtocol);
                packet.WriteLittleEndian16((ushort)0xc004);
                packet.WriteLittleEndian16((short)12);
                int num3 = 13;

                if (((RDPClient.flags & HostFlags.ConsoleSession) != ((HostFlags)0)) || (RDPClient.sessionID != 0))
                {
                    num3 |= 2;
                }
                packet.WriteLittleEndian32(num3);
                packet.WriteLittleEndian32(RDPClient.sessionID);
            }
            packet.WriteLittleEndian16((ushort)0xc002);
            packet.WriteLittleEndian16(use_rdp5 ? ((short)12) : ((short)8));
            int num4 = 0;
            if (serverSelectedProtocol == 0)
            {
                num4 |= 3;
            }
            packet.WriteLittleEndian32(num4);
            if (use_rdp5)
            {
                packet.WriteLittleEndian32(0);
            }
            if (use_rdp5 && (num_channels > 0))
            {
                packet.WriteLittleEndian16((ushort)0xc003);
                packet.WriteLittleEndian16((short)((num_channels * 12) + 8));
                packet.WriteLittleEndian32(num_channels);
                foreach (IVirtualChannel channel in Channels.RegisteredChannels)
                {
                    packet.WriteString(channel.ChannelName, false);
                    packet.WriteByte(0);
                    packet.WriteBigEndian32((uint)0xc0a00000);
                }
            }
            if ((RDPClient.serverNegotiateFlags & NegotiationFlags.EXTENDED_CLIENT_DATA_SUPPORTED) != ((NegotiationFlags)0))
            {
                packet.WriteLittleEndian16((ushort)0xc006);
                packet.WriteLittleEndian16((short)8);
                packet.WriteLittleEndian32(0);
            }
            return packet;
        }

        private static RdpPacket getLoginInfo(string domain, string username, string password, string command, string directory, bool bAutoReconnect)
        {
            int num = 2 * "127.0.0.1".Length;
            int num2 = 2 * @"C:\WINNT\System32\mstscax.dll".Length;
            int num1 = _p;
            int num3 = 2 * domain.Length;
            int num4 = 2 * username.Length;
            int num5 = 2 * password.Length;
            int num6 = 2 * command.Length;
            int num7 = 2 * directory.Length;
            RdpPacket packet = new RdpPacket();
            int num8 = 0x213b;
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian32(num8);
            packet.WriteLittleEndian16((short)num3);
            packet.WriteLittleEndian16((short)num4);
            if ((num8 & 8) != 0)
            {
                packet.WriteLittleEndian16((short)num5);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
            }
            packet.WriteLittleEndian16((short)num6);
            packet.WriteLittleEndian16((short)num7);
            if (0 < num3)
            {
                packet.WriteUnicodeString(domain);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
            }
            packet.WriteUnicodeString(username);
            if ((num8 & 8) != 0)
            {
                packet.WriteUnicodeString(password);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
            }
            if (0 < num6)
            {
                packet.WriteUnicodeString(command);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
            }
            if (0 < num7)
            {
                packet.WriteUnicodeString(directory);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
            }
            packet.WriteLittleEndian16((short)2);
            packet.WriteLittleEndian16((short)(num + 2));
            packet.WriteUnicodeString("127.0.0.1");
            packet.WriteLittleEndian16((short)(num2 + 2));
            packet.WriteUnicodeString(@"C:\WINNT\System32\mstscax.dll");
            TimeZoneInfo info = TimeZoneInfo.Local;
            packet.WriteLittleEndian32((int)info.BaseUtcOffset.TotalMinutes);
            packet.WriteUnicodeString(info.StandardName);
            packet.Position += 0x3e - (2 * info.StandardName.Length);
            if (info.SupportsDaylightSavingTime)
            {
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((ushort)10);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)30);
                packet.WriteLittleEndian16((short)2);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian32(0);
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian32(0);
            }
            packet.WriteUnicodeString(info.DaylightName);
            packet.Position += 0x3e - (2 * info.DaylightName.Length);
            if (info.SupportsDaylightSavingTime)
            {
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((ushort)3);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0x1b);
                packet.WriteLittleEndian16((short)1);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian32((int)(info.BaseUtcOffset.TotalMinutes + 1.0));
            }
            else
            {
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian16((short)0);
                packet.WriteLittleEndian32(0);
            }
            packet.WriteLittleEndianU32(0);
            PerformanceFlags flags = (PerformanceFlags)0;
            if (!RDPClient.IsHostFlagSet(HostFlags.DesktopBackground))
            {
                flags |= PerformanceFlags.PERF_DISABLE_WALLPAPER;
            }
            if (RDPClient.IsHostFlagSet(HostFlags.FontSmoothing))
            {
                flags |= PerformanceFlags.PERF_ENABLE_FONT_SMOOTHING;
            }
            if (RDPClient.IsHostFlagSet(HostFlags.DesktopComposition))
            {
                flags |= PerformanceFlags.PERF_ENABLE_DESKTOP_COMPOSITION;
            }
            if (!RDPClient.IsHostFlagSet(HostFlags.ShowWindowContents))
            {
                flags |= PerformanceFlags.PERF_DISABLE_FULLWINDOWDRAG;
            }
            if (!RDPClient.IsHostFlagSet(HostFlags.MenuAnimation))
            {
                flags |= PerformanceFlags.PERF_DISABLE_MENUANIMATIONS;
            }
            if (!RDPClient.IsHostFlagSet(HostFlags.VisualStyles))
            {
                flags |= PerformanceFlags.PERF_DISABLE_THEMING;
            }
            packet.WriteLittleEndian32((int)flags);
            if (bAutoReconnect)
            {
                packet.WriteLittleEndian32(0x1c);
                packet.WriteLittleEndian32(0x1c);
                packet.WriteLittleEndian32(1);
                packet.WriteLittleEndian32(RDPClient.LogonID);
                HMACT64 hmact = new HMACT64(RDPClient.ReconnectCookie);
                hmact.update(Secure.GetClentRandom());
                byte[] buffer = hmact.digest();
                packet.Write(buffer, 0, buffer.Length);
                return packet;
            }
            packet.WriteLittleEndian32(0);
            return packet;
        }

        private static int domainParamSize(int data0, int data1, int data2, int data3)
        {
            int num = ((((((BERIntSize(data0) + BERIntSize(data1)) + BERIntSize(data2)) + BERIntSize(1)) + BERIntSize(0)) + BERIntSize(1)) + BERIntSize(data3)) + BERIntSize(2);
            return (c(0x30, num) + num);
        }

        private static void parseDomainParams(RdpPacket data)
        {
            int num = berParseHeader(data, BER_Header.TAG_DOMAIN_PARAMS);
            long num2 = data.Position + num;
            if (num2 > data.Length)
            {
                throw new RDFatalException("Bad domain param received");
            }
            data.Position += num;
        }

        private static void processMcsData(RdpPacket mcs_data)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            mcs_data.Position += 0x15L;
            if ((mcs_data.ReadByte() & 0x80) != 0)
            {
                mcs_data.ReadByte();
            }
            while (mcs_data.Position < mcs_data.Length)
            {
                num = mcs_data.getLittleEndian16();
                num2 = mcs_data.getLittleEndian16();
                if (num2 <= 4)
                {
                    return;
                }
                num3 = (int) ((mcs_data.Position + num2) - 4L);
                switch (((SRV) num))
                {
                    case SRV.SEC_TAG_SRV_INFO:
                        processSrvInfo(mcs_data);
                        break;

                    case SRV.SEC_TAG_SRV_CRYPT:
                        Secure.processCryptInfo(mcs_data);
                        break;

                    case SRV.SEC_TAG_SRV_3:
                        break;

                    case SRV.SEC_TAG_SRV_MSG_CHANNEL:
                        Channels.RegisteredChannels.Add(new NetworkCharacteristicsDetection(mcs_data.getLittleEndian16()));
                        break;

                    default:
                        throw new RDFatalException("MSC data incorrect tag " + num.ToString());
                }
                mcs_data.Position = num3;
            }
        }

        private static void processSrvInfo(RdpPacket mcs_data)
        {
            if (mcs_data.getLittleEndian16() == 1)
            {
                RDPClient.use_rdp5 = false;
            }
        }

        private static int receive_AttachUserConfirm()
        {
            int num;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            RdpPacket packet = ISO.ReceiveTPKTOrFastPath(out num);
            num2 = packet.ReadByte();
            if ((num2 >> 2) != AUCF)
            {
                throw new RDFatalException("Bad AUCF " + num2);
            }
            num3 = packet.ReadByte();
            if (num3 != 0)
            {
                throw new RDFatalException("Bad AURQ got " + num3);
            }
            if ((num2 & 2) != 0)
            {
                num4 = packet.ReadBigEndian16();
            }
            if (packet.Position != packet.Length)
            {
                throw new RDFatalException("Illegal Aucf packet length");
            }
            return num4;
        }

        private static void receive_ChannelJoinConfirm()
        {
            int num;
            int num2 = 0;
            int num3 = 0;
            RdpPacket packet = ISO.ReceiveTPKTOrFastPath(out num);
            num2 = packet.ReadByte();
            if ((num2 >> 2) != CJCF)
            {
                throw new RDFatalException("Bad CJCF " + num2);
            }
            num3 = packet.ReadByte();
            if (num3 != 0)
            {
                throw new RDFatalException("Bad CJRQ " + num3);
            }
            packet.Position += 4L;
            if ((num2 & 2) != 0)
            {
                packet.Position += 2L;
            }
            if (packet.Position != packet.Length)
            {
                throw new RDFatalException("Incorrect CJCF length");
            }
        }

        private static void sendConnectNegotiation(int NegotiationFlags, byte[] loadBalanceToken)
        {
            string domainAndUsername = RDPClient.DomainAndUsername;
            if (domainAndUsername.Length > 9)
            {
                domainAndUsername = domainAndUsername.Substring(0, 9);
            }
            RdpPacket packet = new RdpPacket();
            packet.WriteByte(3);
            packet.WriteByte(0);
            long position = packet.Position;
            packet.WriteBigEndian16((short)0);
            packet.WriteByte(0);
            packet.WriteByte(0xe0);
            packet.WriteBigEndian16((short)0);
            packet.WriteBigEndian16((short)0);
            packet.WriteByte(0);
            if (loadBalanceToken != null)
            {
                packet.Write(loadBalanceToken, 0, loadBalanceToken.Length);
                packet.WriteString("\r\n", false);
            }
            else
            {
                packet.WriteString("Cookie: mstshash=" + domainAndUsername + "\r\n", true);
            }
            packet.WriteByte(1);
            packet.WriteByte(0);
            packet.WriteLittleEndian16((short)8);
            packet.WriteLittleEndian32(NegotiationFlags);
            long num2 = packet.Position;
            packet.Position = position;
            packet.WriteBigEndian16((short)num2);
            packet.WriteByte((byte)(num2 - 5L));
            IsoLayer.Write(packet);
        }

        private static int receiveConnectNegotiation()
        {
            RdpPacket packet = ISO.Receive();
            //Print(packet);
            //System.Windows.Forms.MessageBox.Show("Test");

            packet.Position += 7L;
            if (packet.Position >= packet.Length)
            {
                return 0;
            }
            switch (packet.ReadByte())
            {
                case 2:
                    RDPClient.serverNegotiateFlags = (NegotiationFlags) packet.ReadByte();
                    packet.getLittleEndian16();
                    return packet.getLittleEndian32();

                case 3:
                    packet.ReadByte();
                    packet.getLittleEndian16();
                    switch (packet.getLittleEndian32())
                    {
                        case 1:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security with TLS 1.0");

                        case 2:
                            return 0x10000000;

                        case 3:
                            throw new RDFatalException("The server does not possess a valid authentication certificate and cannot initialize the External Security Protocol Provider");

                        case 4:
                            throw new RDFatalException("The list of requested security protocols is not consistent with the current security protocol in effect.");

                        case 5:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security with CredSSP");

                        case 6:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security and certificate-based client authentication");
                    }
                    throw new RDFatalException("Unknown Negotiation failure!");
            }

            throw new RDFatalException("Negotiation failed, requested security level not supported by server.");
        }

        private static void receiveConnectResponse()
        {
            string[] strArray = new string[] { "Successful", "Domain Merging", "Domain not Hierarchical", "No Such Channel", "No Such Domain", "No Such User", "Not Admitted", "Other User ID", "Parameters Unacceptable", "Token Not Available", "Token Not Possessed", "Too Many Channels", "Too Many Tokens", "Too Many Users", "Unspecified Failure", "User Rejected" };            
            RdpPacket data = ISO.Receive();
            data.ReadByte();
            int num = data.ReadByte();
            if (num != 240)
            {
                throw new RDFatalException("Bad connection response packet type " + num.ToString());
            }
            data.ReadByte();
            int index = 0;
            index = berParseHeader(data, BER_Header.CONNECT_RESPONSE);
            index = berParseHeader(data, BER_Header.BER_TAG_RESULT);
            index = data.ReadByte();

            if (index != 0)
            {
                throw new RDFatalException("MCS failed " + strArray[index].ToString());
            }
            index = berParseHeader(data, BER_Header.BER_TAG_INTEGER);
            index = data.ReadByte();
            parseDomainParams(data);
            index = berParseHeader(data, BER_Header.BER_TAG_OCTET_STRING);
            processMcsData(data);
        }

        private static void send_AttachUserRequest()
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte) (AURQ << 2));
            IsoLayer.SendTPKT(data);
        }

        private static void send_ChannelJoinRequest(int channelid)
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte) (CJRQ << 2));
            data.WriteBigEndian16((short)RDPClient.McsUserID);
            data.WriteBigEndian16((short) channelid);
            IsoLayer.SendTPKT(data);
        }

        private static void send_ErectDomainRequest()
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte) (EDRQ << 2));
            data.WriteBigEndian16((short) 1);
            data.WriteBigEndian16((short) 1);
            IsoLayer.SendTPKT(data);
        }

        private static void sendBerHeader(RdpPacket data0, BER_Header data1, int data2)
        {
            int num = (int) data1;
            if (num > 0xff)
            {
                data0.WriteBigEndian16((short) num);
            }
            else
            {
                data0.WriteByte((byte) num);
            }
            if (data2 >= 0x80)
            {
                data0.WriteByte(130);
                data0.WriteBigEndian16((short) data2);
            }
            else
            {
                data0.WriteByte((byte) data2);
            }
        }

        private static void sendBerInteger(RdpPacket buffer, int value)
        {
            int num = 1;
            if (value > 0xff)
            {
                num = 2;
            }
            sendBerHeader(buffer, BER_Header.BER_TAG_INTEGER, num);
            if (value > 0xff)
            {
                buffer.WriteBigEndian16((short) value);
            }
            else
            {
                buffer.WriteByte((byte) value);
            }
        }

        internal static RdpPacket sendConnectInitial(RdpPacket data)
        {
            int length = (int) data.Length;
            int num2 = ((((9 + domainParamSize(0x22, 2, 0, 0xffff)) + domainParamSize(1, 1, 1, 0x420)) + domainParamSize(0xffff, 0xfc17, 0xffff, 0xffff)) + 4) + length;
            RdpPacket packet = new RdpPacket();
            sendBerHeader(packet, BER_Header.CONNECT_INITIAL, num2);
            sendBerHeader(packet, BER_Header.BER_TAG_OCTET_STRING, 1);
            packet.WriteByte(1);
            sendBerHeader(packet, BER_Header.BER_TAG_OCTET_STRING, 1);
            packet.WriteByte(1);
            sendBerHeader(packet, BER_Header.BER_TAG_BOOLEAN, 1);
            packet.WriteByte(0xff);
            sendDomainParams(packet, 0x22, 2, 0, 0xffff);
            sendDomainParams(packet, 1, 1, 1, 0x420);
            sendDomainParams(packet, 0xffff, 0xffff, 0xffff, 0xffff);
            sendBerHeader(packet, BER_Header.BER_TAG_OCTET_STRING, length);
            packet.copyToByteArray(data);
            return packet;
        }

        private static void sendDomainParams(RdpPacket packet, int max_channels, int max_users, int max_tokens, int max_pdusize)
        {
            int num = ((((((BERIntSize(max_channels) + BERIntSize(max_users)) + BERIntSize(max_tokens)) + BERIntSize(1)) + BERIntSize(0)) + BERIntSize(1)) + BERIntSize(max_pdusize)) + BERIntSize(2);
            sendBerHeader(packet, BER_Header.TAG_DOMAIN_PARAMS, num);
            sendBerInteger(packet, max_channels);
            sendBerInteger(packet, max_users);
            sendBerInteger(packet, max_tokens);
            sendBerInteger(packet, 1);
            sendBerInteger(packet, 0);
            sendBerInteger(packet, 1);
            sendBerInteger(packet, max_pdusize);
            sendBerInteger(packet, 2);
        }

        // Вспомогательные методы
        internal static void Print(RdpPacket data)
        {
            data.Position = 0L;

            int count = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (count == 16)
                {
                    count = 0;

                    System.Diagnostics.Trace.Write(string.Format("0x{0:X02}", (short)data.ReadByte()).ToLower() + "\r\n");
                }
                else
                {
                    System.Diagnostics.Trace.Write(string.Format("0x{0:X02}", (short)data.ReadByte()).ToLower() + " ");
                }

                count++;
            }

            System.Diagnostics.Trace.Write("\r\n\r\n");
        }

        internal static void Print(byte[] data)
        {
            int count = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (count == 16)
                {
                    count = 0;

                    System.Diagnostics.Trace.Write(string.Format("0x{0:X02}", (short)data[i]).ToLower() + "\r\n");
                }
                else
                {
                    System.Diagnostics.Trace.Write(string.Format("0x{0:X02}", (short)data[i]).ToLower() + " ");
                }

                count++;
            }

            System.Diagnostics.Trace.Write("\r\n\r\n");
        }

        // Битовые флаги
        [Flags]
        internal enum BER_Header
        {
            BER_TAG_BOOLEAN = 1,
            BER_TAG_INTEGER = 2,
            BER_TAG_OCTET_STRING = 4,
            BER_TAG_RESULT = 10,
            CONNECT_INITIAL = 0x7f65,
            CONNECT_RESPONSE = 0x7f66,
            TAG_DOMAIN_PARAMS = 0x30
        }

        [Flags]
        private enum ClientInfoFlags
        {
            INFO_AUTOLOGON = 8,
            INFO_COMPRESSION = 0x80,
            INFO_DISABLECTRLALTDEL = 2,
            INFO_ENABLEWINDOWSKEY = 0x100,
            INFO_FORCE_ENCRYPTED_CS_PDU = 0x4000,
            INFO_LOGONERRORS = 0x10000,
            INFO_LOGONNOTIFY = 0x40,
            INFO_MAXIMIZESHELL = 0x20,
            INFO_MOUSE = 1,
            INFO_MOUSE_HAS_WHEEL = 0x20000,
            INFO_NOAUDIOPLAYBACK = 0x80000,
            INFO_PASSWORD_IS_SC_PIN = 0x40000,
            INFO_RAIL = 0x8000,
            INFO_REMOTECONSOLEAUDIO = 0x2000,
            INFO_UNICODE = 0x10,
            INFO_USING_SAVED_CREDS = 0x100000,
            RNS_INFO_AUDIOCAPTURE = 0x200000,
            RNS_INFO_VIDEO_DISABLE = 0x400000
        }

        [Flags]
        private enum ClusterFlags
        {
            REDIRECTED_SESSIONID_FIELD_VALID = 2,
            REDIRECTED_SMARTCARD = 0x40,
            REDIRECTION_SUPPORTED = 1,
            REDIRECTION_VERSION1 = 0,
            REDIRECTION_VERSION2 = 4,
            REDIRECTION_VERSION3 = 8,
            REDIRECTION_VERSION4 = 12,
            REDIRECTION_VERSION5 = 0x10,
            REDIRECTION_VERSION6 = 20
        }

        [Flags]
        private enum EncryptionLevel
        {
            ENCRYPTION_LEVEL_NONE,
            ENCRYPTION_LEVEL_LOW,
            ENCRYPTION_LEVEL_CLIENT_COMPATIBLE,
            ENCRYPTION_LEVEL_HIGH,
            ENCRYPTION_LEVEL_FIPS
        }

        [Flags]
        private enum EncryptionMethod
        {
            ENCRYPTION_METHOD_128BIT = 2,
            ENCRYPTION_METHOD_40BIT = 1,
            ENCRYPTION_METHOD_56BIT = 8,
            ENCRYPTION_METHOD_FIPS = 0x10,
            ENCRYPTION_METHOD_NONE = 0
        }

        [Flags]
        private enum NegotiationFailureCodes
        {
            HYBRID_REQUIRED_BY_SERVER = 5,
            INCONSISTENT_FLAGS = 4,
            SSL_CERT_NOT_ON_SERVER = 3,
            SSL_NOT_ALLOWED_BY_SERVER = 2,
            SSL_REQUIRED_BY_SERVER = 1,
            SSL_WITH_USER_AUTH_REQUIRED_BY_SERVER = 6
        }

        [Flags]
        internal enum NegotiationFlags
        {
            DYNVC_GFX_PROTOCOL_SUPPORTED = 2,
            EXTENDED_CLIENT_DATA_SUPPORTED = 1,
            RDP_NEGRSP_RESERVED = 4
        }

        [Flags]
        private enum NegotiationProtocol
        {
            FAILED = 0x10000000,
            PROTOCOL_HYBRID = 2,
            PROTOCOL_RDP = 0,
            PROTOCOL_SSL = 1
        }

        [Flags]
        private enum PerformanceFlags
        {
            PERF_DISABLE_CURSOR_SHADOW = 0x20,
            PERF_DISABLE_CURSORSETTINGS = 0x40,
            PERF_DISABLE_FULLWINDOWDRAG = 2,
            PERF_DISABLE_MENUANIMATIONS = 4,
            PERF_DISABLE_THEMING = 8,
            PERF_DISABLE_WALLPAPER = 1,
            PERF_ENABLE_DESKTOP_COMPOSITION = 0x100,
            PERF_ENABLE_FONT_SMOOTHING = 0x80,
            PERF_RESERVED1 = 0x10
        }

        [Flags]
        internal enum SRV
        {
            SEC_TAG_SRV_3 = 0xc03,
            SEC_TAG_SRV_CHANNELS = 0xc03,
            SEC_TAG_SRV_CRYPT = 0xc02,
            SEC_TAG_SRV_INFO = 0xc01,
            SEC_TAG_SRV_MSG_CHANNEL = 0xc04
        }

    }
}