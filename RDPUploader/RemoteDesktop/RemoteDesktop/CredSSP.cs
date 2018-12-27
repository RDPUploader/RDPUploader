using System;
using System.Text;

namespace RemoteDesktop
{
    public class CredSSP : ASN1
    {
        public static void Negotiate(byte[] ServerPublicKey)
        {
            try
            {
                ASN1.Init();
                RDPClient.m_bAuthenticated = false;
                SendNegotiate();
                while (!RDPClient.m_bAuthenticated)
                {
                    ProcesssResponse(Receive(), ServerPublicKey);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Authentication Error (" + exception.Message + ")", exception); // Ошибка авторизации
            }
        }

        private static void SendNegotiate()
        {
            SendTSRequest(WriteNegoToken(), null, null);
        }

        private static void SendTSRequest(RdpPacket negoTokens, byte[] auth_info, byte[] pub_key_auth)
        {
            RdpPacket packet = new RdpPacket();
            ASN1.WriteTag(packet, ASN1.SequenceTag(0), "TSRequest");
            ASN1.WriteTag(packet, ASN1.ContextTag(0), "CTX_Version");
            ASN1.WriteInteger(packet, 2);
            ASN1.CloseTag(packet, "CTX_Version");

            if (negoTokens != null)
            {
                ASN1.WriteTag(packet, ASN1.ContextTag(1), "CTX_NegTokens");
                ASN1.WriteTag(packet, ASN1.SequenceTag(0), "NegTokens");
                ASN1.WriteTag(packet, ASN1.SequenceTag(0), "NegTokens2");
                ASN1.WriteTag(packet, ASN1.ContextTag(0), "CTX_OctetString");
                ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctetString");
                packet.copyToByteArray(negoTokens);
                ASN1.CloseTag(packet, "OctetString");
                ASN1.CloseTag(packet, "CTX_OctetString");
                ASN1.CloseTag(packet, "NegTokens2");
                ASN1.CloseTag(packet, "NegTokens");
                ASN1.CloseTag(packet, "CTX_NegTokens");
            }

            if (auth_info != null)
            {
                ASN1.WriteTag(packet, ASN1.ContextTag(2), "CTX_AuthInfo");
                ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctetString");
                packet.Write(auth_info, 0, auth_info.Length);
                ASN1.CloseTag(packet, "OctetString");
                ASN1.CloseTag(packet, "CTX_AuthInfo");
            }

            if (pub_key_auth != null)
            {
                ASN1.WriteTag(packet, ASN1.ContextTag(3), "CTX_PubKeyAuth");
                ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctetString");
                packet.Write(pub_key_auth, 0, pub_key_auth.Length);
                ASN1.CloseTag(packet, "OctetString");
                ASN1.CloseTag(packet, "CTX_PubKeyAuth");
            }

            ASN1.CloseTag(packet, "TSRequest");

            Send(packet);
        }

        private static void Send(RdpPacket packet)
        {
            packet.Position = 0L;
            byte[] buffer = new byte[packet.Length];
            packet.Read(buffer, 0, (int)packet.Length);
            Network.Send(buffer);
        }

        public static RdpPacket WriteNegoToken()
        {
            RDPClient.m_NTLMAuthenticate = new NTLM(Network.OpenSocket, RDPClient.Username, RDPClient.Password, RDPClient.Domain);
            byte[] buffer = RDPClient.m_NTLMAuthenticate.Negotiate();
            RdpPacket packet = new RdpPacket();
            packet.Write(buffer, 0, buffer.Length);
            packet.Position = 0L;
            return packet;
        }

        private static void ProcesssResponse(RdpPacket packet, byte[] ServerPublicKey)
        {
            ASN1.ReadTag(packet, ASN1.SequenceTag(0), "TSRequest");
            ASN1.ReadTag(packet, ASN1.ContextTag(0), "CTX_Version");
            if (ASN1.ReadInteger(packet) < 2)
            {
                throw new Exception("TSRequest version not supported!");
            }
            ASN1.CloseTag(packet, "CTX_Version");
            byte[] buffer = null;
            int num2 = ASN1.ReadTag(packet, "Tag");
            if (num2 == ASN1.ContextTag(1))
            {
                ASN1.ReadTag(packet, ASN1.SequenceTag(0), "NegTokens");
                ASN1.ReadTag(packet, ASN1.SequenceTag(0), "NegTokens2");
                ASN1.ReadTag(packet, ASN1.ContextTag(0), "CTX_OctetString");
                RDPClient.m_ChallengeMsg = new byte[ASN1.ReadTag(packet, ASN1.OctetStringTag(), "OctetString")];
                packet.Read(RDPClient.m_ChallengeMsg, 0, RDPClient.m_ChallengeMsg.Length);
                ASN1.CloseTag(packet, "OctetString");
                ASN1.CloseTag(packet, "CTX_OctetString");
                ASN1.CloseTag(packet, "NegTokens2");
                ASN1.CloseTag(packet, "NegTokens");
            }
            else if (num2 == ASN1.ContextTag(3))
            {
                buffer = new byte[ASN1.ReadTag(packet, ASN1.OctetStringTag(), "OctetString")];
                packet.Read(buffer, 0, buffer.Length);
                ASN1.CloseTag(packet, "OctetString");
            }
            ASN1.CloseTag(packet, "Tag");
            ASN1.CloseTag(packet, "TSRequest");
            if (buffer != null)
            {
                byte[] buffer2 = RDPClient.m_NTLMAuthenticate.DecryptMessage(buffer);
                buffer2[0] = (byte) (buffer2[0] - 1);
                if (!NTLM.CompareArray(buffer2, ServerPublicKey))
                {
                    throw new Exception("Unable to verify the server's public key!");
                }
                buffer2[0] = (byte) (buffer2[0] + 1);
                SendTSRequest(null, WriteTSCredentials(), null);
                RDPClient.m_bAuthenticated = true;
            }
            else
            {
                ReadNegoToken(RDPClient.m_ChallengeMsg, ServerPublicKey);
            }
        }

        private static void ReadNegoToken(byte[] Challenge, byte[] ServerPublicKey)
        {
            RdpPacket negoTokens = new RdpPacket();
            byte[] buffer = RDPClient.m_NTLMAuthenticate.ProcessChallenge(Challenge);
            negoTokens.Write(buffer, 0, buffer.Length);
            negoTokens.Position = 0L;
            byte[] buffer2 = RDPClient.m_NTLMAuthenticate.EncryptMessage(ServerPublicKey);
            SendTSRequest(negoTokens, null, buffer2);
        }

        private static RdpPacket Receive()
        {
            byte[] buffer = new byte[0x2000];
            int length = Network.Receive(buffer);
            RdpPacket packet = new RdpPacket();
            packet.Write(buffer, 0, length);
            packet.Position = 0L;
            return packet;
        }

        private static byte[] WriteTSCredentials()
        {
            RdpPacket packet = new RdpPacket();
            ASN1.WriteTag(packet, ASN1.SequenceTag(0), "SEQ_TSCRED");
            ASN1.WriteTag(packet, ASN1.ContextTag(0), "CTX_credType");
            ASN1.WriteInteger(packet, 1);
            ASN1.CloseTag(packet, "CTX_credType");
            ASN1.WriteTag(packet, ASN1.ContextTag(1), "CTX_credentials");
            ASN1.WriteTag(packet, ASN1.OctetStringTag(), "CTX_OctetString");
            ASN1.WriteTag(packet, ASN1.SequenceTag(0), "SEQ_Credentials");
            ASN1.WriteTag(packet, ASN1.ContextTag(0), "CTX_domain");
            ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctectString");
            byte[] bytes = Encoding.Unicode.GetBytes(RDPClient.Domain);
            packet.Write(bytes, 0, bytes.Length);
            ASN1.CloseTag(packet, "OctectString");
            ASN1.CloseTag(packet, "CTX_domain");
            ASN1.WriteTag(packet, ASN1.ContextTag(1), "CTX_user");
            ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctectString");
            byte[] buffer = Encoding.Unicode.GetBytes(RDPClient.Username);
            packet.Write(buffer, 0, buffer.Length);
            ASN1.CloseTag(packet, "OctectString");
            ASN1.CloseTag(packet, "CTX_user");
            ASN1.WriteTag(packet, ASN1.ContextTag(2), "CTX_password");
            ASN1.WriteTag(packet, ASN1.OctetStringTag(), "OctectString");
            byte[] buffer3 = Encoding.Unicode.GetBytes(RDPClient.Password);
            packet.Write(buffer3, 0, buffer3.Length);
            ASN1.CloseTag(packet, "OctectString");
            ASN1.CloseTag(packet, "CTX_password");
            ASN1.CloseTag(packet, "SEQ_Credentials");
            ASN1.CloseTag(packet, "CTX_OctetString");
            ASN1.CloseTag(packet, "CTX_credentials");
            ASN1.CloseTag(packet, "SEQ_TSCRED");
            byte[] buffer4 = new byte[packet.Length];
            packet.Position = 0L;
            packet.Read(buffer4, 0, buffer4.Length);
            return RDPClient.m_NTLMAuthenticate.EncryptMessage(buffer4);
        }

    }
}