using System;
using System.Threading;

namespace RdpUploadClient
{
    internal class Network
    {
        private static bool m_bConnectionAlive = false;
        private static bool m_bSSLConnection = false;
        private static eConnectionStage m_ConnectionStage = eConnectionStage.None;
        private static PacketLogger m_Logger = null;
        internal static NetworkSocket m_OpenSocket = null;

        internal static event ConnectionStageChangedHandler ConnectionStageChanged;

        public static void AddBlob(PacketLogger.PacketType type, byte[] data)
        {
            m_OpenSocket.AddBlob(type, data);
        }

        internal static void Close()
        {
            try
            {
                if (m_OpenSocket != null)
                {
                    m_OpenSocket.Close();
                    m_bConnectionAlive = false;
                    m_OpenSocket = null;
                }
            }
            catch { }
        }

        internal static void Connect(string host, int port)
        {
            ConnectionStage = eConnectionStage.Connecting;
            m_bSSLConnection = false;
            try
            {
                NetworkSocket socket = new NetworkSocket(host.Replace(".", ""));
                socket.Connect(host, port);
                m_OpenSocket = socket;
            }
            catch { }
        }

        internal static void ConnectSSL()
        {
            m_bSSLConnection = true;
            m_OpenSocket.ConnectSSL();
        }

        public static byte[] GetBlob(PacketLogger.PacketType type)
        {
            return m_OpenSocket.GetBlob(type);
        }

        public static byte[] GetSSLPublicKey()
        {
            return m_OpenSocket.GetSSLPublicKey();
        }

        public static int Receive(byte[] buffer)
        {
            return Receive(buffer, buffer.Length);
        }

        public static int Receive(byte[] buffer, int size)
        {
            return m_OpenSocket.Receive(buffer, size);
        }

        public static int Send(byte[] buffer)
        {
            return m_OpenSocket.Send(buffer);
        }

        public static bool Connected
        {
            get
            {
                return (m_OpenSocket != null);
            }
        }

        public static bool ConnectionAlive
        {
            get
            {
                return m_bConnectionAlive;
            }
            set
            {
                m_bConnectionAlive = value;
            }
        }

        internal static eConnectionStage ConnectionStage
        {
            get
            {
                return m_ConnectionStage;
            }
            set
            {
                eConnectionStage connectionStage = m_ConnectionStage;
                m_ConnectionStage = value;
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
                return m_Logger;
            }
            set
            {
                m_Logger = value;
            }
        }

        public static NetworkSocket OpenSocket
        {
            get
            {
                return m_OpenSocket;
            }
        }

        public static bool SSLConnection
        {
            get
            {
                return m_bSSLConnection;
            }
        }

        public enum eConnectionStage
        {
            None,
            Connecting,
            ConnectingToGateway,
            ConnectingToHost,
            Negotiating,
            Securing,
            Authenticating,
            Establishing,
            Login,
            Reconnecting,
            SecureAndLogin
        }

    }
}