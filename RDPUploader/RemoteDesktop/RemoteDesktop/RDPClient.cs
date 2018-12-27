using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Specialized;

namespace RemoteDesktop
{
    public class RDPClient
    {
        // Поля
        [ThreadStatic]
        internal static bool FullXP;
        [ThreadStatic]
        internal static bool GoodAuth;
        [ThreadStatic]
        internal static bool UseAltChecker;
        [ThreadStatic]
        internal static bool NeedChangePassword;

        // Инициализация данных
        private void Init(int sec1, int sec2)
        {
            // Поля безопасности
            StaticSettings.SecureValue1 = sec1;
            StaticSettings.SecureValue2 = sec2;

            // Мои настройки
            FullXP = false;
            GoodAuth = false;
            UseAltChecker = false;
            NeedChangePassword = false;

            // Options
            enableFastPathOutput = false;
            BoundsBottom = (height - 1);
            BoundsRight = (width - 1);
            m_server_bpp = 8;
            width = 1024;
            height = 768;

            // ControlFlow
            rdp_shareid = 0;

            // Network
            m_bSSLConnection = false;
            m_bConnectionAlive = false;
            m_ConnectionStage = RDPClient.eConnectionStage.None;
            m_Logger = null;

            // Secure
            readCert = false;
            modulus_size = StaticSettings.SecureValue1;
            dec_count = 0;
            enc_count = 0;
            m_Client_Random = new byte[] { 
            0, 0xff, 30, 0x37, 0x4d, 0x16, 0xd4, 0x20, 0x61, 0x2d, 0, 0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
            _r = new byte[StaticSettings.SecureValue2];
            m_RC4_Dec = new RC4();
            m_RC4_Enc = new RC4();
            m_Sec_Crypted_Random = new byte[StaticSettings.SecureValue1];
            m_Sec_Decrypt_Update_Key = new byte[StaticSettings.SecureValue2];
            m_Sec_Sign_Key = new byte[StaticSettings.SecureValue2];

            // ASN1
            m_Fixup = new Dictionary<string, ASN1.Fixup>();

            // Channels
            m_Channels = new List<IVirtualChannel>();

            // License
            m_bLicensed = false;
            m_Licence_Sign_Key = new byte[StaticSettings.SecureValue2];
            m_LicenceKey = new byte[StaticSettings.SecureValue2];
            m_Server_Random = new byte[0x20];

            // ControlFlow
            m_bInitialised = false;
            m_bServerSupportsCacheV2 = false;

            // Rdp
            timeoutTimer = new System.Timers.Timer();
            m_bExceptionReported = false;
            m_bHalt = false;
            m_InputCache = new List<Rdp.InputInfo>();

            // ISO
            lastchannel = 0;

            // Channels
            m_Channels = new List<IVirtualChannel>();

            // CredSSP
            m_bAuthenticated = false;

            // NTLM
            NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x8000;
            NTLMSSP_NEGOTIATE_ANONYMOUS = 0x800;
            NTLMSSP_NEGOTIATE_DATAGRAM = (uint)StaticSettings.SecureValue1;
            NTLMSSP_NEGOTIATE_EXTENDED_SESSION_SECURITY = 0x80000;
            NTLMSSP_NEGOTIATE_IDENTIFY = 0x100000;
            NTLMSSP_NEGOTIATE_LM_KEY = 0x80;
            NTLMSSP_NEGOTIATE_NTLM = 0x200;
            NTLMSSP_NEGOTIATE_OEM = 2;
            NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x1000;
            NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x2000;
            NTLMSSP_NEGOTIATE_SEAL = 0x20;
            NTLMSSP_NEGOTIATE_SIGN = (uint)StaticSettings.SecureValue2;
            NTLMSSP_REQUEST_NON_NT_SESSION_KEY = 0x400000;
            NTLMSSP_REQUEST_TARGET = 4;
            NTLMSSP_TARGET_TYPE_DOMAIN = 0x10000;
            NTLMSSP_TARGET_TYPE_SERVER = 0x20000;
            NTLMSSP_NEGOTIATE_UNICODE = 1;
        }

        public ConnectData ConnectFullXP(string ip, int port, string login, string password, int timeout, int secure1, int secure2)
        {
            // Инициализация данных
            Init(secure1, secure2);

            // Мои настройки
            StaticSettings.SocketTimeout = timeout;
            StaticSettings.ConnectionTimeout = timeout + 10000;
            FullXP = true;

            // Проверка домена
            if (login.Contains(@"\"))
            {
                string tempDomain = login.Split(new string[] { @"\" }, StringSplitOptions.None)[0];
                string tempLogin = login.Split(new string[] { @"\" }, StringSplitOptions.None)[1];

                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = tempLogin;
                RDPClient.Password = password;
                RDPClient.Domain = tempDomain;
                RDPClient.DomainAndUsername = tempDomain + @"\" + tempLogin;
            }
            else
            {
                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = login;
                RDPClient.Password = password;
                RDPClient.Domain = "";
                RDPClient.DomainAndUsername = login;
            }

            ClientName = "FreeRDP";
            enableNLA = true;
            use_rdp5 = true;
            sessionID = 0;
            hostname = "";
            flags = null;
            ReconnectCookie = null;
            serverNegotiateFlags = MCS.NegotiationFlags.DYNVC_GFX_PROTOCOL_SUPPORTED;
            Network.ConnectionStage = RDPClient.eConnectionStage.Connecting;

            var result = new ConnectData();
            result.IsValid = InitiateConnectionFullXP();
            result.Domain = RDPClient.Domain;

            return result;
        }

        public bool ConnectLite(string ip, int port, string login, string password, int timeout, int secure1, int secure2)
        {
            // Инициализация данных
            Init(secure1, secure2);

            // Мои настройки
            StaticSettings.SocketTimeout = timeout;
            StaticSettings.ConnectionTimeout = timeout + 10000;

            // Проверка домена
            if (login.Contains(@"\"))
            {
                string tempDomain = login.Split(new string[] { @"\" }, StringSplitOptions.None)[0];
                string tempLogin = login.Split(new string[] { @"\" }, StringSplitOptions.None)[1];

                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = tempLogin;
                RDPClient.Password = password;
                RDPClient.Domain = tempDomain;
                RDPClient.DomainAndUsername = tempDomain + @"\" + tempLogin;
            }
            else
            {
                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = login;
                RDPClient.Password = password;
                RDPClient.Domain = "";
                RDPClient.DomainAndUsername = login;
            }

            ClientName = "FreeRDP";
            enableNLA = true;
            use_rdp5 = true;
            sessionID = 0;
            hostname = "";
            flags = null;
            ReconnectCookie = null;
            serverNegotiateFlags = MCS.NegotiationFlags.DYNVC_GFX_PROTOCOL_SUPPORTED;
            Network.ConnectionStage = RDPClient.eConnectionStage.Connecting;

            return InitiateConnectionLite();
        }

        public CheckData Check(string ip, int port, string login, string password, int timeout, bool useAltChecker, int secure1, int secure2)
        {
            // Инициализация данных
            Init(secure1, secure2);

            // Мои настройки
            UseAltChecker = useAltChecker;

            if (useAltChecker)
            {
                FullXP = true;
                StaticSettings.SocketTimeout = timeout + 30000;
                StaticSettings.ConnectionTimeout = timeout + 30000;
            }
            else
            {
                StaticSettings.SocketTimeout = timeout;
                StaticSettings.ConnectionTimeout = timeout + 10000;
            }
            
            // Проверка домена
            if (login.Contains(@"\"))
            {
                string tempDomain = login.Split(new string[] { @"\" }, StringSplitOptions.None)[0];
                string tempLogin = login.Split(new string[] { @"\" }, StringSplitOptions.None)[1];

                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = tempLogin;
                RDPClient.Password = password;
                RDPClient.Domain = tempDomain;
                RDPClient.DomainAndUsername = tempDomain + @"\" + tempLogin;
            }
            else
            {
                // Базовые настройки
                RDPClient.Host = ip;
                RDPClient.Port = port;
                RDPClient.Username = login;
                RDPClient.Password = password;
                RDPClient.Domain = "";
                RDPClient.DomainAndUsername = login;
            }

            ClientName = "FreeRDP";
            enableNLA = true;
            use_rdp5 = true;
            sessionID = 0;
            hostname = "";
            flags = null;
            ReconnectCookie = null;
            serverNegotiateFlags = MCS.NegotiationFlags.DYNVC_GFX_PROTOCOL_SUPPORTED;
            Network.ConnectionStage = RDPClient.eConnectionStage.Connecting;

            var result = new CheckData();
            result.IsValid = InitiateConnectionCheck();
            result.NeedChangePassword = NeedChangePassword;
            result.Domain = RDPClient.Domain;

            return result;
        }

        // Проверка порта на протокол RDP
        public int CheckRdpVersion(string ip, int port, int timeout, int secure1, int secure2)
        {
            // Инициализация данных
            Init(secure1, secure2);

            // Мои настройки
            StaticSettings.SocketTimeout = timeout;
            StaticSettings.ConnectionTimeout = timeout + 10000;
            GoodAuth = false;

            // Базовые настройки
            RDPClient.Host = ip;
            RDPClient.Port = port;
            RDPClient.Username = "Test";
            RDPClient.Password = "Test";
            RDPClient.Domain = "";
            RDPClient.DomainAndUsername = "Test";
            ClientName = "FreeRDP";
            enableNLA = true;
            use_rdp5 = true;
            sessionID = 0;
            hostname = "";
            flags = null;
            ReconnectCookie = null;
            serverNegotiateFlags = MCS.NegotiationFlags.DYNVC_GFX_PROTOCOL_SUPPORTED;
            Network.ConnectionStage = RDPClient.eConnectionStage.Connecting;

            return InitiateCheckVersion();
        }

        private bool InitiateConnectionFullXP()
        {
            try
            {
                // Проверка Win 8/2008/2012
                Network.Connect(RDPClient.Host, RDPClient.Port);
                MCS.send_connection_request(null, false);

                RDPClient.GoodAuth = false;

                // Запуск сессии
                Rdp.Start();
            }
            catch { }
            finally
            {
                // Остановка
                Disconnect();
            }

            return RDPClient.GoodAuth;
        }

        private bool InitiateConnectionLite()
        {
            bool result = false;

            try
            {
                // Проверка Win 8/2008/2012
                Network.Connect(RDPClient.Host, RDPClient.Port);
                MCS.send_connection_request(null, false);

                // Успешная авторизация
                if (RDPClient.GoodAuth)
                    result = true;
            }
            catch{ }
            finally
            {
                // Остановка
                Disconnect();
            }

            return result;
        }

        private bool InitiateConnectionCheck()
        {
            bool result = false;

            try
            {
                // Проверка Win 8/2008/2012
                Network.Connect(RDPClient.Host, RDPClient.Port);
                MCS.send_connection_request(null, false);

                if (!UseAltChecker)
                {
                    // Успешная авторизация
                    if (RDPClient.GoodAuth)
                        result = true;
                }

                // Запуск сессии
                Rdp.Start();

                // Успешная авторизация
                if (RDPClient.GoodAuth)
                    result = true;
            }
            catch { }
            finally
            {
                // Остановка
                Disconnect();
            }

            return result;
        }

        private int InitiateCheckVersion()
        {
            int result = -1;

            try
            {
                // Проверка порта на протокол RDP
                Network.Connect(RDPClient.Host, RDPClient.Port);
                result = MCS.send_connection_request_for_check(null, false);
            }
            catch
            {
                result = -1;
            }
            finally
            {
                // Остановка
                Disconnect();
            }

            return result;
        }

        // Остановка
        internal static void Disconnect()
        {
            ISO.Disconnect();
        }

        // Данные
        public class ConnectData
        {
            public bool IsValid;
            public string Domain;
        }

        public class CheckData
        {
            public bool IsValid;
            public bool NeedChangePassword;
            public string Domain;
        }

        //////////////////////////////////////////////////////////
        // Static поля

        // MCS
        [ThreadStatic]
        internal static int McsUserID;

        // ISO
        [ThreadStatic]
        internal static RdpPacket m_Packet;
        [ThreadStatic]
        internal static int next_packet;

        // CredSSP
        [ThreadStatic]
        internal static byte[] m_ChallengeMsg;
        [ThreadStatic]
        internal static NTLM m_NTLMAuthenticate;

        // ControlFlow
        [ThreadStatic]
        internal static int rdp_shareid;

        // Network
        [ThreadStatic]
        internal static bool m_bConnectionAlive;
        [ThreadStatic]
        internal static bool m_bSSLConnection;
        [ThreadStatic]
        internal static RDPClient.eConnectionStage m_ConnectionStage;
        [ThreadStatic]
        internal static PacketLogger m_Logger;
        [ThreadStatic]
        internal static NetworkSocket m_OpenSocket;

        // Secure
        [ThreadStatic]
        internal static bool readCert;
        [ThreadStatic]
        internal static int modulus_size;
        [ThreadStatic]
        internal static int dec_count;
        [ThreadStatic]
        internal static int enc_count;
        [ThreadStatic]
        internal static byte[] m_Client_Random;
        [ThreadStatic]
        internal static byte[] _r;
        [ThreadStatic]
        internal static RC4 m_RC4_Dec;
        [ThreadStatic]
        internal static RC4 m_RC4_Enc;
        [ThreadStatic]
        internal static byte[] m_Sec_Crypted_Random;
        [ThreadStatic]
        internal static byte[] m_Sec_Decrypt_Update_Key;
        [ThreadStatic]
        internal static byte[] m_Sec_Sign_Key;

        // ASN1
        [ThreadStatic]
        internal static Dictionary<string, ASN1.Fixup> m_Fixup;

        // ControlFlow
        [ThreadStatic]
        internal static bool m_bInitialised;
        [ThreadStatic]
        internal static bool m_bServerSupportsCacheV2;

        // License
        [ThreadStatic]
        internal static byte[] m_In_Sig;
        [ThreadStatic]
        internal static byte[] m_In_Token; 
        [ThreadStatic]
        internal static bool m_bLicensed;
        [ThreadStatic]
        internal static byte[] m_Licence_Sign_Key;
        [ThreadStatic]
        internal static byte[] m_LicenceKey;
        [ThreadStatic]
        internal static byte[] m_Server_Random;

        // Rdp
        [ThreadStatic]
        internal static System.Timers.Timer timeoutTimer;
        [ThreadStatic]
        internal static bool m_bExceptionReported;
        [ThreadStatic]
        internal static bool m_bHalt;
        [ThreadStatic]
        internal static List<Rdp.InputInfo> m_InputCache;

        // ISO
        [ThreadStatic]
        internal static int lastchannel;

        // CredSSP
        [ThreadStatic]
        internal static bool m_bAuthenticated;

        // NTLM
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_ALWAYS_SIGN;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_ANONYMOUS;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_DATAGRAM;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_EXTENDED_SESSION_SECURITY;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_IDENTIFY;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_LM_KEY;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_NTLM;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_OEM;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_SEAL;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_SIGN;
        [ThreadStatic]
        internal static uint NTLMSSP_REQUEST_NON_NT_SESSION_KEY;
        [ThreadStatic]
        internal static uint NTLMSSP_REQUEST_TARGET;
        [ThreadStatic]
        internal static uint NTLMSSP_TARGET_TYPE_DOMAIN;
        [ThreadStatic]
        internal static uint NTLMSSP_TARGET_TYPE_SERVER;
        [ThreadStatic]
        internal static uint NTLMSSP_NEGOTIATE_UNICODE;

        // Channels
        [ThreadStatic]
        internal static RdpPacket m_FullPacket;
        [ThreadStatic]
        internal static List<IVirtualChannel> m_Channels;

        // Options
        [ThreadStatic]
        internal static int BoundsBottom;
        [ThreadStatic]
        internal static int BoundsLeft;
        [ThreadStatic]
        internal static int BoundsRight;
        [ThreadStatic]
        internal static int BoundsTop;
        [ThreadStatic]
        internal static string ClientName;
        [ThreadStatic]
        internal static string Domain;
        [ThreadStatic]
        internal static string DomainAndUsername;
        [ThreadStatic]
        internal static bool enableFastPathOutput;
        [ThreadStatic]
        internal static bool enableNLA;
        [ThreadStatic]
        internal static HostFlags? flags;
        [ThreadStatic]
        internal static string Host;
        [ThreadStatic]
        internal static string hostname;
        [ThreadStatic]
        internal static int LogonID;
        [ThreadStatic]
        private static int m_server_bpp;
        [ThreadStatic]
        internal static string Password;
        [ThreadStatic]
        internal static int Port;
        [ThreadStatic]
        internal static byte[] ReconnectCookie;
        [ThreadStatic]
        internal static MCS.NegotiationFlags? serverNegotiateFlags;
        [ThreadStatic]
        internal static int sessionID;
        [ThreadStatic]
        internal static bool suppress_output_supported;
        [ThreadStatic]
        internal static bool use_fastpath_input;
        [ThreadStatic]
        internal static bool use_rdp5;
        [ThreadStatic]
        internal static string Username;
        [ThreadStatic]
        internal static int height;
        [ThreadStatic]
        internal static int width;

        // Методы из класса Options
        internal static bool IsHostFlagSet(HostFlags Flag)
        {
            return ((flags & Flag) == Flag);
        }

        internal static void OnInitialise()
        {
            // throw new Exception("OnInitialise");
        }

        internal static void OnClosed()
        {
            //throw new Exception("OnClosed");
        }

        internal static void OnError(Exception e)
        {
            if (e is RDFatalException)
            {
                throw new Exception(e.Message + "\n" + e.StackTrace);
            }
            else if (e is RDException)
            {
                throw new Exception(e.Message + "\n" + e.StackTrace);
            }
            else
            {
                throw new Exception(e.Message + "\n" + e.StackTrace);
            }
        }

        internal static int server_bpp
        {
            get
            {
                return m_server_bpp;
            }
            set
            {
                m_server_bpp = value;
            }
        }

        internal enum eConnectionStage
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