namespace RDP_Uploader
{
    class UploadObject
    {
        private string ip;

        private int port;

        private string login;

        private string password;

        private bool nla;

        private string url;

        private string ftpHost;

        private string ftpPort;

        private string ftpLogin;

        private string ftpPassword;

        private string ftpFilePath;

        private bool useDRIVE;

        private bool useCLIPBOARD;

        private bool useHTTP_BA;

        private bool useHTTP_PS;

        private bool useFTP;

        private int timeout;

        private int loadTimeout;

        private int connectionTImeout;

        private string fileParams;

        private string powerShellScript;

        private bool isDebug;

        public string Ip
        {
            get
            {
                return ip;
            }

            set
            {
                ip = value;
            }
        }

        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                port = value;
            }
        }

        public string Login
        {
            get
            {
                return login;
            }

            set
            {
                login = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
            }
        }

        public bool Nla
        {
            get
            {
                return nla;
            }

            set
            {
                nla = value;
            }
        }

        public string Url
        {
            get
            {
                return url;
            }

            set
            {
                url = value;
            }
        }

        public string FtpHost
        {
            get
            {
                return ftpHost;
            }

            set
            {
                ftpHost = value;
            }
        }

        public string FtpPort
        {
            get
            {
                return ftpPort;
            }

            set
            {
                ftpPort = value;
            }
        }

        public string FtpLogin
        {
            get
            {
                return ftpLogin;
            }

            set
            {
                ftpLogin = value;
            }
        }

        public string FtpPassword
        {
            get
            {
                return ftpPassword;
            }

            set
            {
                ftpPassword = value;
            }
        }

        public string FtpFilePath
        {
            get
            {
                return ftpFilePath;
            }

            set
            {
                ftpFilePath = value;
            }
        }

        public bool UseDRIVE
        {
            get
            {
                return useDRIVE;
            }

            set
            {
                useDRIVE = value;
            }
        }

        public bool UseCLIPBOARD
        {
            get
            {
                return useCLIPBOARD;
            }

            set
            {
                useCLIPBOARD = value;
            }
        }

        public bool UseHTTP_BA
        {
            get
            {
                return useHTTP_BA;
            }

            set
            {
                useHTTP_BA = value;
            }
        }

        public bool UseHTTP_PS
        {
            get
            {
                return useHTTP_PS;
            }

            set
            {
                useHTTP_PS = value;
            }
        }

        public bool UseFTP
        {
            get
            {
                return useFTP;
            }

            set
            {
                useFTP = value;
            }
        }

        public int Timeout
        {
            get
            {
                return timeout;
            }

            set
            {
                timeout = value;
            }
        }

        public int LoadTimeout
        {
            get
            {
                return loadTimeout;
            }

            set
            {
                loadTimeout = value;
            }
        }

        public int ConnectionTImeout
        {
            get
            {
                return connectionTImeout;
            }

            set
            {
                connectionTImeout = value;
            }
        }

        public string FileParams
        {
            get
            {
                return fileParams;
            }

            set
            {
                fileParams = value;
            }
        }

        public string PowerShellScript
        {
            get
            {
                return powerShellScript;
            }

            set
            {
                powerShellScript = value;
            }
        }

        public bool IsDebug
        {
            get
            {
                return isDebug;
            }

            set
            {
                isDebug = value;
            }
        }

        public UploadObject(string ip, int port, string login, string password, bool nla, string url, string ftpHost, string ftpPort, string ftpLogin, string ftpPassword, string ftpFilePath, bool useDRIVE, bool useCLIPBOARD, bool useHTTP_BA, bool useHTTP_PS, bool useFTP, int timeout, int loadTimeout, int connectionTImeout, string fileParams, string powerShellScript, bool isDebug)
        {
            this.Ip = ip;
            this.Port = port;
            this.Login = login;
            this.Password = password;
            this.Nla = nla;
            this.Url = url;
            this.FtpHost = ftpHost;
            this.FtpPort = ftpPort;
            this.FtpLogin = ftpLogin;
            this.FtpPassword = ftpPassword;
            this.FtpFilePath = ftpFilePath;
            this.UseDRIVE = useDRIVE;
            this.UseCLIPBOARD = useCLIPBOARD;
            this.UseHTTP_BA = useHTTP_BA;
            this.UseHTTP_PS = useHTTP_PS;
            this.UseFTP = useFTP;
            this.Timeout = timeout;
            this.LoadTimeout = loadTimeout;
            this.ConnectionTImeout = connectionTImeout;
            this.FileParams = fileParams;
            this.PowerShellScript = powerShellScript;
            this.IsDebug = isDebug;
        }
    }
}
