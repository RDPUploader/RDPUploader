using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktop
{
    internal static class StaticSettings
    {
        // Таймаут сокета
        public static int SocketTimeout = 3000;

        // Таймаут ожидания соединения
        public static int ConnectionTimeout = 20000;

        // Поля безопасности
        internal static int SecureValue1;
        internal static int SecureValue2;

    }
}