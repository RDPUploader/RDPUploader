using System;
using System.Text;

namespace RdpUploadClient
{
    internal class SavedHost
    {
        private bool m_bDisableNLA;
        private int m_Bpp;
        private bool m_bSavePassword;
        private string m_CacheFilename;
        private HostFlags m_Flags;
        private string m_FSDeviceName;
        private string m_FSDriveName;
        private string m_GUID;
        private string m_HostName;
        private string m_Name;
        private int m_nConnections;
        private string m_Password;
        private int m_Port;
        private string m_Username;
        private int m_ConnectTimeout;

        public SavedHost()
        {
            this.Reset();
        }

        public SavedHost(string sName)
        {
            this.Reset();
            this.m_Name = sName;
        }

        private void Reset()
        {
            this.m_Name = "";
            this.m_HostName = "";
            this.m_Username = this.m_Password = "";
            this.m_Bpp = 0x10;
            this.m_Port = 0xd3d;
            this.m_CacheFilename = "";
            this.m_Flags = HostFlags.DefaultFlags;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        public int BitsPerPixel
        {
            get
            {
                return this.m_Bpp;
            }
            set
            {
                this.m_Bpp = value;
            }
        }

        public string CacheFilename
        {
            get
            {
                return this.m_CacheFilename;
            }
            set
            {
                this.m_CacheFilename = value;
            }
        }

        public int Connections
        {
            get
            {
                return this.m_nConnections;
            }
            set
            {
                this.m_nConnections = value;
            }
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_HostName))
                {
                    return this.m_Name;
                }
                return (this.m_Name + " (" + this.m_HostName + ")");
            }
        }

        public bool EnableBmpCache
        {
            get
            {
                return ((this.m_Flags & HostFlags.PeristentBmpCache) != ((HostFlags) 0));
            }
            set
            {
                if (value)
                {
                    this.m_Flags |= HostFlags.PeristentBmpCache;
                }
                else
                {
                    this.m_Flags &= ~HostFlags.PeristentBmpCache;
                }
            }
        }

        public bool EnableNLA
        {
            get
            {
                return !this.m_bDisableNLA;
            }
            set
            {
                this.m_bDisableNLA = !value;
            }
        }

        public HostFlags Flags
        {
            get
            {
                return this.m_Flags;
            }
            set
            {
                this.m_Flags = value;
            }
        }

        public int FlagsValue
        {
            get
            {
                return (int) this.m_Flags;
            }
            set
            {
                this.m_Flags = (HostFlags) value;
            }
        }

        public string FSDeviceName
        {
            get
            {
                return this.m_FSDeviceName;
            }
            set
            {
                this.m_FSDeviceName = value;
            }
        }

        public string FSDriveName
        {
            get
            {
                return this.m_FSDriveName;
            }
            set
            {
                this.m_FSDriveName = value;
            }
        }

        public string GUID
        {
            get
            {
                if (this.m_GUID == null)
                {
                    this.m_GUID = Guid.NewGuid().ToString();
                }
                return this.m_GUID;
            }
            set
            {
                this.m_GUID = value;
            }
        }

        public string HostInfo
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("Host: ");
                builder.Append(this.m_HostName);
                return builder.ToString();
            }
        }

        public string HostName
        {
            get
            {
                return this.m_HostName;
            }
            set
            {
                this.m_HostName = value;
            }
        }

        public string Password
        {
            get
            {
                return this.m_Password;
            }
            set
            {
                this.m_Password = value;
            }
        }

        public int Port
        {
            get
            {
                return this.m_Port;
            }
            set
            {
                this.m_Port = value;
            }
        }

        public bool SavePassword
        {
            get
            {
                return this.m_bSavePassword;
            }
            set
            {
                this.m_bSavePassword = value;
            }
        }

        public string Username
        {
            get
            {
                return this.m_Username;
            }
            set
            {
                this.m_Username = value;
            }
        }

        public int ConnectTimeout
        {
            get
            {
                return this.m_ConnectTimeout;
            }
            set
            {
                this.m_ConnectTimeout = value;
            }
        }

    }
}