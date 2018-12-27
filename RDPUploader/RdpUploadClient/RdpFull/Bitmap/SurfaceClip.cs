using System;

namespace RdpUploadClient
{
    internal class SurfaceClip
    {
        internal static int Bottom;
        internal static int Left;
        internal static int Right;
        internal static int Top;

        internal static void Reset()
        {
            Left = 0;
            Top = 0;
            Right = 0;
            Bottom = 0;
        }

    }
}