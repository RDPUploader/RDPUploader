using System;

namespace RdpUploadClient
{
    internal class MultiDestBltOrder
    {
        internal static int CX;
        internal static int CY;
        internal static int DeltaEntries;
        internal static Rectangle[] DeltaList;
        internal static int Opcode;
        internal static int X;
        internal static int Y;

        internal static void drawMultiDestBltOrder()
        {
            foreach (Rectangle rectangle in DeltaList)
            {
                DestBltOrder.drawDestBltOrder(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, Opcode);
            }
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            CX = 0;
            CY = 0;
            Opcode = 0;
            DeltaEntries = 0;
            DeltaList = null;
        }

    }
}