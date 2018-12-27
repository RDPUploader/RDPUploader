using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class Options
    {
        public static bool IsRunned;
        internal const string DllPassword = "34589;erw02134qwwdsKJASD#@_ei232relwrkwjlerasdnfa!@#"; // Пароль от DLL
        internal static X509Certificate2Collection CertCollection = null; // Хранилище сертификатов
        internal static int SocketTimeout = 3000; // Таймаут сокета
        internal static RdpCanvas Canvas;
        internal static int BoundsBottom = (height - 1);
        internal static int BoundsLeft = 0;
        internal static int BoundsRight = (width - 1);
        internal static int BoundsTop = 0;
        internal static int Bpp = 3; // Bit Per Pixel
        internal static int bpp_mask = 0xffffff;
        internal static int client_bpp = 0x18; // Client Bit Per Pixel
        private static int m_server_bpp = 8; // Server Bit Per Pixel
        internal static string ClientName = "Windows7"; // Client Name
        internal static string Domain = ""; // Domain
        internal static string DomainAndUsername = ""; // Domain and Username
        internal static string Host = ""; // Host
        internal static string hostname = "";
        internal static string Username = ""; // Username
        internal static string Password = ""; // Password
        internal static int Port = 3389; // Port
        internal static bool use_rdp5 = true; // Use RDP 5
        internal static bool enableNLA = true; // Enable NLA
        internal static int width = 0x400; // Width
        internal static int height = 0x300; // Height
        internal static int LogonID; // Logon ID
        internal static int sessionID = 0; // Session ID
        internal static int e = 100;
        internal static byte[] ReconnectCookie; // Reconnect Cookie
        internal static int Keyboard = 0x00000409; // Keyboard: en-US
        internal static HostFlags flags;
        internal static MCS.NegotiationFlags serverNegotiateFlags;
        internal static bool cache_bitmaps = true;
        internal static bool persistentBmpCache = true;
        internal static bool suppress_output_supported = false;
        internal static bool use_fastpath_input = false;
        internal static bool enableFastPathOutput = true;
        internal static SavedHost CurrentHost = null;
        public delegate void OnAutorizationEventHandler();
        public static event OnAutorizationEventHandler OnAutorizationEvent;

        internal static void Enter()
        {
            Monitor.Enter(Canvas);
        }

        public static bool TryEnter()
        {
            return Monitor.TryEnter(Canvas);
        }

        internal static void Exit()
        {
            Monitor.Exit(Canvas);
        }

        public static bool IsHostFlagSet(HostFlags Flag)
        {
            return ((flags & Flag) == Flag);
        }

        internal static void OnAutorization()
        {
            Debug.WriteLine("OnAutorization");

            if (OnAutorizationEvent != null)
                OnAutorizationEvent();
        }

        internal static void OnError(Exception e)
        {
            if (e is RDFatalException)
            {
                Debug.WriteLine("OnError: " + e.Message);
            }
            else if (e is RDException)
            {
                Debug.WriteLine("OnError: " + e.Message);
            }
            else
            {
                Debug.WriteLine("OnError: " + e.Message);
            }

            Options.IsRunned = false;
        }

        internal static void OnClosed()
        {
            Debug.WriteLine("OnClosed");

            Options.IsRunned = false;
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

    }
}