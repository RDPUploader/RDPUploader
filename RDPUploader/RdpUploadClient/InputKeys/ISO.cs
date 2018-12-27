using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RdpUploadClient.InputKeys
{
    internal class ScanCode
    {
        // SendKey - клавиша нажата и отпущена
        internal static void SendKey(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)Rdp.KeyboardFlags.KBDFLAGS_DOWN, key, 0);
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)Rdp.KeyboardFlags.KBDFLAGS_RELEASE, key, 0);
            Thread.Sleep(11);
        }

        // SendKey - клавиша нажата
        internal static void SendKeyOn(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)Rdp.KeyboardFlags.KBDFLAGS_DOWN, key, 0);
        }

        // SendKey - клавиша отпущена
        internal static void SendKeyOff(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)Rdp.KeyboardFlags.KBDFLAGS_RELEASE, key, 0);
        }

        // SendExtKey - клавиша нажата и отпущена
        internal static void SendKeyEx(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)(Rdp.KeyboardFlags.KBDFLAGS_EXTENDED | Rdp.KeyboardFlags.KBDFLAGS_DOWN), key, 0);
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)(Rdp.KeyboardFlags.KBDFLAGS_EXTENDED | Rdp.KeyboardFlags.KBDFLAGS_RELEASE), key, 0);
        }

        // SendExtKey - клавиша нажата
        internal static void SendKeyExOn(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)(Rdp.KeyboardFlags.KBDFLAGS_EXTENDED | Rdp.KeyboardFlags.KBDFLAGS_DOWN), key, 0);
        }

        // SendExtKey - клавиша отпущена
        internal static void SendKeyExOff(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_SCANCODE, (int)(Rdp.KeyboardFlags.KBDFLAGS_EXTENDED | Rdp.KeyboardFlags.KBDFLAGS_RELEASE), key, 0);
        }

        // Отправка едениченого символа
        internal static void SendChar(char ch, bool isUpper)
        {
            if (!SpecialChars(ch))
            {
                if (isUpper)
                {
                    SendKey(0x3A);
                    SendKey(KeyConverter.GetScanCode(KeyConverter.GetVKeyCode(ch)));
                    SendKey(0x3A);
                }
                else
                {
                    SendKey(KeyConverter.GetScanCode(KeyConverter.GetVKeyCode(ch)));
                }
            }
        }

        // Расширенная отправка едениченого символа
        internal static void SendCharEx(char ch)
        {
            if (!SpecialChars(ch))
                SendKeyEx(KeyConverter.GetScanCode(KeyConverter.GetVKeyCode(ch)));
        }

        // Отправка строки
        internal static void SendString(string sourceStr)
        {
            foreach (var ch in sourceStr)
            {
                if (!SpecialChars(ch))
                    SendChar(char.ToLower(ch), char.IsUpper(ch));
            }
        }

        // Спец символы
        internal static bool SpecialChars(char ch)
        {
            if (ch == '@')
            {
                SendKeyOn(0x2A);
                SendKey(0x03);
                SendKeyOff(0x2A);
                return true;
            }
            else if (ch == ':')
            {
                SendKeyOn(0x2A);
                SendKey(0x27);
                SendKeyOff(0x2A);
                return true;
            }

            return false;
        }
    }

    internal class Unicode
    {
        // SendKey - клавиша нажата и отпущена
        internal static void SendKey(uint key)
        {
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_UNICODE, 0, key, 0);
            Rdp.SendInput(Main.InputTime++, Rdp.InputType.INPUT_EVENT_UNICODE, (int)Rdp.KeyboardFlags.KBDFLAGS_RELEASE, key, 0);
        }

        // Отправка строки
        internal static void SendString(string sourceStr)
        {
            foreach (var ch in sourceStr)
            {
                SendKey((uint)ch);
            }
        }
    }

}