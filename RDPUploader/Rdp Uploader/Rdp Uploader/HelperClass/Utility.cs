using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.ComponentModel;

namespace RDP_Uploader
{
    internal static class Utility
    {
        internal class FileLines
        {
            // Посчитывает строки в файле построчно
            internal static uint Get(string pathFile)
            {
                uint @out = 0;

                // Читаем файл
                using (var stream = new FileStream(pathFile, FileMode.Open))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        while (true)
                        {
                            // Читаем и проверяем IP
                            string ip = "";
                            if ((ip = reader.ReadLine()) == null)
                            {
                                break;
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(ip))
                                    @out++;
                            }
                        }
                    }
                }

                return @out;
            }
        }

        internal class TextUtil
        {
            // Определяет тип аккаунта и возвращает валидный
            internal static string AccountNormalizer(string account)
            {
                try
                {
                    string ip = "";
                    string port = "";
                    string login = "";
                    string password = "";

                    if (account.Contains(":") && account.Contains(";") && account.Contains(" | "))
                    {
                        string tempAccount = account.Split(new string[] { " | " }, StringSplitOptions.None)[0];

                        ip = tempAccount.Split(';')[0].Trim().Split(':')[0].Trim();
                        port = tempAccount.Split(';')[0].Trim().Split(':')[1].Trim();
                        login = tempAccount.Split(';')[1].Trim();
                        try
                        {
                            password = Regex.Split(tempAccount, "^[^;]+;[^;]+;(.+)$")[1].Trim();
                        }
                        catch { }
                    }
                    else if (account.Contains(":") && account.Contains(";"))
                    {
                        ip = account.Split(';')[0].Trim().Split(':')[0].Trim();
                        port = account.Split(';')[0].Trim().Split(':')[1].Trim();
                        login = account.Split(';')[1].Trim();
                        try
                        {
                            password = Regex.Split(account, "^[^;]+;[^;]+;(.+)$")[1].Trim();
                        }
                        catch { }
                    }

                    if (!string.IsNullOrWhiteSpace(ip) && !string.IsNullOrWhiteSpace(login))
                    {
                        return ip + ":" + port + ";" + login + ";" + password;
                    }
                    else
                    {
                        return "";
                    }
                }
                catch { return ""; }
            }

            internal static string Reverse(string inputStr)
            {
                return new string(inputStr.ToCharArray().Reverse().ToArray());
            }

            internal static int PosCount(string sourceStr, string substr)
            {
                return sourceStr.Split(new string[] { substr }, StringSplitOptions.None).Length - 1;
            }
        }

    }
}