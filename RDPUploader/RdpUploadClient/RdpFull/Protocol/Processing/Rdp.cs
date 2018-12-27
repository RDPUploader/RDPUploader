using System;
using System.Collections.Generic;
using System.Threading;

namespace RdpUploadClient
{
    internal class Rdp
    {
        private static bool m_bExceptionReported = false;
        private static bool m_bHalt = false;
        internal static Thread m_CommsThread;
        private static Thread m_InputThread;
        private static List<InputInfo> m_InputCache = new List<InputInfo>();
        private static DateTime m_KeepAliveTimer = DateTime.Now;
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

        public static void Start()
        {
            // Main
            m_CommsThread = new Thread(new ThreadStart(Rdp.mainloop));
            m_CommsThread.IsBackground = true;
            m_CommsThread.Start();

            // Input
            m_InputThread = new Thread(new ThreadStart(Rdp.inputloop));
            m_InputThread.IsBackground = true;
            m_InputThread.Start();
        }

        // Halt
        public static void Halt()
        {
            m_bHalt = true;
            foreach (IVirtualChannel channel in Channels.RegisteredChannels)
            {
                channel.close();
            }

            if (Network.Connected)
                Network.Close();

            m_CommsThread.Join();
            m_InputThread.Join();
        }

        public static bool IsHalted()
        {
            return (((m_CommsThread == null) || !m_CommsThread.IsAlive) && ((m_InputThread == null) || !m_InputThread.IsAlive));
        }

