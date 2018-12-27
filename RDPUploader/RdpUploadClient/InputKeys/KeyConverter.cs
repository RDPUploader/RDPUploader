using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RdpUploadClient.InputKeys
{
    internal static class KeyConverter
    {
        // WinAPI
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short VkKeyScan(char ch);

        // Перевод Virtual Key Code в Scan Code
        internal static uint GetScanCode(int keyCode)
        {
            return (uint)MapVirtualKey(keyCode, 0);
        }

        // Перевод символа в Virtual Key Code
        internal static int GetVKeyCode(char ch)
        {
            // Задаем английскую раскладку клавиатуры
            InputLanguage.CurrentInputLanguage = InputLanguage.FromCulture(new System.Globalization.CultureInfo("en-US"));

            return (int)VkKeyScan(ch);
        }

    }
}