using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class MCS
    {
        internal static int McsUserID;
        private static readonly int AUCF = 11;
        private static readonly int AURQ = 10;
        private static readonly int CJCF = 15;
        private static readonly int CJRQ = 14;
        internal static readonly int DPUM = 8;
        private static readonly int EDRQ = 1;
        internal static readonly int SDIN = 0x1a;
        internal static readonly int SDRQ = 0x19;
        internal static readonly int MCS_USERCHANNEL_BASE = 0x3e9; // 1001
        internal static readonly int MSC_GLOBAL_CHANNEL = 0x3eb; // 1003
        internal static List<int> serverSupportedChannels = new List<int>();

        /// <summary>
        /// Disconnect packet
        /// </summary>
        public static void Disconnect()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short) 1);
            packet.WriteLittleEndian16((short) 0x3ea);
            IsoLayer.SendPDU(packet, IsoLayer.PDUType2.PDUTYPE2_SHUTDOWN_REQUEST, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0);
            packet = new RdpPacket();
            packet.WriteByte((byte) (DPUM << 2));
            packet.WriteByte(3);

            IsoLayer.SendTPKT(packet);
        }

        /// <summary>
        /// Negotiation Start
        /// </summary>
        /// <param name="loadBalanceToken">null</param>
        /// <param name="bAutoReconnect">false</param>
        internal static void sendСonnectionRequest(byte[] loadBalanceToken, bool bAutoReconnect)
        {
            int num;
            Secure.dec_count = 0;
            Secure.enc_count = 0;
            Network.ConnectionStage = Network.eConnectionStage.Negotiating;

            if (Options.enableNLA)
            {
                // Client X.224 Connection Request PDU
                sendConnectNegotiation(
                    NegotiationProtocol.PROTOCOL_RDP |
                    NegotiationProtocol.PROTOCOL_SSL |
                    NegotiationProtocol.PROTOCOL_HYBRID,
                    loadBalanceToken);

                // Server X.224 Connection Confirm PDU
                num = receiveConnectNegotiation();

                if (num == Main.SecureValue3) // SSL подключение запрещено настройками сервера
                {
                    Network.Close();
                    Network.Connect(Options.Host, Options.Port);

                    // Client X.224 Connection Request PDU
                    sendConnectNegotiation(NegotiationProtocol.PROTOCOL_RDP, loadBalanceToken);

                    // Server X.224 Connection Confirm PDU
                    num = receiveConnectNegotiation();

                    if (num != 0)
                    {
                        throw new RDFatalException("Security negotiation failed!");
                    }
                }
                else // SSL подключение разрешено
                {
                    if (((num & 1) != 0) || ((num & 2) != 0))
                    {
                        Network.ConnectionStage = Network.eConnectionStage.Securing;
                        Network.ConnectSSL();
                    }

                    if ((num & 2) != 0)
                    {
                        Network.ConnectionStage = Network.eConnectionStage.Authenticating;
                        CredSSP.Negotiate(Network.GetSSLPublicKey());
                    }
                }
            }
            else
            {
                // Client X.224 Connection Request PDU
                sendConnectNegotiation(NegotiationProtocol.PROTOCOL_RDP, loadBalanceToken);

                // Server X.224 Connection Confirm PDU
                num = receiveConnectNegotiation();

                if (num != 0)
                {
                    throw new RDFatalException("Security negotiation failed!");
                }
            }

            Network.ConnectionStage = Network.eConnectionStage.Establishing;

            // Client MCS Connect Initial PDU
            IsoLayer.SendTPKT(sendConnectInitial(sendMcsData(true, Channels.RegisteredChannels.Count, num)));

            // Server MCS Connect Response PDU with GCC Conference Create Response
            receiveConnectResponse();

            // Client MCS Erect Domain Request PDU
            send_ErectDomainRequest();

            // Client MCS Attach User Request PDU
            send_AttachUserRequest();

            // Server MCS Attach User Confirm PDU
            McsUserID = receive_AttachUserConfirm();

            // Open User channel
            send_ChannelJoinRequest(McsUserID + MCS_USERCHANNEL_BASE); // Client MCS Channel Join Request PDU
            receive_ChannelJoinConfirm(); // Server MCS Channel Join Confirm PDU

            // Open Global channel
            send_ChannelJoinRequest(MSC_GLOBAL_CHANNEL);
            receive_ChannelJoinConfirm();

            // Open over channels
            foreach (var channel in Channels.RegisteredChannels)
            {
                if (serverSupportedChannels.Contains(channel.ChannelID))
                {
                    send_ChannelJoinRequest(channel.ChannelID);
                    receive_ChannelJoinConfirm();
                    Debug.WriteLine("Client open over channel: " + channel.ChannelID.ToString());
                }
            }

            int num2 = 0x40;

            if (Secure.RDPEncrypted())
            {
                Network.ConnectionStage = Network.eConnectionStage.SecureAndLogin;
                RdpPacket packet = Secure.establishKey();
                packet.Position = 0L;
                IsoLayer.SendMCS(packet, MSC_GLOBAL_CHANNEL);
                num2 |= 8;
            }
            else
            {
                Network.ConnectionStage = Network.eConnectionStage.Login;
            }

            // Client Info PDU
            IsoLayer.SendToGlobalChannel(getLoginInfo(Options.Domain, Options.Username, Options.Password, "", "", bAutoReconnect), num2);
        }

        /// <summary>
        /// Client X.224 Connection Request PDU
        /// </summary>
        private static void sendConnectNegotiation(NegotiationProtocol NegotiationFlags, byte[] loadBalanceToken)
        {
            string domainAndUsername = Options.DomainAndUsername;

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

            // RDP Negotiation Request
            packet.WriteByte(0x01);
            packet.WriteByte(0);
            packet.WriteLittleEndian16((short)8);
            packet.WriteLittleEndian32((int)NegotiationFlags); // Standard RDP Security, TLS 1.0, CredSSP

            long num2 = packet.Position;
            packet.Position = position;
            packet.WriteBigEndian16((short)num2);
            packet.WriteByte((byte)(num2 - 5L));

            IsoLayer.Write(packet);
        }

        /// <summary>
        /// Server X.224 Connection Confirm PDU
        /// </summary>
        private static int receiveConnectNegotiation()
        {
            RdpPacket packet = ISO.Receive();
            packet.Position += 7L;

            if (packet.Position >= packet.Length)
            {
                return 0;
            }

            switch (packet.ReadByte())
            {
                // TYPE_RDP_NEG_RSP
                case 0x02:
                    Options.serverNegotiateFlags = (NegotiationFlags)packet.ReadByte();
                    packet.ReadLittleEndian16();
                    return packet.ReadLittleEndian32();

                // TYPE_RDP_NEG_FAILURE
                case 0x03:
                    packet.ReadByte();
                    packet.ReadLittleEndian16();

                    switch ((NegotiationFailureCodes)packet.ReadLittleEndian32())
                    {
                        case NegotiationFailureCodes.SSL_REQUIRED_BY_SERVER:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security with TLS 1.0");

                        case NegotiationFailureCodes.SSL_NOT_ALLOWED_BY_SERVER:
                            return 0x10000000;

                        case NegotiationFailureCodes.SSL_CERT_NOT_ON_SERVER:
                            throw new RDFatalException("The server does not possess a valid authentication certificate and cannot initialize the External Security Protocol Provider");

                        case NegotiationFailureCodes.INCONSISTENT_FLAGS:
                            throw new RDFatalException("The list of requested security protocols is not consistent with the current security protocol in effect.");

                        case NegotiationFailureCodes.HYBRID_REQUIRED_BY_SERVER:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security with CredSSP");

                        case NegotiationFailureCodes.SSL_WITH_USER_AUTH_REQUIRED_BY_SERVER:
                            throw new RDFatalException("The server requires that the client support Enhanced RDP Security and certificate-based client authentication");
                    }

                    throw new RDFatalException("Unknown Negotiation failure!");
            }

            throw new RDFatalException("Negotiation failed, requested security level not supported by server.");
        }
        
        /// <summary>
        /// Server MCS Connect Response PDU with GCC Conference Create Response
        /// Part 1
        /// </summary>
        private static void receiveConnectResponse()
        {
            string[] strArray = new string[] 
            { 
                "Successful", 
                "Domain Merging", 
                "Domain not Hierarchical", 
                "No Such Channel", 
                "No Such Domain", 
                "No Such User", 
                "Not Admitted", 
                "Other User ID", 
                "Parameters Unacceptable", 
                "Token Not Available", 
                "Token Not Possessed", 
                "Too Many Channels", 
                "Too Many Tokens", 
                "Too Many Users", 
                "Unspecified Failure", 
                "User Rejected" 
            };

            RdpPacket data = ISO.Receive();
            data.ReadByte();
            int num = data.ReadByte();

            if (num != 240)
            {
                throw new RDFatalException("Bad connection response packet type " + num.ToString());
            }

            data.ReadByte();
            int index = 0;
            index = BER.berParseHeader(data, BER.BER_Header.CONNECT_RESPONSE);
            index = BER.berParseHeader(data, BER.BER_Header.BER_TAG_RESULT);
            index = data.ReadByte();

            if (index != 0)
            {
                throw new RDFatalException("MCS failed " + strArray[index].ToString());
            }

            index = BER.berParseHeader(data, BER.BER_Header.BER_TAG_INTEGER);
            index = data.ReadByte();
            parseDomainParams(data);
            index = BER.berParseHeader(data, BER.BER_Header.BER_TAG_OCTET_STRING);
            processMcsData(data);
        }

        /// <summary>
        /// Server MCS Connect Response PDU with GCC Conference Create Response
        /// Part 1.1
        /// </summary>
        private static void parseDomainParams(RdpPacket data)
        {
            int num = BER.berParseHeader(data, BER.BER_Header.TAG_DOMAIN_PARAMS);
            long num2 = data.Position + num;

            if (num2 > data.Length)
            {
                throw new RDFatalException("Bad domain param received");
            }

            data.Position += num;
        }

        /// <summary>
        /// Server MCS Connect Response PDU with GCC Conference Create Response
        /// Part 2.
        ///
        /// serverCoreData
        /// serverSecurityData
        /// serverNetworkData
        /// serverMessageChannelData
        /// </summary>
        private static void processMcsData(RdpPacket mcsData)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            mcsData.Position += 0x15L;

            if ((mcsData.ReadByte() & 0x80) != 0)
            {
                mcsData.ReadByte();
            }

            while (mcsData.Position < mcsData.Length)
            {
                num = mcsData.ReadLittleEndian16();
                num2 = mcsData.ReadLittleEndian16();

                if (num2 <= 4)
                {
                    return;
                }

                num3 = (int)((mcsData.Position + num2) - 4L);

                switch ((SERVER)num)
                {
                    case SERVER.SC_CORE:
                        processSrvCoreInfo(mcsData);
                        break;

                    case SERVER.SC_SECURITY:
                        Secure.processCryptInfo(mcsData);
                        break;

                    case SERVER.SC_NET:
                        processSrvNetInfo(mcsData);
                        break;

                    case SERVER.SC_MCS_MSGCHANNEL:
                        int channel = mcsData.ReadLittleEndian16();
                        Debug.WriteLine("Network Characteristics Detection channel: " + channel);
                        //Channels.RegisteredChannels.Add(new NetworkCharacteristicsDetection(channel));
                        break;

                    default:
                        throw new RDFatalException("MSC data incorrect tag " + num.ToString());
                }

                mcsData.Position = num3;
            }
        }

        /// <summary>
        /// Server MCS Connect Response PDU with GCC Conference Create Response
        /// Part 2.1
        /// 
        /// Rdp 5 supported
        /// </summary>
        private static void processSrvCoreInfo(RdpPacket data)
        {
            // 0x00080001 - RDP 4.0 servers
            // 0x00080004 - RDP 5.0, 5.1, 5.2, 6.0, 6.1, 7.0, 7.1, and 8.0 servers
            if (data.ReadLittleEndian32() == 0x00080001)
            {
                Options.use_rdp5 = false;
            }
        }

        /// <summary>
        /// Server MCS Connect Response PDU with GCC Conference Create Response
        /// Part 2.2
        /// 
        /// I/O channels server supported
        /// </summary>
        private static void processSrvNetInfo(RdpPacket data)
        {
            int MCSChannelId = data.ReadLittleEndian16();
            Debug.WriteLine("Server support channel: " + MCSChannelId.ToString()); // MCSChannelId = 1003

            serverSupportedChannels.Clear();
            int channelCount = data.ReadLittleEndian16(); // channelCount

            // Проверяем каналы, поддерживаемые сервером
            for (int i = 0; i < channelCount; i++)
            {
                int channelId = data.ReadLittleEndian16();
                serverSupportedChannels.Add(channelId);

                Debug.WriteLine("Server support channel: " + channelId.ToString());
            }
        }

        /// <summary>
        /// Client MCS Connect Initial PDU
        /// Part 1
        /// 
        /// BER Encode packet and send
        /// </summary>
        internal static RdpPacket sendConnectInitial(RdpPacket data)
        {
            int length = (int)data.Length;
            int num2 = ((((9 + BER.domainParamSize(0x22, 2, 0, 0xffff))
                + BER.domainParamSize(1, 1, 1, 0x420))
                + BER.domainParamSize(0xffff, 0xfc17, 0xffff, 0xffff)) + 4) + length;

            RdpPacket packet = new RdpPacket();
            BER.sendBerHeader(packet, BER.BER_Header.CONNECT_INITIAL, num2);
            BER.sendBerHeader(packet, BER.BER_Header.BER_TAG_OCTET_STRING, 1);
            packet.WriteByte(1);
            BER.sendBerHeader(packet, BER.BER_Header.BER_TAG_OCTET_STRING, 1);
            packet.WriteByte(1);
            BER.sendBerHeader(packet, BER.BER_Header.BER_TAG_BOOLEAN, 1);
            packet.WriteByte(0xff);

            sendDomainParams(packet, 0x22, 2, 0, 0xffff);
            sendDomainParams(packet, 1, 1, 1, 0x420);
            sendDomainParams(packet, 0xffff, 0xffff, 0xffff, 0xffff);

            BER.sendBerHeader(packet, BER.BER_Header.BER_TAG_OCTET_STRING, length);
            packet.copyToByteArray(data);

            return packet;
        }

        /// <summary>
        /// Client MCS Connect Initial PDU
        /// Part 1.1
        /// 
        /// BER Encode packet
        /// </summary>
        private static void sendDomainParams(RdpPacket packet, int max_channels, int max_users, int max_tokens, int max_pdusize)
        {
            int num = ((((((BER.BERIntSize(max_channels) + BER.BERIntSize(max_users))
                + BER.BERIntSize(max_tokens)) + BER.BERIntSize(1)) + BER.BERIntSize(0))
                + BER.BERIntSize(1)) + BER.BERIntSize(max_pdusize)) + BER.BERIntSize(2);

            BER.sendBerHeader(packet, BER.BER_Header.TAG_DOMAIN_PARAMS, num);
            BER.sendBerInteger(packet, max_channels);
            BER.sendBerInteger(packet, max_users);
            BER.sendBerInteger(packet, max_tokens);
            BER.sendBerInteger(packet, 1);
            BER.sendBerInteger(packet, 0);
            BER.sendBerInteger(packet, 1);
            BER.sendBerInteger(packet, max_pdusize);
            BER.sendBerInteger(packet, 2);
        }

        /// <summary>
        /// Client MCS Connect Initial PDU
        /// Part 2
        /// 
        /// Create packet
        /// </summary>
        internal static RdpPacket sendMcsData(bool useRdp5, int num_channels, int serverSelectedProtocol)
        {
            RdpPacket packet = new RdpPacket();

            // Проверка длины Client Name
            string clientName = Options.ClientName;

            if (clientName.Length > 15)
            {
                clientName = clientName.Substring(0, 15);
            }

            int num = 2 * clientName.Length;
            int num2 = 0x9e;

            if (useRdp5)
            {
                num2 += 0x60;
            }

            if (useRdp5 && (num_channels > 0))
            {
                num2 += (num_channels * 12) + 8;
            }

            if (Options.serverNegotiateFlags.HasFlag(NegotiationFlags.EXTENDED_CLIENT_DATA_SUPPORTED))
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

            // Client Core Data (TS_UD_CS_CORE)
            packet.WriteLittleEndian16((ushort)CLIENT.CS_CORE);
            packet.WriteLittleEndian16(useRdp5 ? ((short)0xd8) : ((short)0x88));
            packet.WriteLittleEndian16(useRdp5 ? ((short)4) : ((short)1));
            packet.WriteLittleEndian16((short)8);
            packet.WriteLittleEndian16((short)Options.width); // Width
            packet.WriteLittleEndian16((short)Options.height); // Height
            packet.WriteLittleEndian16((ushort)0xca01);
            packet.WriteLittleEndian16((ushort)0xaa03);
            packet.WriteLittleEndian32(Options.Keyboard); // Клавиатура
            packet.WriteLittleEndian32(useRdp5 ? 0xa28 : 0x1a3); // Client Build
            packet.WriteUnicodeString(clientName); // Client Name
            packet.Position += 30 - num;
            packet.WriteLittleEndian32(0x00000004); // IBM enhanced (101- or 102-key) keyboard
            packet.WriteLittleEndian32(0);
            packet.WriteLittleEndian32(12); // Функциональные клавиши (F1-F12)
            packet.Position += 0x40L;
            packet.WriteLittleEndian16((ushort)0xCA01); // NS_UD_COLOR_8BPP
            packet.WriteLittleEndian16(useRdp5 ? ((short)1) : ((short)0));

            if (useRdp5)
            {
                packet.WriteLittleEndian32(0);
                packet.WriteLittleEndian16((short)((byte)Options.server_bpp));
                packet.WriteLittleEndian16((short)7);
                packet.WriteLittleEndian16((short)1);
                packet.Position += 0x40L;
                packet.WriteByte(0);
                packet.WriteByte(0);
                packet.WriteLittleEndian32(serverSelectedProtocol);

                // Client Cluster Data (TS_UD_CS_CLUSTER)
                packet.WriteLittleEndian16((ushort)CLIENT.CS_CLUSTER);
                packet.WriteLittleEndian16((short)12);
                int num3 = 13;

                if (Options.flags.HasFlag(HostFlags.ConsoleSession) || (Options.sessionID != 0))
                {
                    num3 |= 2;
                }

                packet.WriteLittleEndian32(num3);
                packet.WriteLittleEndian32(Options.sessionID);
            }

            // Client Security Data (TS_UD_CS_SEC)
            packet.WriteLittleEndian16((ushort)CLIENT.CS_SECURITY);
            packet.WriteLittleEndian16(useRdp5 ? ((short)12) : ((short)8));

            int num4 = 0;
            if (serverSelectedProtocol == 0)
            {
                num4 |= 3;
            }

            packet.WriteLittleEndian32(num4);

            if (useRdp5)
            {
                packet.WriteLittleEndian32(0);
            }

            // Client Network Data (TS_UD_CS_NET)
            if (useRdp5 && (num_channels > 0))
            {
                packet.WriteLittleEndian16((ushort)CLIENT.CS_NET);
                packet.WriteLittleEndian16((short)((num_channels * 12) + 8));
                packet.WriteLittleEndian32(num_channels);

                foreach (IVirtualChannel channel in Channels.RegisteredChannels)
                {
                    Debug.WriteLine("Client Network Data. Channel name length: " + channel.ChannelName.Length);

                    packet.WriteString(channel.ChannelName, false);
                    packet.WriteBigEndian32((uint)(CHANNEL_DEF.CHANNEL_OPTION_INITIALIZED));
                }
            }

            // Client Message Channel Data (TS_UD_CS_MCS_MSGCHANNEL)
            if (Options.serverNegotiateFlags.HasFlag(NegotiationFlags.EXTENDED_CLIENT_DATA_SUPPORTED))
            {
                packet.WriteLittleEndian16((ushort)CLIENT.CS_MCS_MSGCHANNEL);
                packet.WriteLittleEndian16((short)8);
                packet.WriteLittleEndian32(0);
            }

            return packet;
        }

        /// <summary>
        /// Client MCS Erect Domain Request PDU
        /// </summary>
        private static void send_ErectDomainRequest()
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte)(EDRQ << 2));
            data.WriteBigEndian16((short)1);
            data.WriteBigEndian16((short)1);
            IsoLayer.SendTPKT(data);
        }

        /// <summary>
        /// Client MCS Attach User Request PDU
        /// </summary>
        private static void send_AttachUserRequest()
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte)(AURQ << 2));
            IsoLayer.SendTPKT(data);
        }

        /// <summary>
        /// Server MCS Attach User Confirm PDU
        /// </summary>
        private static int receive_AttachUserConfirm()
        {
            int num, num2, num3, num4 = 0;
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

        /// <summary>
        /// Client MCS Channel Join Request PDU
        /// </summary>
        private static void send_ChannelJoinRequest(int channelId)
        {
            RdpPacket data = new RdpPacket();
            data.WriteByte((byte)(CJRQ << 2));
            data.WriteBigEndian16((short)McsUserID);
            data.WriteBigEndian16((short)channelId);
            IsoLayer.SendTPKT(data);
        }

        /// <summary>
        /// Server MCS Channel Join Confirm PDU
        /// </summary>
        private static void receive_ChannelJoinConfirm()
        {
            int num, num2, num3 = 0;
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

        /// <summary>
        /// Client Info PDU
        /// </summary>
        private static RdpPacket getLoginInfo(string domain, string username, string password, string command, string directory, bool bAutoReconnect)
        {
            int num1 = 2 * "127.0.0.1".Length;
            int num2 = 2 * @"C:\WINNT\System32\mstscax.dll".Length;
            int num3 = 2 * domain.Length;
            int num4 = 2 * username.Length;
            int num5 = 2 * password.Length;
            int num6 = 2 * command.Length;
            int num7 = 2 * directory.Length;

            //int num8 = 0x213b;

            int num8 = (int)(
                ClientInfoFlags.INFO_AUTOLOGON |
                ClientInfoFlags.INFO_DISABLECTRLALTDEL |
                ClientInfoFlags.INFO_LOGONERRORS |
                ClientInfoFlags.INFO_LOGONNOTIFY |
                ClientInfoFlags.INFO_ENABLEWINDOWSKEY |
                ClientInfoFlags.INFO_MOUSE |
                ClientInfoFlags.INFO_NOAUDIOPLAYBACK |
                ClientInfoFlags.INFO_UNICODE);

            RdpPacket packet = new RdpPacket();
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
            packet.WriteLittleEndian16((short)(num1 + 2));
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

            if (!Options.IsHostFlagSet(HostFlags.DesktopBackground))
            {
                flags |= PerformanceFlags.PERF_DISABLE_WALLPAPER;
            }

            if (Options.IsHostFlagSet(HostFlags.FontSmoothing))
            {
                flags |= PerformanceFlags.PERF_ENABLE_FONT_SMOOTHING;
            }

            if (Options.IsHostFlagSet(HostFlags.DesktopComposition))
            {
                flags |= PerformanceFlags.PERF_ENABLE_DESKTOP_COMPOSITION;
            }

            if (!Options.IsHostFlagSet(HostFlags.ShowWindowContents))
            {
                flags |= PerformanceFlags.PERF_DISABLE_FULLWINDOWDRAG;
            }

            if (!Options.IsHostFlagSet(HostFlags.MenuAnimation))
            {
                flags |= PerformanceFlags.PERF_DISABLE_MENUANIMATIONS;
            }

            if (!Options.IsHostFlagSet(HostFlags.VisualStyles))
            {
                flags |= PerformanceFlags.PERF_DISABLE_THEMING;
            }

            packet.WriteLittleEndian32((int)flags);

            if (bAutoReconnect)
            {
                packet.WriteLittleEndian32(0x1c);
                packet.WriteLittleEndian32(0x1c);
                packet.WriteLittleEndian32(1);
                packet.WriteLittleEndian32(Options.LogonID);
                HMACT64 hmact = new HMACT64(Options.ReconnectCookie);
                hmact.update(Secure.GetClentRandom());
                byte[] buffer = hmact.digest();
                packet.Write(buffer, 0, buffer.Length);
                return packet;
            }

            packet.WriteLittleEndian32(0);

            return packet;
        }
        
        // Битовые флаги
        [Flags]
        private enum NegotiationProtocol
        {
            PROTOCOL_RDP = 0x00000000,
            PROTOCOL_SSL = 0x00000001,
            PROTOCOL_HYBRID = 0x00000002
        }

        [Flags]
        internal enum NegotiationFlags
        {
            EXTENDED_CLIENT_DATA_SUPPORTED = 0x01,
            DYNVC_GFX_PROTOCOL_SUPPORTED = 0x02,
            NEGRSP_FLAG_RESERVED = 0x04,
            RESTRICTED_ADMIN_MODE_SUPPORTED = 0x08
        }

        [Flags]
        private enum ClientInfoFlags
        {
            INFO_MOUSE = 0x00000001,
            INFO_DISABLECTRLALTDEL = 0x00000002,
            INFO_AUTOLOGON = 0x00000008,
            INFO_UNICODE = 0x00000010,
            INFO_MAXIMIZESHELL = 0x00000020,
            INFO_LOGONNOTIFY = 0x00000040,
            INFO_COMPRESSION = 0x00000080,
            CompressionTypeMask = 0x00001E00,
            INFO_ENABLEWINDOWSKEY = 0x00000100,
            INFO_REMOTECONSOLEAUDIO = 0x00002000,
            INFO_FORCE_ENCRYPTED_CS_PDU = 0x00004000,
            INFO_RAIL = 0x00008000,
            INFO_LOGONERRORS = 0x00010000,
            INFO_MOUSE_HAS_WHEEL = 0x00020000,
            INFO_PASSWORD_IS_SC_PIN = 0x00040000,
            INFO_NOAUDIOPLAYBACK = 0x00080000,
            INFO_USING_SAVED_CREDS = 0x00100000,
            RNS_INFO_AUDIOCAPTURE = 0x00200000,
            RNS_INFO_VIDEO_DISABLE = 0x00400000
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
        internal enum EncryptionLevel
        {
            ENCRYPTION_LEVEL_NONE = 0x00000000,
            ENCRYPTION_LEVEL_LOW = 0x00000001,
            ENCRYPTION_LEVEL_CLIENT_COMPATIBLE = 0x00000002,
            ENCRYPTION_LEVEL_HIGH = 0x00000003,
            ENCRYPTION_LEVEL_FIPS = 0x00000004
        }

        [Flags]
        internal enum EncryptionMethod
        {
            NCRYPTION_METHOD_NONE = 0x00000000,
            ENCRYPTION_METHOD_40BIT = 0x00000001,
            ENCRYPTION_METHOD_128BIT = 0x00000002,
            ENCRYPTION_METHOD_56BIT = 0x00000008,
            ENCRYPTION_METHOD_FIPS = 0x00000010
        }

        [Flags]
        private enum NegotiationFailureCodes
        {
            SSL_REQUIRED_BY_SERVER = 0x00000001,
            SSL_NOT_ALLOWED_BY_SERVER = 0x00000002,
            SSL_CERT_NOT_ON_SERVER = 0x00000003,
            INCONSISTENT_FLAGS = 0x00000004,
            HYBRID_REQUIRED_BY_SERVER = 0x00000005,
            SSL_WITH_USER_AUTH_REQUIRED_BY_SERVER = 0x00000006
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
        internal enum SERVER
        {
            SC_CORE = 0x0C01,
            SC_SECURITY = 0x0C02,
            SC_NET = 0x0C03,
            SC_MCS_MSGCHANNEL = 0x0C04,
            SC_MULTITRANSPORT = 0x0C08
        }

        [Flags]
        internal enum CLIENT
        {
            CS_CORE = 0xC001,
            CS_SECURITY = 0xC002,
            CS_NET = 0xC003,
            CS_CLUSTER = 0xC004,
            CS_MONITOR = 0xC005,
            CS_MCS_MSGCHANNEL = 0xC006,
            CS_MONITOR_EX = 0xC008,
            CS_MULTITRANSPORT = 0xC00A
        }

        [Flags]
        internal enum CHANNEL_DEF : uint
        {
            CHANNEL_OPTION_INITIALIZED = 0x80000000,
            CHANNEL_OPTION_ENCRYPT_RDP = 0x40000000,
            CHANNEL_OPTION_ENCRYPT_SC = 0x20000000,
            CHANNEL_OPTION_ENCRYPT_CS = 0x10000000,
            CHANNEL_OPTION_PRI_HIGH = 0x08000000,
            CHANNEL_OPTION_PRI_MED = 0x04000000,
            CHANNEL_OPTION_PRI_LOW = 0x02000000,
            CHANNEL_OPTION_COMPRESS_RDP = 0x00800000,
            CHANNEL_OPTION_COMPRESS = 0x00400000,
            CHANNEL_OPTION_SHOW_PROTOCOL = 0x00200000,
            REMOTE_CONTROL_PERSISTENT = 0x00100000
        }

        [Flags]
        internal enum TS_SECURITY_HEADER
        {
            SEC_EXCHANGE_PKT = 0x0001,
            SEC_TRANSPORT_REQ = 0x0002,
            RDP_SEC_TRANSPORT_RSP = 0x0004,
            SEC_ENCRYPT = 0x0008,
            SEC_RESET_SEQNO = 0x0010,
            SEC_IGNORE_SEQNO = 0x0020,
            SEC_INFO_PKT = 0x0040,
            SEC_LICENSE_PKT = 0x0080,
            SEC_LICENSE_ENCRYPT_CS = 0x0200,
            SEC_LICENSE_ENCRYPT_SC = 0x0200,
            SEC_REDIRECTION_PKT = 0x0400,
            SEC_SECURE_CHECKSUM = 0x0800,
            SEC_AUTODETECT_REQ = 0x1000,
            SEC_AUTODETECT_RSP = 0x2000,
            SEC_HEARTBEAT = 0x4000,
            SEC_FLAGSHI_VALID = 0x8000
        }

    }
}