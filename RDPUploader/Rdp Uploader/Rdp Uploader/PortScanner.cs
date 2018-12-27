using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using RemoteDesktop;

namespace RDP_Uploader
{
    /**
     * PortScanner
     * 
     * PortScanner class contains method check is open port
     */
    public static class PortScaner
    {
        // Проверяет заданный порт
        internal static bool IsOpenPort(string ip, int port, int timeout)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    tcpClient.ReceiveTimeout = timeout;
                    tcpClient.SendTimeout = timeout;
                    tcpClient.Connect(ip, port);
                    return tcpClient.Connected;
                }
            }
            catch { return false; }
        }

        // Проверка ip на версию протокола RDP
        internal static bool? GetRdpVersion(string ip, int port, int timeout)
        {
            bool? result = false;

            try
            {
                // Rdp клиент
                var client = new RDPClient();

                // Подключение
                int version = 0;

                try
                {
                    version = client.CheckRdpVersion(ip, port, timeout, NativeWrapper.Dll.SecureValue1, NativeWrapper.Dll.SecureValue2);
                }
                catch { }

                if (version == 8) // PROTOCOL_HYBRID_EX 0x00000008
                {
                    result = true;
                }
                else if (version == 2) // PROTOCOL_HYBRID 0x00000002
                {
                    result = true;
                }
                else if (version == 268435456) // PROTOCOL_RDP (Windows 2003) 0x10000000
                {
                    result = false;
                }
                else if (version == 1) // PROTOCOL_SSL (Windows 2003) 0x00000001
                {
                    result = false;
                }
                else if (version == 0) // PROTOCOL_RDP (Windows XP) 0x00000000
                {
                    result = false;
                }
                else if (version == -1)
                {
                    result = null;
                }
                else
                {
                    result = false;
                }
            }
            catch { }

            return result;
        }

    }
}