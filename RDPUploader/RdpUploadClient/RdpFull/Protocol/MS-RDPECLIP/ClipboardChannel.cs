using System;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RdpUploadClient
{
    internal class ClipboardChannel : IVirtualChannel
    {
        // Поля
        private int FourButesAlign = 4;
        private int FourButesAlignLen = 4;
        private GeneralFlags ServerGeneralFlags; // Серверные флаги доступа к буферу обмена
        private int ClipboardFormatID = 0xC004; // Format ID
        private string ClipboardFormatName = "FileGroupDescriptorW"; // Формат передачи 
        private string ClientTempDir = @"c:\temp\clipdata"; // Временная директория

        // Данные файла
        private static bool IsFileLoaded;
        private static byte[] FileData;
        private static string FileName = "clipdata.exe";
        private static ulong FileSize;
        private static ulong FileSizeLow;
        private static ulong FileSizeHigh;
        private static bool FileCanLoad = false;
        private static uint StreamID;
        private static int BufferNextPart;
        private static int Position;

        public void channel_process(RdpPacket data)
        {
            byte[] buffer = new byte[2];
            data.Read(buffer, 0, 2);
            var num = (MsgType)BitConverter.ToUInt16(buffer, 0);

            Debug.WriteLine(num);

            // Проверка, загружен ли файл
            if (!IsFileLoaded)
                return;

            switch (num)
            {
                case MsgType.CB_MONITOR_READY:
                    serverMonitorReady(data);
                    clientClipboardCapabilities();
                    //clientTemporaryDirectory();
                    clientFormatList();
                    break;

                case MsgType.CB_FORMAT_LIST:
                    break;

                case MsgType.CB_FORMAT_LIST_RESPONSE:
                    serverFormatListResponse(data);
                    break;

                case MsgType.CB_FORMAT_DATA_REQUEST:
                    serverFormatDataRequest(data);
                    clientFormatDataResponse();
                    break;

                case MsgType.CB_FORMAT_DATA_RESPONSE:
                    break;

                case MsgType.CB_TEMP_DIRECTORY:
                    break;

                case MsgType.CB_CLIP_CAPS:
                    serverClipboardCapabilities(data);
                    break;

                case MsgType.CB_FILECONTENTS_REQUEST:
                    serverFileContentsRequest(data);
                    clientFileContentsResponse();
                    break;

                case MsgType.CB_FILECONTENTS_RESPONSE:
                    break;

                case MsgType.CB_LOCK_CLIPDATA:
                    break;

                case MsgType.CB_UNLOCK_CLIPDATA:
                    break;

                default:
                    break;
            }
        }

        public void close()
        {
            FileData = null;
        }

        public int ChannelID
        {
            get
            {
                return 0x03ed; // 1005
            }
        }

        public string ChannelName
        {
            get
            {
                return "cliprdr\0";
            }
        }

        internal static void SetFile(byte[] fileData, string fileName)
        {
            if (fileData.Length > 0 && !string.IsNullOrWhiteSpace(fileName))
            {
                FileData = fileData;
                FileName = fileName.Replace(" ", "");
                FileSize = (ulong)fileData.Length;
                FileSizeLow = FileSize;
                FileSizeHigh = FileSize;
                IsFileLoaded = true;
            }
        }

        internal void serverClipboardCapabilities(RdpPacket data)
        {
            if (!((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.NOT_SET))
            {
                throw new Exception("Error NOT_SET message flag!");
            }

            int length = data.ReadLittleEndian32();

            int cCapabilitiesSets = data.ReadLittleEndian16();
            data.ReadLittleEndian16(); // Padding 2

            for (int i = 0; i < cCapabilitiesSets; i++)
            {
                if (data.ReadLittleEndian16() != (int)CapsType.CB_CAPSTYPE_GENERAL)
                {
                    throw new Exception("Error CB_CAPSTYPE_GENERAL value!");
                }

                int num = data.ReadLittleEndian16();

                if (num != 12)
                {
                    data.Position += (num - 4);
                }
                else
                {
                    data.ReadLittleEndian32(); // version
                    ServerGeneralFlags = (GeneralFlags)data.ReadLittleEndian32(); // generalFlags
                }
            }
        }

        internal void serverMonitorReady(RdpPacket data)
        {
            if (!((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.NOT_SET))
            {
                throw new Exception("Error NOT_SET message flag!");
            }

            data.ReadLittleEndian32(); // length
        }

        internal void clientClipboardCapabilities()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)MsgType.CB_CLIP_CAPS);
            packet.WriteLittleEndian16((short)MsgFlags.NOT_SET);
            packet.WriteLittleEndian32(16 + FourButesAlignLen); // length

            packet.WriteLittleEndian16(1); // Колл-во cCapabilitiesSets
            packet.WritePadding(2); // Padding 2

            packet.WriteLittleEndian16((short)CapsType.CB_CAPSTYPE_GENERAL);
            packet.WriteLittleEndian16(12); // lengthCapability
            packet.WriteLittleEndian32((int)CapsVersion.CB_CAPS_VERSION_2);

            if (ServerGeneralFlags.HasFlag(GeneralFlags.CB_STREAM_FILECLIP_ENABLED))
            {
                packet.WriteLittleEndian32((int)ServerGeneralFlags);
            }
            else
            {
                packet.WriteLittleEndian32((int)(GeneralFlags.CB_STREAM_FILECLIP_ENABLED | GeneralFlags.CB_USE_LONG_FORMAT_NAMES));
            }

            packet.WritePadding(FourButesAlign); // Add four bytes

            send(packet);
        }

        internal void clientTemporaryDirectory()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)MsgType.CB_TEMP_DIRECTORY);
            packet.WriteLittleEndian16((short)MsgFlags.NOT_SET);
            packet.WriteLittleEndian32(520 + FourButesAlignLen); // length

            // Формируем строку, содержащую нулевой байт после каждого символа
            string tempStr = "";
            foreach (var ch in ClientTempDir)
                tempStr += ch + "\0";

            // Получаем байты строки
            var bytes = ASCIIEncoding.GetBytes(tempStr, false).ToList();

            if (bytes.Count > 520)
            {
                packet.Write(bytes.ToArray(), 0, 520);
            }
            else
            {
                int endPos = 520 - bytes.Count;

                for (int i = 0; i < endPos; i++)
                    bytes.Add(0x00);

                packet.Write(bytes.ToArray(), 0, bytes.Count);
            }

            packet.WritePadding(FourButesAlign); // Add four bytes

            send(packet);
        }

        internal void clientFormatList()
        {
            // Формируем строку, содержащую нулевой байт после каждого символа
            string tempStr = "";
            foreach (var ch in ClipboardFormatName)
                tempStr += ch + "\0";

            // Получаем байты строки
            var bytes = ASCIIEncoding.GetBytes(tempStr, false).ToList();

            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)MsgType.CB_FORMAT_LIST);
            packet.WriteLittleEndian16((short)MsgFlags.NOT_SET);
            packet.WriteLittleEndian32(bytes.Count + 4 + FourButesAlignLen); // length

            // Устанавливаем Clipboard Format ID
            packet.WriteLittleEndian32(ClipboardFormatID);
            packet.Write(bytes.ToArray(), 0, bytes.Count);

            packet.WritePadding(FourButesAlign); // Add four bytes

            send(packet);
        }

        internal void serverFormatListResponse(RdpPacket data)
        {
            if (((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.CB_RESPONSE_OK))
            {
                Debug.WriteLine("Server FormatListResponse: OK");
            }
            else if (((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.CB_RESPONSE_FAIL))
            {
                Debug.WriteLine("Server FormatListResponse: FAIL");
            }

            data.ReadLittleEndian32(); // length
        }

        internal void serverFormatDataRequest(RdpPacket data)
        {
            if (!((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.NOT_SET))
            {
                throw new Exception("Error NOT_SET message flag!");
            }

            data.ReadLittleEndian32(); // length

            if (ClipboardFormatID != data.ReadLittleEndian32())
            {
                Debug.WriteLine("Error Clipboard Format ID!");
            }
        }

        internal void clientFormatDataResponse()
        {
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)MsgType.CB_FORMAT_DATA_RESPONSE);
            packet.WriteLittleEndian16((short)MsgFlags.CB_RESPONSE_OK);
            packet.WriteLittleEndian32(520 + 76 + FourButesAlignLen); // length

            // cItems
            packet.WriteLittleEndian32(1); // 1 файл в списке файлов буфера обмена

            // File Descriptor
            packet.WriteLittleEndian32((int)(FD_FLAGS.FD_ATTRIBUTES));
            packet.WritePadding(32); // Padding 32
            packet.WriteLittleEndian32((int)(FILE_ATTRIBUTE.FILE_ATTRIBUTE_NORMAL));
            packet.WritePadding(16); // Padding 16
            packet.WriteLittleEndian64(DateTime.Now.Ticks); // lastWriteTime
            packet.WriteLittleEndianU32((uint)FileSizeHigh);
            packet.WriteLittleEndianU32((uint)FileSizeLow);

            // Формируем строку, содержащую нулевой байт после каждого символа
            string tempStr = "";
            foreach (var ch in FileName)
                tempStr += ch + "\0";

            // Получаем байты строки
            var bytes = ASCIIEncoding.GetBytes(tempStr, false).ToList();

            if (bytes.Count > 520)
            {
                packet.Write(bytes.ToArray(), 0, 520);
            }
            else
            {
                int endPos = 520 - bytes.Count;

                for (int i = 0; i < endPos; i++)
                    bytes.Add(0x00);

                packet.Write(bytes.ToArray(), 0, bytes.Count);
            }

            packet.WritePadding(FourButesAlign); // Add four bytes

            send(packet);
        }

        internal void serverFileContentsRequest(RdpPacket data)
        {
            if (!((MsgFlags)data.ReadLittleEndian16()).HasFlag(MsgFlags.NOT_SET))
            {
                throw new Exception("Error NOT_SET message flag!");
            }

            data.ReadLittleEndian32(); // length

            StreamID = data.ReadLittleEndianU32(); // Stream ID
            data.ReadLittleEndianU32(); // lindex

            var flag = (FILECONTENTS_SIZE)data.ReadLittleEndianU32();

            if (flag.HasFlag(FILECONTENTS_SIZE.FILECONTENTS_SIZE))
            {
                Debug.WriteLine("FILECONTENTS_SIZE!");

                data.ReadLittleEndian32(); // 0x00000000
                data.ReadLittleEndian32(); // 0x00000000

                if (data.ReadLittleEndian32() != 0x00000008)
                {
                    Debug.WriteLine("The cbRequested field MUST be set to 0x00000008!");
                }

                data.ReadLittleEndian32(); // clipDataId

                FileCanLoad = false;
                Debug.WriteLine("FILECONTENTS_SIZE!");
            }
            else if (flag.HasFlag(FILECONTENTS_SIZE.FILECONTENTS_RANGE))
            {
                FileCanLoad = true;
                Debug.WriteLine("FILECONTENTS_RANGE!");

                data.ReadLittleEndian32();
                data.ReadLittleEndian32();

                BufferNextPart = data.ReadLittleEndian32(); // cbRequested
            }
        }

        internal void clientFileContentsResponse()
        {
            // Вычисляем часть файла
            byte[] tempArr = null;
            if ((FileData.Length - Position) > 0)
            {
                if ((FileData.Length - Position) >= BufferNextPart)
                {
                    tempArr = new byte[BufferNextPart];
                    Array.Copy(FileData, Position, tempArr, 0, BufferNextPart);
                    Position += BufferNextPart;
                }
                else
                {
                    tempArr = new byte[(FileData.Length - Position)];
                    Array.Copy(FileData, Position, tempArr, 0, (FileData.Length - Position));
                    BufferNextPart = 0;
                    Position = 0;
                }
            }
            else if ((FileData.Length - Position) == 0)
            {
                BufferNextPart = 0;
                Position = 0;
                return;
            }

            // Формируем пакет
            RdpPacket packet = new RdpPacket();
            packet.WriteLittleEndian16((short)MsgType.CB_FILECONTENTS_RESPONSE);
            packet.WriteLittleEndian16((short)MsgFlags.CB_RESPONSE_OK);

            // Устанавливаем длину сообщения
            if (FileCanLoad)
            {
                packet.WriteLittleEndian32(tempArr.Length + 4); // length
            }
            else
            {
                packet.WriteLittleEndian32(8 + 4); // length
            }

            // Stream ID
            packet.WriteLittleEndian32(StreamID); 

            // Устанавливаем размер или байты самого файла, в зависимости от значения FileCanLoad
            if (FileCanLoad && FileData != null)
            {
                // requestedFileContentsData
                packet.Write(tempArr, 0, tempArr.Length);
            }
            else
            {
                // requestedFileContentsData
                packet.WriteLittleEndianU64(FileSize);
            }

            send(packet);
        }

        private void send(RdpPacket data)
        {
            data.Position = 0L;
            int length = (int)data.Length;
            int count = Math.Min(length, 1600);
            int num = length - count;

            if (num == 0)
            {
                RdpPacket packet = new RdpPacket();
                packet.WriteLittleEndian32((int)length);
                packet.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_FIRST | CHANNEL_FLAG.CHANNEL_FLAG_LAST | CHANNEL_FLAG.CHANNEL_FLAG_SHOW_PROTOCOL));
                packet.copyToByteArray(data);

                IsoLayer.SendToCannel(packet, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0, this.ChannelID);
            }
            else
            {
                RdpPacket packet2 = new RdpPacket();
                packet2.WriteLittleEndian32((int)length);
                packet2.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_FIRST | CHANNEL_FLAG.CHANNEL_FLAG_SHOW_PROTOCOL));
                byte[] buffer = new byte[count];
                data.Read(buffer, 0, count);
                packet2.Write(buffer, 0, count);

                IsoLayer.SendToCannel(packet2, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0, this.ChannelID);
            }

            while (num > 0)
            {
                count = Math.Min(num, 1600);
                num -= count;
                RdpPacket packet3 = new RdpPacket();
                packet3.WriteLittleEndian32((int)length);

                if (num == 0)
                {
                    packet3.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_LAST | CHANNEL_FLAG.CHANNEL_FLAG_SHOW_PROTOCOL));
                }
                else
                {
                    packet3.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_SHOW_PROTOCOL));
                }

                byte[] buffer2 = new byte[count];
                data.Read(buffer2, 0, count);
                packet3.Write(buffer2, 0, count);
                
                IsoLayer.SendToCannel(packet3, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0, this.ChannelID);
            }
        }

        internal static void Print(RdpPacket data)
        {
            data.Position = 0L;

            int count = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (count == 16)
                {
                    count = 0;

                    Debug.Write(string.Format("0x{0:X02}", (short)data.ReadByte()).ToLower() + "\r\n");
                }
                else
                {
                    Debug.Write(string.Format("0x{0:X02}", (short)data.ReadByte()).ToLower() + " ");
                }

                count++;
            }
        }

        // Битовые флаги
        [Flags]
        private enum CHANNEL_FLAG
        {
            CHANNEL_FLAG_FIRST = 0x00000001,
            CHANNEL_FLAG_LAST = 0x00000002,
            CHANNEL_FLAG_SHOW_PROTOCOL = 0x00000010,
            CHANNEL_FLAG_SUSPEND = 0x00000020,
            CHANNEL_FLAG_RESUME = 0x00000040,
            CHANNEL_PACKET_COMPRESSED = 0x00200000,
            CHANNEL_PACKET_AT_FRONT = 0x00400000,
            CHANNEL_PACKET_FLUSHED = 0x00800000,
            CompressionTypeMask = 0x000F0000
        }

        [Flags]
        private enum MsgType
        {
            CB_MONITOR_READY = 0x0001,
            CB_FORMAT_LIST = 0x0002,
            CB_FORMAT_LIST_RESPONSE = 0x0003,
            CB_FORMAT_DATA_REQUEST = 0x0004,
            CB_FORMAT_DATA_RESPONSE = 0x0005,
            CB_TEMP_DIRECTORY = 0x0006,
            CB_CLIP_CAPS = 0x0007,
            CB_FILECONTENTS_REQUEST = 0x0008,
            CB_FILECONTENTS_RESPONSE = 0x0009,
            CB_LOCK_CLIPDATA = 0x000A,
            CB_UNLOCK_CLIPDATA = 0x000B,
        }

        [Flags]
        private enum MsgFlags
        {
            NOT_SET = 0x0000,
            CB_RESPONSE_OK = 0x0001,
            CB_RESPONSE_FAIL = 0x0002,
            CB_ASCII_NAMES = 0x0004
        }

        [Flags]
        private enum CapsType
        {
            CB_CAPSTYPE_GENERAL = 0x0001
        }

        [Flags]
        private enum CapsVersion
        {
            CB_CAPS_VERSION_1 = 0x00000001,
            CB_CAPS_VERSION_2 = 0x00000002
        }

        [Flags]
        private enum GeneralFlags
        {
            NOT_SET = 0x00000000,
            CB_USE_LONG_FORMAT_NAMES = 0x00000002,
            CB_STREAM_FILECLIP_ENABLED = 0x00000004,
            CB_FILECLIP_NO_FILE_PATHS = 0x00000008,
            CB_CAN_LOCK_CLIPDATA = 0x00000010
        }

        [Flags]
        private enum MappingMode
        {
            MM_TEXT = 0x00000001,
            MM_LOMETRIC = 0x00000002,
            MM_HIMETRIC = 0x00000003,
            MM_LOENGLISH = 0x00000004,
            MM_HIENGLISH = 0x00000005,
            MM_TWIPS = 0x00000006,
            MM_ISOTROPIC = 0x00000007,
            MM_ANISOTROPIC = 0x00000008
        }

        [Flags]
        private enum FD_FLAGS
        {
            FD_ATTRIBUTES = 0x00000004,
            FD_FILESIZE = 0x00000040,
            FD_WRITESTIME = 0x00000020,
            FD_SHOWPROGRESSUI = 0x00004000
        }

        [Flags]
        private enum FILE_ATTRIBUTE
        {
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_NORMAL = 0x00000080
        }

        [Flags]
        private enum FILECONTENTS_SIZE
        {
            FILECONTENTS_SIZE = 0x00000001,
            FILECONTENTS_RANGE = 0x00000002
        }

    }
}