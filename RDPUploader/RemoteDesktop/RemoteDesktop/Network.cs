using System;
using System.Threading;

namespace RemoteDesktop
{
    public delegate void ConnectionStageChangedHandler();

    public class Network
    {
        internal static event ConnectionStageChangedHandler ConnectionStageChanged;

        public static void AddBlob(PacketLogger.PacketType type, byte[] data)
        {
            RDPClient.m_OpenSocket.AddBlob(type, data);
        }

        internal static void Close()
        {
            try
            {
                if (RDPClient.m_OpenSocket != null)
                {
                    RDPClient.m_OpenSocket.Close();
                    RDPClient.m_bConnectionAlive = false;
                    RDPClient.m_OpenSocket = null;
                }
            }
            catch { }
        }

        internal static void Connect(string host, int port)
        {
            ConnectionStage = RDPClient.eConnectionStage.Connecting;
            RDPClient.m_bSSLConnection = false;
            try
            {
                NetworkSocket socket = new NetworkSocket(host.Replace(".", ""));
                socket.Connect(host, port);
                RDPClient.m_OpenSocket = socket;
            }
            catch
            {
            }
        }

        internal static void ConnectSSL()
        {
            RDPClient.m_bSSLConnection = true;
            RDPClient.m_OpenSocket.ConnectSSL();
        }

        public static byte[] GetBlob(PacketLogger.PacketType type)
        {
            return RDPClient.m_OpenSocket.GetBlob(type);
        }

        public static byte[] GetSSLPublicKey()
        {
            return RDPClient.m_OpenSocket.GetSSLPublicKey();
        }

        public static int Receive(byte[] buffer)
        {
            return Receive(buffer, buffer.Length);
        }

        public static int Receive(byte[] buffer, int size)
        {
            return RDPClient.m_OpenSocket.Receive(buffer, size);
        }

        public static int Send(byte[] buffer)
        {
            return RDPClient.m_OpenSocket.Send(buffer);
        }

        public static bool Connected
        {
            get
            {
                return (RDPClient.m_OpenSocket != null);
            }
        }

        public static bool ConnectionAlive
        {
            get
            {
                return RDPClient.m_bConnectionAlive;
            }
            set
            {
                RDPClient.m_bConnectionAlive = value;
            }
        }

        internal static RDPClient.eConnectionStage ConnectionStage
        {
            get
            {
                return RDPClient.m_ConnectionStage;
            }
            set
            {
                RDPClient.eConnectionStage connectionStage = RDPClient.m_ConnectionStage;
                RDPClient.m_ConnectionStage = value;
                if ((connectionStage != value) && (ConnectionStageChanged != null))
                {
                    ConnectionStageChanged();
                }
            }
        }

        public static PacketLogger Logger
        {
            get
            {
                return RDPClient.m_Logger;
            }
            set
            {
                RDPClient.m_Logger = value;
            }
        }

        public static NetworkSocket OpenSocket
        {
            get
            {
                return RDPClient.m_OpenSocket;
            }
        }

        public static bool SSLConnection
        {
            get
            {
                return RDPClient.m_bSSLConnection;
            }
        }

    }
}