        // Main
        internal static void mainloop()
        {
            ControlFlow.m_bInitialised = false;
            m_bHalt = false;
            m_bExceptionReported = false;

            try
            {
                while (!m_bHalt)
                {
                    int num;
                    RdpPacket data = ISO.Secure_receive(out num);
                    m_KeepAliveTimer = DateTime.Now;

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
                            catch { }

                            Network.ConnectionAlive = true;
                            break;

                        case 7:
                            processData(data);
                            break;

                        case 10:
                            ControlFlow.processRedirection(data, false);
                            break;

                        case 0xff:
                            FastPathUpdate.ProcessFastPathUpdate(data);
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
                if (!m_bExceptionReported)
                {
                    m_bExceptionReported = true;
                    Options.OnError(exception);
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
                if (!m_bExceptionReported)
                {
                    m_bExceptionReported = true;
                    Options.OnError(exception2);
                }
            }
            finally
            {
                ISO.Disconnect();
                Options.OnClosed();
            }
        }
        
        private static bool processData(RdpPacket data)
        {
            int num3, num = 0;
            data.Position += 6L;
            data.ReadLittleEndian16();
            num = data.ReadByte();
            data.ReadByte();
            data.ReadLittleEndian16();

            switch (num)
            {
                case 0x26:
                    // RDP_DATA_PDU_SAVE_SESSION_INFO
                    processLogonInfo(data);
                    goto Label_015E;

                case 0x2f:
                    // RDP_DATA_PDU_SET_ERROR_INFO
                    num3 = data.ReadLittleEndian32();

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
                    processUpdate(data);
                    return false;

                case 0x1b:
                    return false;

                default:
                    goto Label_015E;
            }

            // Неизвестная ошибка, наблюдалась на виртуалке нестабильной.
            if (num3 == 4498)
                return false;

            throw new RDFatalException("Error code: " + num3.ToString("X8") + ", please contact Developer with this error code");

        Label_015E:
            return false;
        }

        private static void processLogonInfo(RdpPacket data)
        {
            // Вызываем событие инициализации
            Options.OnAutorization();

            int num;
            switch (data.ReadLittleEndian32())
            {
                case 0:
                case 1:
                case 2:
                    return;

                case 3:
                    data.ReadLittleEndian16();
                    num = data.ReadLittleEndian32();

                    if ((num & 1) != 0)
                    {
                        if (data.ReadLittleEndian32() != 0x1c)
                        {
                            throw new Exception("Invalid length for AutoReconnectCookie!");
                        }

                        data.ReadLittleEndian32();
                        Options.LogonID = data.ReadLittleEndian32();

                        if (Options.ReconnectCookie == null)
                        {
                            Options.ReconnectCookie = new byte[0x10];
                        }

                        data.Read(Options.ReconnectCookie, 0, 0x10);
                        break;
                    }
                    break;

                default:
                    return;
            }

            if ((num & 2) != 0)
            {
                data.ReadLittleEndian32();
                data.ReadLittleEndian32();
            }
        }

        internal static void processUpdate(RdpPacket data)
        {
            switch (data.ReadLittleEndian16())
            {
                case 0:
                    data.Position += 2L;
                    int num2 = data.ReadLittleEndian16();
                    data.Position += 2L;
                    Orders.processOrders(data, ISO.next_packet, num2);
                    return;

                case 1:
                    Bitmaps.processBitmapUpdates(data);
                    return;

                case 2:
                    Palette.processPalette(data);
                    break;

                case 3:
                    break;

                default:
                    return;
            }
        }

        // Input
        internal static void inputloop()
        {
            m_bHalt = false;
            m_bExceptionReported = false;
            m_InputCache.Clear();
            m_KeepAliveTimer = DateTime.Now;

            try
            {
                List<InputInfo> inputToSend = new List<InputInfo>();
                List<InputInfo> list2 = new List<InputInfo>();

                while (!m_bHalt)
                {
                    lock (m_InputCache)
                    {
                        inputToSend.Clear();
                        DateTime now = DateTime.Now;

                        foreach (InputInfo info in m_InputCache)
                        {
                            TimeSpan span = (TimeSpan)(now - info.TimeStamp);

                            if (span.TotalMilliseconds > 80)
                            {
                                inputToSend.Add(info);
                                list2.Add(info);
                            }
                        }

                        TimeSpan span2 = (TimeSpan)(DateTime.Now - m_KeepAliveTimer);

                        if (span2.TotalSeconds > 30)
                        {
                            IsoLayer.RefreshRect(new Rectangle[] { new Rectangle(0, 0, 0x20, 0x20) });
                        }

                        foreach (InputInfo info2 in list2)
                        {
                            m_InputCache.Remove(info2);
                        }

                        list2.Clear();
                    }

                    if (inputToSend.Count > 0)
                    {
                        IsoLayer.FastSendInput(inputToSend);
                    }

                    Thread.Sleep(60);
                }
            }
            catch (EndOfTransmissionException)
            {
                Options.OnClosed();
            }
            catch (SocketAbortException)
            {
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception exception)
            {
                if (!m_bExceptionReported)
                {
                    m_bExceptionReported = true;
                    Options.OnError(exception);
                }
            }
        }

        public static void SendInput(int time, InputType message_type, int device_flags, uint param1, uint param2)
        {
            lock (m_InputCache)
            {
                m_InputCache.Add(new InputInfo(time, message_type, device_flags, param1, param2));
            }
        }

        // Битовые флаги дейтсвий мышки и клавиатуры (SlowPath и FastPath)
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

        // Slow Path
        [Flags]
        internal enum InputType
        {
            INPUT_EVENT_SYNC = 0x0000,
            INPUT_EVENT_SCANCODE = 0x0004,
            INPUT_EVENT_UNICODE = 0x0005,
            INPUT_EVENT_MOUSE = 0x8001,
            INPUT_EVENT_MOUSEX = 0x8002
        }

        [Flags]
        internal enum KeyboardFlags
        {
            KBDFLAGS_EXTENDED = 0x0100,
            KBDFLAGS_EXTENDED1 = 0x0200,
            KBDFLAGS_DOWN = 0x4000,
            KBDFLAGS_RELEASE = 0x8000
        }

        [Flags]
        internal enum MouseInputType
        {
            PTRFLAGS_MOVE = 0x0800,
            PTRFLAGS_DOWN = 0x8000,
            PTRFLAGS_BUTTON1 = 0x1000,
            PTRFLAGS_BUTTON2 = 0x2000,
            PTRFLAGS_BUTTON3 = 0x4000
        }

        // Fast Path
        [Flags]
        internal enum FastInputType
        {
            FASTPATH_INPUT_EVENT_SCANCODE = 0x0,
            FASTPATH_INPUT_EVENT_MOUSE = 0x1,
            FASTPATH_INPUT_EVENT_MOUSEX = 0x2,
            FASTPATH_INPUT_EVENT_SYNC = 0x3,
            FASTPATH_INPUT_EVENT_UNICODE = 0x4
        }

        [Flags]
        internal enum FastKeyboardFlags
        {
            FASTPATH_INPUT_KBDFLAGS_RELEASE = 0x01,
            FASTPATH_INPUT_KBDFLAGS_EXTENDED = 0x02
        }

        [Flags]
        internal enum LogonFields
        {
            LOGON_EX_AUTORECONNECTCOOKIE = 1,
            LOGON_EX_LOGONERRORS = 2
        }

    }
}