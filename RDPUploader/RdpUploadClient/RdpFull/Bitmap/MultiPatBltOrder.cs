using System;

namespace RdpUploadClient
{
    internal class MultiPatBltOrder
    {
        internal static int BackgroundColor = 0;
        internal static BrushOrder Brush = new BrushOrder();
        internal static int CX = 0;
        internal static int CY = 0;
        internal static int DeltaEntries;
        internal static Rectangle[] DeltaList;
        internal static int ForegroundColor = 0;
        internal static int Opcode = 0;
        internal static int X = 0;
        internal static int Y = 0;

        internal static void drawMultiPatBltOrder()
        {
            int fgcolor = RdpBitmap.convertColor(ForegroundColor);
            int bgcolor = RdpBitmap.convertColor(BackgroundColor);
            foreach (Rectangle rectangle in DeltaList)
            {
                PatBltOrder.drawPatBltOrder(Opcode, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, fgcolor, bgcolor, Brush);
            }
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            CX = 0;
            CY = 0;
            Opcode = 0;
            ForegroundColor = 0;
            BackgroundColor = 0;
            Brush.Reset();
            DeltaEntries = 0;
            DeltaList = null;
        }

    }
}