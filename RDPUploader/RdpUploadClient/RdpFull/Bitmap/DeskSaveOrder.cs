using System;

namespace RdpUploadClient
{
    internal class DeskSaveOrder
    {
        internal static int Action;
        internal static int Bottom;
        internal static int Left;
        internal static int Offset;
        internal static int Right;
        internal static int Top;

        internal static void Reset()
        {
            Top = 0;
            Left = 0;
            Right = 0;
            Bottom = 0;
            Offset = 0;
            Action = 0;
        }

    }
}