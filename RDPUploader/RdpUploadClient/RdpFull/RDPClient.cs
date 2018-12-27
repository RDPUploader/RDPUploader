using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace RdpUploadClient
{
    internal class RDPClient
    {
        // Поля
        internal static SavedHost CurrentHost;

        public void Connect(string ip, int port, string login, string password, int connectTimeout, bool nla)
        {
            // Базовые настройки хоста
            Options.IsRunned = true;
            CurrentHost = new SavedHost();
            CurrentHost.HostName = ip;
            CurrentHost.Port = port;
            CurrentHost.Username = login;
            CurrentHost.Password = password;
            CurrentHost.ConnectTimeout = connectTimeout;
            CurrentHost.EnableBmpCache = true;
            CurrentHost.EnableNLA = nla;
            CurrentHost.Flags = HostFlags.ConnectLocalFileSystem | HostFlags.DesktopBackground;

            // Настройки файловой системы
            CurrentHost.FSDeviceName = Options.ClientName.Replace(" ", "");
            CurrentHost.FSDriveName = "drive";

            // Проверка хоста, порта, имени пользователя и пароля
            if (CurrentHost == null)
            {
                throw new Exception();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CurrentHost.HostName) || (port == 0) || (port > 65535))
                {
                    throw new Exception();
                }
                else if (string.IsNullOrWhiteSpace(CurrentHost.Username))
                {
                    throw new Exception();
                }
                else
                {
                    CurrentHost.Connections++;
                    InitiateConnection();
                }
            }

            // Блокируем метод до завершения подключения
            while (Options.IsRunned)
                Thread.Sleep(1000);
        }

        private void InitiateConnection()
        {
            // Настройка
            Options.Host = CurrentHost.HostName;
            Options.Port = CurrentHost.Port;
            Options.width = 1024;
            Options.height = 768;
            Options.server_bpp = CurrentHost.BitsPerPixel;
            Options.Username = CurrentHost.Username;
            Options.Password = CurrentHost.Password;
            Options.SocketTimeout = CurrentHost.ConnectTimeout;
            CurrentHost.Username = CurrentHost.Username;
            CurrentHost.Password = CurrentHost.SavePassword ? CurrentHost.Password : "";
            CurrentHost.SavePassword = CurrentHost.SavePassword;
            Options.Domain = "";
            Options.enableNLA = CurrentHost.EnableNLA; // Переключатель NLA
            Options.persistentBmpCache = CurrentHost.EnableBmpCache;
            Options.use_fastpath_input = false; // Переключатель Fast-Path и Slow-Path
            Options.enableFastPathOutput = !CurrentHost.Flags.HasFlag(HostFlags.DisableFastPathOutput);
            Options.flags = CurrentHost.Flags;
            Options.sessionID = 0;
            Options.DomainAndUsername = Options.Username;

            // Проверка домена в имени пользователя
            if (Options.Username.Contains(@"\"))
            {
                string username = Options.Username;
                int index = username.IndexOf(@"\");

                if (index != -1)
                {
                    Options.Domain = username.Substring(0, index);
                    Options.Username = username.Substring(index + 1);
                }
            }

            // Очищаем каналы
            Channels.RegisteredChannels.Clear();

            // MS-RDPEFS Channel (File Transfer)
            if (CurrentHost.Flags.HasFlag(HostFlags.ConnectLocalFileSystem))
            {
                // MS-RDPEFS
                Channels.RegisteredChannels.Add(new FileSystemChannel(CurrentHost.FSDeviceName, CurrentHost.FSDriveName));

                // MS-RDPECLIP
                Channels.RegisteredChannels.Add(new ClipboardChannel());

                // MS-RDPEA (This fix need for support File Transfer for WinRT)
                Channels.RegisteredChannels.Add(new SoundChannel());
            }

            // Устанавливаем хост и флаг подключения
            Options.CurrentHost = CurrentHost;
            Network.ConnectionStage = Network.eConnectionStage.Connecting;

            this.ConnectThread();
        }

        private void ConnectThread()
        {
            try
            {
                // Инициализация
                Options.width = 1024;
                Options.height = 768;
                Options.Canvas = new RdpCanvas(Options.width, Options.height);
                ControlFlow.resetOrderState();
                Licence.Reset();
                ChangedRect.Reset();

                // Подключение
                Network.Connect(Options.Host, Options.Port);
                MCS.sendСonnectionRequest(null, false);

                // Получения изображения
                Rdp.Start();
            }
            catch
            {
                // Флаг выполнения
                Options.IsRunned = false;
            }
        }

        public void Disconnect()
        {
            ISO.Disconnect();

            if (!Rdp.IsHalted())
                Rdp.Halt();

            Options.IsRunned = false;
        }

    }
}