using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace RemoteDesktop
{
    public class NetworkSocket
    {
        // Поля
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private SslStream sslStream;
        private string m_sSocketName;
        private byte[] m_SSLPublicKey;
        private object lockObj = new object();

        // Конструктор
        public NetworkSocket(string sSocketName)
        {
            this.m_sSocketName = sSocketName;
        }

        // Методы подключения
        public void Connect(string host, int port)
        {
            // TcpClient
            tcpClient = new TcpClient();
            tcpClient.SendTimeout = StaticSettings.SocketTimeout;
            tcpClient.ReceiveTimeout = StaticSettings.SocketTimeout;
            tcpClient.Connect(IPAddress.Parse(host), port);

            // NetworkStream
            networkStream = tcpClient.GetStream();
            networkStream.WriteTimeout = StaticSettings.SocketTimeout;
            networkStream.ReadTimeout = StaticSettings.SocketTimeout;
        }

        public void ConnectSSL()
        {
            lock (lockObj)
            {
                // Инициализируем SslClient
                sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate));
                sslStream.WriteTimeout = StaticSettings.SocketTimeout;
                sslStream.ReadTimeout = StaticSettings.SocketTimeout;

                // Авторизация
                sslStream.AuthenticateAsClient(RDPClient.Host);

                if (!RDPClient.UseAltChecker)
                {
                    // Флаг авторизации
                    if (!RDPClient.FullXP)
                        RDPClient.GoodAuth = true;
                }
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            m_SSLPublicKey = certificate.GetPublicKey();

            if (Network.Logger == null)
                Network.Logger = new PacketLogger();

            if (Network.Logger != null)
                Network.Logger.SSLPublicKey(m_SSLPublicKey, m_sSocketName);

            return true;
        }

        // Отключение
        public void Close()
        {
            if (sslStream != null)
            {
                try
                {
                    sslStream.Flush();
                    sslStream.Close();
                    sslStream.Dispose();
                }
                catch { }
            }

            if (networkStream != null)
            {
                try
                {
                    networkStream.Flush();
                    networkStream.Close();
                    networkStream.Dispose();
                }
                catch { }
            }

            if (tcpClient != null)
            {
                try
                {
                    tcpClient.Close();
                }
                catch { }
            }

            m_sSocketName = "";
            m_SSLPublicKey = null;
        }

        // Рабочие методы
        public void AddBlob(PacketLogger.PacketType type, byte[] data)
        {
            if (Network.Logger == null)
                Network.Logger = new PacketLogger();

            Network.Logger.AddBlob(type, data, m_sSocketName);
        }

        public byte[] GetBlob(PacketLogger.PacketType type)
        {
            return Network.Logger.GetBlob(type, m_sSocketName);
        }

        public byte[] GetSSLPublicKey()
        {
            return m_SSLPublicKey;
        }

        protected int InternalReceive(byte[] buffer, int offset, int size)
        {
            return networkStream.Read(buffer, offset, size);
        }

        protected int InternalSend(byte[] buffer, int offset, int size)
        {
            networkStream.Write(buffer, offset, size);
            return buffer.Length;
        }

        public int Receive(byte[] buffer, int size)
        {
            return Receive(buffer, 0, size);
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            int len = -1;
            if (sslStream != null)
            {
                len = sslStream.Read(buffer, offset, size);
            }
            else if (networkStream != null || len == -1)
            {
                len = InternalReceive(buffer, offset, size);
            }
            return len;
        }

        public int Send(byte[] buffer)
        {
            lock (lockObj)
            {
                if (sslStream != null)
                {
                    sslStream.Write(buffer, 0, buffer.Length);
                    sslStream.Flush();
                    return buffer.Length;
                }
            }
            return InternalSend(buffer, 0, buffer.Length);
        }


    }
}