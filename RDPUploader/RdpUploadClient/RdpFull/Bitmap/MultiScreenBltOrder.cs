using System;

namespace RdpUploadClient
{
    internal class MultiScreenBltOrder
    {
        public static int CX;
        public static int CY;
        internal static int DeltaEntries;
        internal static Rectangle[] DeltaList;
        public static int Opcode;
        public static int SrcX;
        public static int SrcY;
        public static int X;
        public static int Y;

        internal static void drawMultiScreenBltOrder()
        {
            foreach (Rectangle rectangle in DeltaList)
            {
                ScreenBltOrder.drawScreenBltOrder(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, SrcX, SrcY, Opcode);
            }
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            CX = 0;
            CY = 0;
            SrcX = 0;
            SrcY = 0;
            Opcode = 0;
            DeltaEntries = 0;
            DeltaList = null;
        }

    }
}