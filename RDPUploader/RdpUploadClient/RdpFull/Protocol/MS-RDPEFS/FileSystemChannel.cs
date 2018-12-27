using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace RdpUploadClient
{
    internal class FileSystemChannel : IVirtualChannel
    {
        private int m_ClientID;
        private string m_sDeviceName;
        private string m_sDriveName;
        internal static string[] m_DirectoryContents;
        private static IsolatedStorageFile m_IsolatedStorage;
        private static int m_nDirectoryContent = 0;
        private static string m_RootFolder = @"\FileSystem";
        private static int FILE_CHARACTERISTIC_TS_DEVICE = 0x1000;
        private static int FILE_CHARACTERISTIC_WEBDAV_DEVICE = 0x2000;
        private static int FILE_DEVICE_CD_ROM = 2;
        private static int FILE_DEVICE_DISK = 7;
        private static int FILE_DEVICE_IS_MOUNTED = 0x20;
        private static int FILE_DEVICE_SECURE_OPEN = 0x100;
        private const int FILE_DIRECTORY_FILE = 1;
        private static int FILE_FLOPPY_DISKETTE = 4;
        private static int FILE_READ_ONLY_DEVICE = 2;
        private static int FILE_REMOTE_DEVICE = 0x10;
        private static int FILE_REMOVABLE_MEDIA = 1;
        private static int FILE_VIRTUAL_VOLUME = 0x40;
        private static int FILE_WRITE_ONCE_MEDIA = 8;
        private const int FileAttributeTagInformation = 0x23;
        private const int FileBasicInformation = 4;
        private const int FileStandardInformation = 5;
        private static uint IOSTATUS_ENDOFFILE = 0xc0000011;
        private static uint IOSTATUS_INVALID_HANDLE = 0xc0000008;
        private static uint IOSTATUS_INVALID_PARAMETER = 0xc000000d;
        private static uint IOSTATUS_NO_MORE_FILES = 0x80000006;
        private static uint IOSTATUS_NOSUCHFILE = 0xc000000f;
        private static uint IOSTATUS_SUCCESS = 0;
        private static string[] m_StringBuffer = new string[0x100];
        private const int MJ_CLOSE = 2;
        private const int MJ_CREATE = 0;
        private const int MJ_DEVICE_CONTROL = 14;
        private const int MJ_DIRECTORY_CONTROL = 12;
        private const int MJ_LOCK_CONTROL = 0x11;
        private const int MJ_QUERY_INFORMATION = 5;
        private const int MJ_QUERY_VOLUME_INFORMATION = 10;
        private const int MJ_READ = 3;
        private const int MJ_SET_INFORMATION = 6;
        private const int MJ_WRITE = 4;
        private const int PAKID_CORE_CLIENT_CAPABILITY = 0x4350;
        private const int PAKID_CORE_CLIENT_NAME = 0x434e;
        private const int PAKID_CORE_CLIENTID_CONFIRM = 0x4343;
        private const int PAKID_CORE_DEVICE_IOCOMPLETION = 0x4943;
        private const int PAKID_CORE_DEVICE_IOREQUEST = 0x4952;
        private const int PAKID_CORE_DEVICE_REPLY = 0x6472;
        private const int PAKID_CORE_DEVICELIST_ANNOUNCE = 0x4441;
        private const int PAKID_CORE_DEVICELIST_REMOVE = 0x444d;
        private const int PAKID_CORE_SERVER_ANNOUNCE = 0x496e;
        private const int PAKID_CORE_SERVER_CAPABILITY = 0x5350;
        private const int PAKID_CORE_USER_LOGGEDON = 0x554c;
        private const int PAKID_PRN_CACHE_DATA = 0x5043;
        private const int PAKID_PRN_USING_XPS = 0x5543;
        private const int RDPDR_CTYP_CORE = 0x4472;
        private const int RDPDR_CTYP_PRN = 0x5052;
        private const int RDPDR_DTYP_SERIAL = 0x00000001;
        private const int RDPDR_DTYP_PARALLEL = 0x00000002;
        private const int RDPDR_DTYP_PRINT = 0x00000004;
        private const int RDPDR_DTYP_FILESYSTEM = 0x00000008;
        private const int RDPDR_DTYP_SMARTCARD = 0x00000020;
        
        public FileSystemChannel(string sDeviceName, string sDriveName)
        {
            this.m_sDeviceName = sDeviceName;
            this.m_sDriveName = sDriveName;
            byte[] data = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(data);
            this.m_ClientID = BitConverter.ToInt32(data, 0);
        }

        // Загрузить файл в изолированное хранилище
        internal static void LoadFileToStorage(byte[] file, string fileName, int timeout, int loadTimeout)
        {
            // Инициализация изолированного хранилища
            if (m_IsolatedStorage == null)
            {
                // Получаем изолированное хранилище для домена
                m_IsolatedStorage = IsolatedStorageFile.GetUserStoreForDomain();

                if (!m_IsolatedStorage.DirectoryExists(m_RootFolder))
                {
                    m_IsolatedStorage.CreateDirectory(RootFolder);
                }
                else
                {
                    m_IsolatedStorage.Remove();
                    m_IsolatedStorage = IsolatedStorageFile.GetUserStoreForDomain();
                    m_IsolatedStorage.CreateDirectory(RootFolder);
                }
            }

            string virtualFile = FileSystemChannel.RootFolder + @"\" + fileName.Trim();
            using (var storage = IsolatedStorageFile.GetUserStoreForDomain())
            {
                if (!storage.FileExists(virtualFile))
                {
                    Thread.Sleep(1000);
                    storage.CreateFile(virtualFile).Close();
                    Thread.Sleep(timeout / 2);

                    using (var localStream = new MemoryStream(file))
                    {
                        using (var stream = new IsolatedStorageFileStream(virtualFile, FileMode.Create, storage))
                        {
                            localStream.CopyTo(stream);
                        }
                    }
                }
            }

            Thread.Sleep(loadTimeout);
        }

        public void channel_process(RdpPacket data)
        {
            try
            {
                byte[] buffer = new byte[4];
                data.Read(buffer, 0, 4);
                string str = ASCIIEncoding.GetString(buffer, 0, 4);

                switch (str)
                {
                    case null:
                        return;

                    case "rDnI":
                        {
                            data.ReadLittleEndian16();
                            int minorVersion = data.ReadLittleEndian16();
                            int clientId = data.ReadLittleEndian32();

                            if (minorVersion < 12)
                            {
                                clientId = this.m_ClientID;
                            }

                            this.open();
                            this.sendClientAnnounce(clientId, minorVersion);
                            this.sendClientName();
                            return;
                        }

                    case "rDPS":
                        this.sendClientCapability();
                        return;

                    case "rDCC":
                        this.sendDeviceListAnnounce();
                        return;

                    case "rDrd":
                        data.ReadLittleEndian32();
                        data.ReadLittleEndian32();
                        return;
                }

                if (str != "rDRI")
                {
                    throw new Exception("Unknown packet: " + str);
                }

                try
                {
                    this.deviceIORequest(data);
                }
                catch { }
            }
            catch { }
        }

        public void close()
        {
            FileIOManager.closeFiles();
            if (m_IsolatedStorage != null)
            {
                m_IsolatedStorage.Dispose();
                m_IsolatedStorage = null;
            }
        }

        public int ChannelID
        {
            get
            {
                return 0x3ec; // 1004
            }
        }

        public string ChannelName
        {
            get
            {
                return "rdpdr\0\0\0";
            }
        }

        public void open()
        {
            if (m_IsolatedStorage == null)
            {
                // Получаем изолированное хранилище для домена
                m_IsolatedStorage = IsolatedStorageFile.GetUserStoreForDomain();

                if (!m_IsolatedStorage.DirectoryExists(m_RootFolder))
                {
                    m_IsolatedStorage.CreateDirectory(RootFolder);
                }
                else
                {
                    m_IsolatedStorage.Remove();
                    m_IsolatedStorage = IsolatedStorageFile.GetUserStoreForDomain();
                    m_IsolatedStorage.CreateDirectory(RootFolder);
                }
            }
        }

        public static string RootFolder
        {
            get
            {
                return m_RootFolder;
            }
        }

        private void deviceIORequest(RdpPacket data)
        {
            uint num;
            uint num2;
            uint num3;
            uint num4;
            uint num5;
            int num6;
            string str;
            int num7;
            RdpPacket packet;
            byte[] buffer;
            int deviceId = data.ReadLittleEndian32();
            int num9 = data.ReadLittleEndian32();
            int completionId = data.ReadLittleEndian32();
            int num11 = data.ReadLittleEndian32();
            int num12 = data.ReadLittleEndian32();
            int num30 = num11;

            switch (num30)
            {
                case 0:
                {
                    num = (uint) data.ReadBigEndian32();
                    data.Position += 8L;
                    num2 = (uint) data.ReadLittleEndian32();
                    num3 = (uint) data.ReadLittleEndian32();
                    num4 = (uint) data.ReadLittleEndian32();
                    num5 = (uint) data.ReadLittleEndian32();
                    num6 = data.ReadLittleEndian32();
                    if (num6 == 0)
                    {
                        str = "";
                        break;
                    }
                    byte[] buffer2 = new byte[num6 - 2];
                    data.Read(buffer2, 0, num6 - 2);
                    str = Encoding.Unicode.GetString(buffer2, 0, buffer2.Length);
                    break;
                }

                case 1:
                case 7:
                case 8:
                case 9:
                case 11:
                case 13:
                case 15:
                case 0x10:
                    return;

                case 2:
                {
                    FileIOHandle handle = FileIOManager.getFile(num9);
                    try
                    {
                        if (handle != null)
                        {
                            handle.Close();
                            if (handle.tempFile)
                            {
                                if (!handle.directory)
                                {
                                    m_IsolatedStorage.DeleteFile(this.getFullPath(handle.path));
                                }
                                else
                                {
                                    m_IsolatedStorage.DeleteDirectory(this.getFullPath(handle.path));
                                }
                            }
                        }
                        FileIOManager.clearFile(num9);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[1], 1);
                    }
                    catch (Exception)
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_INVALID_PARAMETER, 0, new byte[1], 1);
                    }
                    return;
                }

                case 3:
                {
                    int count = data.ReadLittleEndian32();
                    int num14 = data.ReadLittleEndian32();
                    try
                    {
                        FileIOHandle handle2 = FileIOManager.getFile(num9);
                        byte[] buffer3 = new byte[count];
                        handle2.stream.Seek((long) num14, SeekOrigin.Begin);
                        int fileHandle = handle2.stream.Read(buffer3, 0, count);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, fileHandle, buffer3, fileHandle);
                    }
                    catch
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_ENDOFFILE, 0, null, 1);
                    }
                    return;
                }

                case 4:
                {
                    int num16 = data.ReadLittleEndian32();
                    int num17 = data.ReadLittleEndian32();
                    data.Position += 0x18L;
                    try
                    {
                        FileIOHandle handle3 = FileIOManager.getFile(num9);
                        handle3.stream.Seek((long) num17, SeekOrigin.Begin);
                        byte[] buffer4 = new byte[num16];
                        data.Read(buffer4, 0, num16);
                        handle3.stream.Write(buffer4, 0, num16);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, num16, null, 1);
                    }
                    catch
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_ENDOFFILE, 0, null, 1);
                    }
                    return;
                }

                case 5:
                    try
                    {
                        num7 = data.ReadLittleEndian32();
                        long ticks = DateTime.Now.Ticks;
                        FileIOHandle handle4 = FileIOManager.getFile(num9);
                        if (handle4 == null)
                        {
                            throw new FileSystemException(IOSTATUS_INVALID_HANDLE);
                        }
                        int length = 0;
                        string fullpath = handle4.fullpath;
                        Path.GetFileName(handle4.path);
                        long num18 = m_IsolatedStorage.GetCreationTime(fullpath).ToUniversalTime().ToFileTime();
                        long num19 = m_IsolatedStorage.GetLastAccessTime(fullpath).ToUniversalTime().ToFileTime();
                        long num20 = m_IsolatedStorage.GetLastWriteTime(fullpath).ToUniversalTime().ToFileTime();
                        if (!m_IsolatedStorage.DirectoryExists(fullpath))
                        {
                            if (handle4.stream != null)
                            {
                                length = (int) handle4.stream.Length;
                            }
                            else
                            {
                                using (FileStream stream = m_IsolatedStorage.OpenFile(fullpath, FileMode.Open))
                                {
                                    length = (int) stream.Length;
                                }
                            }
                        }
                        packet = new RdpPacket();
                        switch (num7)
                        {
                            case 5:
                                if (handle4.directory)
                                {
                                    packet.WriteLittleEndian32(0x60);
                                    packet.WriteLittleEndian32(0);
                                    packet.WriteLittleEndian32(0x60);
                                    packet.WriteLittleEndian32(0);
                                }
                                else
                                {
                                    packet.WriteLittleEndian32(length);
                                    packet.WriteLittleEndian32(0);
                                    packet.WriteLittleEndian32(length);
                                    packet.WriteLittleEndian32(0);
                                }
                                packet.WriteLittleEndian32(0);
                                packet.WriteByte(0);
                                if (handle4.directory)
                                {
                                    packet.WriteByte(1);
                                }
                                else
                                {
                                    packet.WriteByte(0);
                                }
                                break;

                            case 0x23:
                                if (handle4.directory)
                                {
                                    packet.WriteLittleEndianU32(0x10);
                                }
                                else
                                {
                                    packet.WriteLittleEndianU32(0x80);
                                }
                                packet.WriteLittleEndianU32(0);
                                break;

                            case 4:
                                packet.WriteLittleEndianU32((uint) num18);
                                packet.WriteLittleEndianU32((uint) (num18 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num19);
                                packet.WriteLittleEndianU32((uint) (num19 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num20);
                                packet.WriteLittleEndianU32((uint) (num20 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num20);
                                packet.WriteLittleEndianU32((uint) (num20 >> 0x10));
                                if (handle4.directory)
                                {
                                    packet.WriteLittleEndianU32(0x10);
                                }
                                else
                                {
                                    packet.WriteLittleEndianU32(0x80);
                                }
                                break;
                        }
                        buffer = new byte[packet.Length];
                        packet.Position = 0L;
                        packet.Read(buffer, 0, buffer.Length);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, buffer.Length, buffer, buffer.Length);
                    }
                    catch (FileSystemException exception)
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, exception.Status, 0, null, 1);
                    }
                    catch
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_NOSUCHFILE, 0, null, 1);
                    }
                    return;

                case 6:
                    num7 = data.ReadLittleEndian32();
                    try
                    {
                        FileIOHandle handle5 = FileIOManager.getFile(num9);
                        if (handle5 == null)
                        {
                            throw new Exception("FileId is not open!");
                        }
                        this.setAttributes(deviceId, num9, completionId, handle5.path, num7, data);
                    }
                    catch (Exception)
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_NOSUCHFILE, 0, null, 1);
                    }
                    return;

                case 10:
                    num7 = data.ReadLittleEndian32();
                    packet = new RdpPacket();
                    switch (num7)
                    {
                        case 1:
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x1e240);
                            packet.WriteLittleEndian32((int) ((this.m_sDriveName.Length * 2) + 2));
                            packet.WriteByte(0);
                            packet.WriteByte(0);
                            packet.WriteUnicodeString(this.m_sDriveName);
                            goto Label_0723;

                        case 3:
                            packet.WriteLittleEndian32(0x186a0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x186a0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x200);
                            packet.WriteLittleEndian32(0x200);
                            goto Label_0723;

                        case 4:
                            packet.WriteLittleEndian32(FILE_DEVICE_DISK);
                            packet.WriteLittleEndian32(FILE_REMOTE_DEVICE);
                            return;

                        case 5:
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0xff);
                            packet.WriteLittleEndian32((int) (("fat32".Length * 2) + 2));
                            packet.WriteUnicodeString("fat32");
                            goto Label_0723;

                        case 7:
                            packet.WriteLittleEndian32(0x186a0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x186a0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x186a0);
                            packet.WriteLittleEndian32(0);
                            packet.WriteLittleEndian32(0x200);
                            packet.WriteLittleEndian32(0x200);
                            goto Label_0723;
                    }
                    this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_INVALID_PARAMETER, 0, null, 0);
                    goto Label_0723;

                case 12:
                    try
                    {
                        if (num12 != 1)
                        {
                            return;
                        }
                        FileIOHandle handle6 = FileIOManager.getFile(num9);
                        if (handle6 == null)
                        {
                            throw new FileSystemException(IOSTATUS_INVALID_HANDLE);
                        }
                        num7 = data.ReadLittleEndian32();
                        int num22 = data.ReadByte();
                        num6 = data.ReadLittleEndian32();
                        data.Position += 0x17L;
                        if ((num22 > 0) && (num6 > 0))
                        {
                            byte[] buffer5 = new byte[num6 - 2];
                            data.Read(buffer5, 0, num6 - 2);
                            m_StringBuffer[completionId] = Encoding.Unicode.GetString(buffer5, 0, buffer5.Length);
                            string str3 = Path.GetFileName(m_StringBuffer[completionId]);
                            string str4 = handle6.fullpath;
                            if (!str4.EndsWith(@"\"))
                            {
                                str4 = str4 + @"\";
                            }
                            string[] directoryNames = m_IsolatedStorage.GetDirectoryNames(str4 + str3);
                            string[] fileNames = m_IsolatedStorage.GetFileNames(str4 + str3);
                            m_DirectoryContents = new string[directoryNames.Length + fileNames.Length];
                            int num23 = 0;
                            foreach (string str5 in directoryNames)
                            {
                                m_DirectoryContents[num23++] = str4 + str5;
                            }
                            foreach (string str6 in fileNames)
                            {
                                m_DirectoryContents[num23++] = str4 + str6;
                            }
                            m_nDirectoryContent = -1;
                        }
                        m_nDirectoryContent++;
                        packet = new RdpPacket();
                        if (m_nDirectoryContent >= m_DirectoryContents.Length)
                        {
                            goto Label_0D67;
                        }
                        bool flag2 = false;
                        int num27 = 0;
                        string path = m_DirectoryContents[m_nDirectoryContent];
                        string fileName = Path.GetFileName(path);
                        long num24 = m_IsolatedStorage.GetCreationTime(path).ToUniversalTime().ToFileTime();
                        long num25 = m_IsolatedStorage.GetLastAccessTime(path).ToUniversalTime().ToFileTime();
                        long num26 = m_IsolatedStorage.GetLastWriteTime(path).ToUniversalTime().ToFileTime();
                        flag2 = m_IsolatedStorage.DirectoryExists(path);
                        if (!flag2)
                        {
                            num27 = FileIOManager.getFileLength(path);
                            if (num27 == -1)
                            {
                                num27 = 0;
                                try
                                {
                                    using (FileStream stream2 = m_IsolatedStorage.OpenFile(path, FileMode.Open))
                                    {
                                        num27 = (int) stream2.Length;
                                    }
                                }
                                catch { }
                            }
                        }
                        switch (num7)
                        {
                            case 1:
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndianU32((uint)num24);
                                packet.WriteLittleEndianU32((uint)(num24 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num25);
                                packet.WriteLittleEndianU32((uint)(num25 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num26);
                                packet.WriteLittleEndianU32((uint)(num26 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num26);
                                packet.WriteLittleEndianU32((uint)(num26 >> 0x10));
                                if (!flag2)
                                {
                                    goto Label_0CB8;
                                }
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                goto Label_0CDA;

                            case 2:
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndianU32((uint)num24);
                                packet.WriteLittleEndianU32((uint)(num24 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num25);
                                packet.WriteLittleEndianU32((uint)(num25 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num26);
                                packet.WriteLittleEndianU32((uint)(num26 >> 0x10));
                                packet.WriteLittleEndianU32((uint)num26);
                                packet.WriteLittleEndianU32((uint)(num26 >> 0x10));
                                if (!flag2)
                                {
                                    goto Label_0B8F;
                                }
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                goto Label_0BB1;

                            case 3:
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndianU32((uint) num24);
                                packet.WriteLittleEndianU32((uint) (num24 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num25);
                                packet.WriteLittleEndianU32((uint) (num25 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num26);
                                packet.WriteLittleEndianU32((uint) (num26 >> 0x10));
                                packet.WriteLittleEndianU32((uint) num26);
                                packet.WriteLittleEndianU32((uint) (num26 >> 0x10));
                                if (!flag2)
                                {
                                    break;
                                }
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                packet.WriteLittleEndianU32(0x60);
                                packet.WriteLittleEndianU32(0);
                                goto Label_0A9F;

                            case 12:
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndian32(0);
                                packet.WriteLittleEndian32((int) ((fileName.Length * 2) + 2));
                                packet.WriteUnicodeString(fileName);
                                goto Label_0D29;

                            default:
                                throw new Exception("Unsupported infoClass " + num7);
                        }
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                    Label_0A9F:
                        if (flag2)
                        {
                            packet.WriteLittleEndianU32(0x10);
                        }
                        else
                        {
                            packet.WriteLittleEndianU32(0x80);
                        }
                        packet.WriteLittleEndian32((int) ((fileName.Length * 2) + 2));
                        packet.WriteLittleEndian32(0);
                        packet.WriteByte(0);
                        packet.Position += 0x18L;
                        packet.WriteUnicodeString(fileName);
                        goto Label_0D29;
                    Label_0B8F:
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                    Label_0BB1:
                        if (flag2)
                        {
                            packet.WriteLittleEndianU32(0x10);
                        }
                        else
                        {
                            packet.WriteLittleEndianU32(0x80);
                        }
                        packet.WriteLittleEndian32((int) ((fileName.Length * 2) + 2));
                        packet.WriteLittleEndian32(0);
                        packet.WriteUnicodeString(fileName);
                        goto Label_0D29;
                    Label_0CB8:
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                        packet.WriteLittleEndianU32((uint) num27);
                        packet.WriteLittleEndianU32(0);
                    Label_0CDA:
                        if (flag2)
                        {
                            packet.WriteLittleEndianU32(0x10);
                        }
                        else
                        {
                            packet.WriteLittleEndianU32(0x80);
                        }
                        packet.WriteLittleEndian32((int) ((fileName.Length * 2) + 2));
                        packet.WriteUnicodeString(fileName);
                    Label_0D29:
                        buffer = new byte[packet.Length];
                        packet.Position = 0L;
                        packet.Read(buffer, 0, buffer.Length);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, buffer.Length, buffer, buffer.Length);
                        return;
                    Label_0D67:
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_NO_MORE_FILES, 0, null, 1);
                    }
                    catch (FileSystemException exception2)
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, exception2.Status, 0, null, 0);
                    }
                    catch (Exception)
                    {
                        this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_NO_MORE_FILES, 0, null, 0);
                    }
                    return;

                case 14:
                {
                    data.ReadLittleEndian32();
                    data.ReadLittleEndian32();
                    int num28 = data.ReadLittleEndian32();
                    data.Position += 20L;
                    uint ioStatus = 0;
                    if (((num28 >> 0x10) != 20) || ((num28 >> 0x10) != 9))
                    {
                        ioStatus = IOSTATUS_INVALID_PARAMETER;
                    }
                    else
                    {
                        num28 = num28 >> 2;
                        num28 &= 0xfff;
                        num30 = num28;
                        ioStatus = IOSTATUS_INVALID_PARAMETER;
                    }
                    this.sendDeviceIDResponse(deviceId, completionId, ioStatus, 0, new byte[0], 0);
                    return;
                }
                case 0x11:
                    this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[0], 0);
                    return;

                default:
                    return;
            }
            this.openFile(deviceId, num9, completionId, num, num2, num3, num4, num5, str);
            return;

        Label_0723:
            buffer = new byte[packet.Length];
            packet.Position = 0L;
            packet.Read(buffer, 0, buffer.Length);
            this.sendDeviceIDResponse(deviceId, completionId, 0, buffer.Length, buffer, buffer.Length);
        }

        private string getFullPath(string path)
        {
            if (m_RootFolder.EndsWith(@"\") && path.StartsWith(@"\"))
            {
                return (m_RootFolder.TrimEnd(new char[] { '\\' }) + path);
            }

            return (m_RootFolder + path);
        }

        private static bool isDirectory(FileAttributes fileAttribs)
        {
            return ((fileAttribs & FileAttributes.Directory) != 0);
        }

        private void openFile(int deviceId, int fileId, int completionId, uint desiredAccess, uint fileAttributes, uint sharedAccess, uint createDisposition, uint createOptions, string path)
        {
            uint num = 0x80000000;
            uint num2 = 0x40000000;
            uint num3 = 0x10000000;

            if (path.EndsWith(@"\desktop.ini") || path.EndsWith(":$DATA"))
            {
                byte[] information = new byte[1];
                this.sendDeviceIDResponse(deviceId, completionId, IOSTATUS_NOSUCHFILE, 0, information, 1);
                return;
            }

            uint ioStatus = IOSTATUS_SUCCESS;
            string str = this.getFullPath(path);
            bool flag = false;

            try
            {
                flag = m_IsolatedStorage.DirectoryExists(str);
            }
            catch { }

            FileStream stream = null;
            bool flag2 = false;

            if (flag || ((createOptions & 1) != 0))
            {
                flag2 = true;

                try
                {
                    switch (((CreateDisposition) createDisposition))
                    {
                        case CreateDisposition.FILE_CREATE:
                        case CreateDisposition.FILE_OPEN_IF:
                        case CreateDisposition.FILE_OVERWRITE_IF:
                            m_IsolatedStorage.CreateDirectory(str);
                            goto Label_01A1;

                        case CreateDisposition.FILE_OVERWRITE:
                            goto Label_01A1;
                    }
                }
                catch
                {
                    ioStatus = IOSTATUS_NOSUCHFILE;
                }
            }
            else
            {
                FileAccess readWrite;

                if (((desiredAccess & num3) != 0) || (((desiredAccess & num) != 0) && ((desiredAccess & num2) != 0)))
                {
                    readWrite = FileAccess.ReadWrite;
                }
                else if (((desiredAccess & num2) != 0) && ((desiredAccess & num) == 0))
                {
                    readWrite = FileAccess.Write;
                }
                else
                {
                    readWrite = FileAccess.Read;
                }

                FileShare none = FileShare.None;

                if ((sharedAccess & 1) != 0)
                {
                    none |= FileShare.Read;
                }

                if ((sharedAccess & 2) != 0)
                {
                    none |= FileShare.Write;
                }

                if ((sharedAccess & 4) != 0)
                {
                    none |= FileShare.Delete;
                }

                try
                {
                    switch (((CreateDisposition) createDisposition))
                    {
                        case CreateDisposition.FILE_OPEN:
                            stream = m_IsolatedStorage.OpenFile(str, FileMode.Open, readWrite);
                            goto Label_01A1;

                        case CreateDisposition.FILE_CREATE:
                            stream = m_IsolatedStorage.OpenFile(str, FileMode.CreateNew, readWrite);
                            goto Label_01A1;

                        case CreateDisposition.FILE_OPEN_IF:
                            stream = m_IsolatedStorage.OpenFile(str, FileMode.OpenOrCreate, readWrite);
                            goto Label_01A1;

                        case CreateDisposition.FILE_OVERWRITE:
                            stream = m_IsolatedStorage.OpenFile(str, FileMode.Truncate, readWrite);
                            goto Label_01A1;

                        case CreateDisposition.FILE_OVERWRITE_IF:
                            stream = m_IsolatedStorage.OpenFile(str, FileMode.Create, readWrite);
                            goto Label_01A1;
                    }
                }
                catch (Exception)
                {
                    ioStatus = IOSTATUS_NOSUCHFILE;
                }
            }

        Label_01A1:
            if (ioStatus != IOSTATUS_SUCCESS)
            {
                byte[] buffer2 = new byte[1];
                this.sendDeviceIDResponse(deviceId, completionId, ioStatus, 0, buffer2, 1);
            }
            else
            {
                int num5 = FileIOManager.getFreeSlot();

                FileIOHandle file = new FileIOHandle {
                    desiredAccess = desiredAccess,
                    createOptions = createOptions,
                    tempFile = false,
                    deviceId = (uint) deviceId,
                    path = path,
                    fullpath = str,
                    directory = flag2,
                    stream = stream
                };

                FileIOManager.putFile(num5, file);
                byte[] buffer3 = new byte[1];

                switch (((CreateDisposition) createDisposition))
                {
                    case CreateDisposition.FILE_OPEN:
                    case CreateDisposition.FILE_CREATE:
                    case CreateDisposition.FILE_OVERWRITE:
                        buffer3[0] = 0;
                        break;

                    case CreateDisposition.FILE_OPEN_IF:
                        buffer3[0] = 1;
                        break;

                    case CreateDisposition.FILE_OVERWRITE_IF:
                        buffer3[0] = 2;
                        break;
                }

                this.sendDeviceIDResponse(deviceId, completionId, ioStatus, num5, buffer3, buffer3.Length);
            }
        }
        
        private void sendClientAnnounce(int clientId, int minorVersion)
        {
            byte[] bytes = ASCIIEncoding.GetBytes("rDCC");
            RdpPacket data = new RdpPacket();
            data.Write(bytes, 0, 4);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)minorVersion);
            data.WriteBigEndian32(clientId);

            this.send(data);
        }

        // Вызов Capability обновляет файловый канал
        internal void sendClientCapability()
        {
            byte[] bytes = ASCIIEncoding.GetBytes("rDPC");
            RdpPacket data = new RdpPacket();
            data.Write(bytes, 0, 4);
            data.WriteLittleEndian16((short)5);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)40);
            data.WriteLittleEndian32(1);
            data.WriteLittleEndian32(2);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian16((short)1);
            data.WriteLittleEndian16((short)5);
            data.WriteLittleEndian16((short)0x3fff);
            data.WriteLittleEndian16((short)0);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian32(1);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian16((short)2);
            data.WriteLittleEndian16((short)8);
            data.WriteLittleEndian32(1);
            data.WriteLittleEndian16((short)3);
            data.WriteLittleEndian16((short)8);
            data.WriteLittleEndian32(1);
            data.WriteLittleEndian16((short)4);
            data.WriteLittleEndian16((short)8);
            data.WriteLittleEndian32(2);
            data.WriteLittleEndian16((short)5);
            data.WriteLittleEndian16((short)8);
            data.WriteLittleEndian32(1);

            this.send(data);
        }

        private void sendClientName()
        {
            byte[] bytes = ASCIIEncoding.GetBytes("rDNC");
            RdpPacket data = new RdpPacket();
            int num = (this.m_sDeviceName.Length + 1) * 2;
            data.Write(bytes, 0, 4);
            data.WriteLittleEndian32(1);
            data.WriteLittleEndian32(0);
            data.WriteLittleEndian32(num);
            data.WriteUnicodeString(this.m_sDeviceName);

            this.send(data);
        }

        private void sendDeviceIDResponse(int deviceId, int completionId, uint ioStatus, int fileHandle, byte[] information, int information_len)
        {
            byte[] bytes = ASCIIEncoding.GetBytes("rDCI");
            RdpPacket data = new RdpPacket();
            data.Write(bytes, 0, 4);
            data.WriteLittleEndian32(deviceId);
            data.WriteLittleEndian32(completionId);
            data.WriteLittleEndianU32(ioStatus);
            data.WriteLittleEndian32(fileHandle);

            if (information == null)
            {
                while (information_len-- > 0)
                {
                    data.WriteByte(0);
                }
            }
            else
            {
                data.Write(information, 0, information_len);
            }

            this.send(data);
        }

        private void sendDeviceListAnnounce()
        {
            int num = 1;
            byte[] bytes = ASCIIEncoding.GetBytes("rDAD");
            RdpPacket data = new RdpPacket();
            data.Write(bytes, 0, 4);
            data.WriteLittleEndian32(num);

            for (int i = 0; i < num; i++)
            {
                data.WriteLittleEndian32(8);
                data.WriteLittleEndian32((int) (i + 1));
                data.WriteString("Storage\0", false);
                data.WriteLittleEndian32((int) (this.m_sDriveName.Length + 1));
                data.WriteString(this.m_sDriveName, false);
                data.WriteByte(0);
            }

            this.send(data);
        }

        private void setAttributes(int deviceId, int fileId, int completionId, string path, int infoClass, RdpPacket data)
        {
            FileInfoClass class2 = (FileInfoClass) infoClass;

            if (class2 <= FileInfoClass.FileRenameInformation)
            {
                switch (class2)
                {
                    case FileInfoClass.FileBasicInformation:
                    {
                        data.Position += 4L;
                        data.Position += 0x18L;
                        ulong num3 = (ulong) data.ReadLittleEndian32();
                        ulong num4 = (ulong) data.ReadLittleEndian32();
                        ulong num5 = (num4 << 0x20) + num3;
                        num5 /= (ulong) 0x989680L;
                        num5 -= (ulong) 0x2b6109100L;
                        num3 = (ulong) data.ReadLittleEndian32();
                        num4 = (ulong) data.ReadLittleEndian32();
                        num5 = (num4 << 0x20) + num3;
                        num3 = (ulong) data.ReadLittleEndian32();
                        num4 = (ulong) data.ReadLittleEndian32();
                        num3 = (ulong) data.ReadLittleEndian32();
                        num4 = (ulong) data.ReadLittleEndian32();
                        data.ReadLittleEndian32();
                        data.ReadLittleEndian32();

                        this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[0], 0);
                        return;
                    }

                    case FileInfoClass.FileRenameInformation:
                    {
                        data.Position += 4L;
                        data.Position += 0x1aL;
                        int num = data.ReadLittleEndian32();
                        byte[] buffer = new byte[num - 2];
                        data.Read(buffer, 0, num - 2);
                        string str = Encoding.Unicode.GetString(buffer, 0, num - 2);
                        string sourceFileName = this.getFullPath(path);
                        str = this.getFullPath(str);
                        m_IsolatedStorage.MoveFile(sourceFileName, str);
                        this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[0], 0);
                        break;
                    }
                }
            }
            else
            {
                FileInfoClass class3 = class2;

                if (class3 != FileInfoClass.FileDispositionInformation)
                {
                    if (class3 != FileInfoClass.FileEndOfFileInformation)
                    {
                        return;
                    }
                }
                else
                {
                    int num2 = 1;
                    FileIOHandle handle = FileIOManager.getFile(fileId);

                    if ((num2 != 0) || ((handle.desiredAccess & 0x1100) != 0))
                    {
                        handle.tempFile = true;
                    }

                    this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[0], 0);
                    return;
                }

                this.sendDeviceIDResponse(deviceId, completionId, 0, 0, new byte[0], 0);
            }
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
                packet.WriteLittleEndian32((int)data.Length);
                packet.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_FIRST | CHANNEL_FLAG.CHANNEL_FLAG_LAST));
                packet.copyToByteArray(data);

                IsoLayer.SendToCannel(packet, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0, this.ChannelID);
            }
            else
            {
                RdpPacket packet2 = new RdpPacket();
                packet2.WriteLittleEndian32((int)data.Length);
                packet2.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_FIRST));
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
                packet3.WriteLittleEndian32((int)data.Length);

                if (num == 0)
                {
                    packet3.WriteLittleEndian32((int)(CHANNEL_FLAG.CHANNEL_FLAG_LAST));
                }
                else
                {
                    packet3.WriteLittleEndian32(0x00);
                }

                byte[] buffer2 = new byte[count];
                data.Read(buffer2, 0, count);
                packet3.Write(buffer2, 0, count);

                IsoLayer.SendToCannel(packet3, Secure.RDPEncrypted() ? (int)(MCS.TS_SECURITY_HEADER.SEC_ENCRYPT) : 0, this.ChannelID);
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
        internal enum CreateDisposition
        {
            FILE_CREATE = 2,
            FILE_OPEN = 1,
            FILE_OPEN_IF = 3,
            FILE_OVERWRITE = 4,
            FILE_OVERWRITE_IF = 5
        }

        private class FileSystemException : Exception
        {
            public uint Status;

            public FileSystemException(uint Status)
            {
                this.Status = Status;
            }
        }

    }
}