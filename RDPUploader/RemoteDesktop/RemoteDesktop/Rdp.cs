using System;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDesktop
{
    internal class Rdp
    {
        private const int RDP_DATA_PDU_POINTER = 0x1b;
        private const int RDP_DATA_PDU_SAVE_SESSION_INFO = 0x26;
        private const int RDP_DATA_PDU_SET_ERROR_INFO = 0x2f;
        private const int RDP_DATA_PDU_UPDATE = 2;
        internal const int RDP_PDU_DATA = 7;
        private const int RDP_PDU_DEACTIVATE = 6;
        private const int RDP_PDU_DEMAND_ACTIVE = 1;
        private const int RDP_PDU_REDIRECT = 10;
        private const int RDP_UPDATE_BITMAP = 1;
        private const int RDP_UPDATE_ORDERS = 0;
        private const int RDP_UPDATE_PALETTE = 2;
        private const int RDP_UPDATE_SYNCHRONIZE = 3;

        // Старт
        public static void Start()
        {
            // Запускаем таймер отсечки
            RDPClient.timeoutTimer.AutoReset = false;
            RDPClient.timeoutTimer.Enabled = true;
            RDPClient.timeoutTimer.Interval = StaticSettings.ConnectionTimeout;
            RDPClient.timeoutTimer.Elapsed += timeoutTimer_Elapsed;
            RDPClient.timeoutTimer.Start();

            // Запускаем основной цикл примема\передачи
            Rdp.mainloop();
        }

        // Отсечка таймера
        private static void timeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Остановка
            RDPClient.m_bHalt = true;
        }

        internal static void mainloop()
        {
            RDPClient.m_bInitialised = false;
            RDPClient.m_bHalt = false;
            RDPClient.m_bExceptionReported = false;
            try
            {
                while (!RDPClient.m_bHalt)
                {
                    int num;
                    RdpPacket data = ISO.Secure_Receive(out num);

                    switch (num)
                    {
                        case 0:
                        case 6:
                            break;

                        case 1:
                            try
                            {
                                ControlFlow.processDemandActive(data);
                            }
                            catch
                            {
                            }
                            Network.ConnectionAlive = true;
                            break;

                        case 7:
                            processData(data);
                            break;

                        case 10:
                            ControlFlow.processRedirection(data, false);
                            break;

                        case 0xff: // 255
                            // FastPathUpdate
                            break;

                        default:
                            throw new RDFatalException("Illegal type in main process :" + num.ToString());
                    }
                    Thread.Sleep(1);
                }
            }
            catch (RDFatalException exception)
            {
                Network.Close();
                if (!RDPClient.m_bExceptionReported)
                {
                    RDPClient.m_bExceptionReported = true;
                    RDPClient.OnError(exception);
                }
            }
            catch (EndOfTransmissionException)
            {
                Network.Close();
            }
            catch (SocketAbortException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception exception2)
            {
                if (!RDPClient.m_bExceptionReported)
                {
                    RDPClient.m_bExceptionReported = true;
                    RDPClient.OnError(exception2);
                }
            }
            finally
            {
                ISO.Disconnect();
                RDPClient.OnClosed();
            }
        }

        private static bool processData(RdpPacket data)
        {
            int num3;
            int num = 0;
            data.Position += 6L;
            data.getLittleEndian16();
            num = data.ReadByte();
            data.ReadByte();
            data.getLittleEndian16();

            switch (num)
            {
                case 0x26: // 38
                    // RDP_DATA_PDU_SAVE_SESSION_INFO
                    processLogonInfo(data);
                    goto Label_015E;

                case 0x2f: // 47
                    // RDP_DATA_PDU_SET_ERROR_INFO
                    num3 = data.getLittleEndian32();

                    switch (num3)
                    {
                        case 0:
                        case 12:
                            goto Label_015E;

                        case 1:
                            throw new RDFatalException("The disconnection was initiated by an administrative tool on the server in another session.");

                        case 2:
                            throw new RDFatalException("The disconnection was due to a forced logoff initiated by an administrative tool on the server in another session.");

                        case 3:
                            throw new RDFatalException("The idle session limit timer on the server has elapsed.");

                        case 4:
                            throw new RDFatalException("The active session limit timer on the server has elapsed.");

                        case 5:
                            throw new RDFatalException("Another user connected to the server, forcing the disconnection of the current connection.");

                        case 7:
                            throw new RDFatalException("The server denied the connection.");

                        case 9:
                            throw new RDFatalException("The user cannot connect to the server due to insufficient access privileges.");

                        case 11:
                            throw new RDFatalException("The disconnection was initiated by an administrative tool on the server running in the user's session.");

                        case 0x102:
                            throw new RDFatalException("There are no Client Access Licenses available for the target remote computer, please contact your network administrator.");
                    }
                    break;

                case 2:
                    // processUpdate
                    return false;

                case 0x1b: // 27
                    return false;

                default:
                    goto Label_015E;
            }
            throw new RDFatalException("Error code: " + num3.ToString("X8") + ", please contact Support with this error code");

        Label_015E:
            return false;
        }

        private static void processLogonInfo(RdpPacket data)
        {
            var number = (InfoType)data.getLittleEndian32();

            switch (number)
            {
                case InfoType.INFOTYPE_LOGON:
                    logonInfoVersion1(data);
                    break;

                case InfoType.INFOTYPE_LOGON_LONG:
                    logonInfoVersion2(data);
                    break;

                case InfoType.INFOTYPE_LOGON_PLAINNOTIFY:
                    plainNotify(data);
                    break;

                case InfoType.INFOTYPE_LOGON_EXTENDED_INFO:
                    logonInfoExtended(data);
                    break;

                default:
                    break;
            }

            if (!RDPClient.UseAltChecker)
            {
                // Флаг авторизации
                if (!RDPClient.FullXP)
                    RDPClient.GoodAuth = true;
            }

            // Прерываем выполнение
            RDPClient.timeoutTimer.Stop();
            RDPClient.m_bHalt = true;
        }

        private static void logonInfoVersion1(RdpPacket data)
        {
            var cbDomain = data.getLittleEndian32();
            var Domain = new byte[52];
            data.Read(Domain, 0, cbDomain);

            RDPClient.Domain = System.Text.Encoding.Unicode.GetString(Domain).Replace("\0", "");

            // Флаг авторизации
            if (RDPClient.FullXP)
                RDPClient.GoodAuth = true;
        }

        private static void logonInfoVersion2(RdpPacket data)
        {
            data.getLittleEndian16();
            data.getLittleEndian32();
            data.getLittleEndian32();
            var cbDomain = data.getLittleEndian32();
            data.Read(new byte[562], 0, 562); // Username + Padding
            var Domain = new byte[52];
            data.Read(Domain, 0, cbDomain);

            RDPClient.Domain = System.Text.Encoding.Unicode.GetString(Domain).Replace("\0", "");

            // Флаг авторизации
            if (RDPClient.FullXP)
                RDPClient.GoodAuth = true;
        }

        private static void plainNotify(RdpPacket data)
        {
            // Флаг авторизации
            if (RDPClient.FullXP)
                RDPClient.GoodAuth = true;
        }

        private static void logonInfoExtended(RdpPacket data)
        {
            // Флаг авторизации
            if (RDPClient.FullXP)
                RDPClient.GoodAuth = true;

            data.getLittleEndian16();
            var num2 = (FieldsPresent)data.getLittleEndian32();

            // Reconnect Cookie
            if (num2.HasFlag(FieldsPresent.LOGON_EX_AUTORECONNECTCOOKIE))
            {
                if (data.getLittleEndian32() != 0x1c)
                {
                    throw new Exception("Invalid length for AutoReconnectCookie!");
                }

                data.getLittleEndian32();
                RDPClient.LogonID = data.getLittleEndian32();
                if (RDPClient.ReconnectCookie == null)
                {
                    RDPClient.ReconnectCookie = new byte[0x10];
                }
                data.Read(RDPClient.ReconnectCookie, 0, RDPClient.ReconnectCookie.Length);
            }

            // Error
            if (num2.HasFlag(FieldsPresent.LOGON_EX_LOGONERRORS))
            {
                var ErrorType = (ErrorNotificationType)data.getLittleEndian32();
                var ErrorData = (ErrorNotificationData)data.getLittleEndian32();

                // Проверка ошибок
                if (ErrorType.HasFlag(ErrorNotificationType.LOGON_MSG_NO_PERMISSION) ||
                    ErrorType.HasFlag(ErrorNotificationType.LOGON_MSG_DISCONNECT_REFUSED) ||
                    ErrorType.HasFlag(ErrorNotificationType.LOGON_MSG_SESSION_TERMINATE) ||
                    ErrorData.HasFlag(ErrorNotificationData.LOGON_FAILED_BAD_PASSWORD) ||
                    ErrorData.HasFlag(ErrorNotificationData.LOGON_WARNING) ||
                    ErrorData.HasFlag(ErrorNotificationData.LOGON_FAILED_OTHER))
                {
                    // Флаг авторизации
                    if (RDPClient.FullXP)
                        RDPClient.GoodAuth = false;
                }

                // Проверка на смену пароля
                if (ErrorData.HasFlag(ErrorNotificationData.LOGON_FAILED_UPDATE_PASSWORD))
                {
                    // Флаг смены пароля
                    RDPClient.NeedChangePassword = true;

                    // Флаг авторизации
                    if (RDPClient.FullXP)
                        RDPClient.GoodAuth = true;
                }
            }
        }

        internal class InputInfo
        {
            public int Device_Flags;
            public Rdp.InputType Message_Type;
            public uint Param1;
            public uint Param2;
            public int Time;
            public DateTime TimeStamp;

            public InputInfo(int time, Rdp.InputType message_type, int device_flags, uint param1, uint param2)
            {
                this.Time = time;
                this.Message_Type = message_type;
                this.Device_Flags = device_flags;
                this.Param1 = param1;
                this.Param2 = param2;
                this.TimeStamp = DateTime.Now;
            }
        }

        [Flags]
        public enum InputType
        {
            INPUT_EVENT_MOUSE = 0x8001,
            INPUT_EVENT_SCANCODE = 4,
            INPUT_EVENT_SYNC = 0,
            INPUT_EVENT_UNICODE = 5
        }

        [Flags]
        public enum InfoType
        {
            INFOTYPE_LOGON = 0x00000000,
            INFOTYPE_LOGON_LONG = 0x00000001,
            INFOTYPE_LOGON_PLAINNOTIFY = 0x00000002,
            INFOTYPE_LOGON_EXTENDED_INFO = 0x00000003
        }

        [Flags]
        public enum FieldsPresent
        {
            LOGON_EX_AUTORECONNECTCOOKIE = 0x00000001,
            LOGON_EX_LOGONERRORS = 0x00000002
        }

        [Flags]
        public enum ErrorNotificationType : uint
        {
            LOGON_MSG_DISCONNECT_REFUSED = 0xFFFFFFF9,
            LOGON_MSG_NO_PERMISSION = 0xFFFFFFFA,
            LOGON_MSG_BUMP_OPTIONS = 0xFFFFFFFB,
            LOGON_MSG_RECONNECT_OPTIONS = 0xFFFFFFFC,
            LOGON_MSG_SESSION_TERMINATE = 0xFFFFFFFD,
            LOGON_MSG_SESSION_CONTINUE = 0xFFFFFFFE
        }

        [Flags]
        public enum ErrorNotificationData
        {
            LOGON_FAILED_BAD_PASSWORD = 0x00000000,
            LOGON_FAILED_UPDATE_PASSWORD = 0x00000001,
            LOGON_FAILED_OTHER = 0x00000002,
            LOGON_WARNING = 0x00000003
        }

    }
}