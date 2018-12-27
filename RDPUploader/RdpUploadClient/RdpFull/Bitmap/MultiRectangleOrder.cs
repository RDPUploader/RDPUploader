using System;

namespace RdpUploadClient
{
    internal class MultiRectangleOrder
    {
        internal static int ColourB;
        internal static int ColourG;
        internal static int ColourR;
        internal static int CX;
        internal static int CY;
        internal static int DeltaEntries;
        internal static Rectangle[] DeltaList;
        internal static int X;
        internal static int Y;

        internal static void drawMultiRectangleOrder()
        {
            int num;
            if (Options.server_bpp == 0x10)
            {
                num = RdpBitmap.convertFrom16bit(((ColourB << 0x10) | (ColourG << 8)) | ColourR);
            }
            else
            {
                num = RdpBitmap.convertFrom8bit(ColourR);
            }
            foreach (Rectangle rectangle in DeltaList)
            {
                RectangleOrder.fillRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, num);
            }
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            CX = 0;
            CY = 0;
            ColourB = 0;
            ColourG = 0;
            ColourR = 0;
            DeltaEntries = 0;
            DeltaList = null;
        }

    }
}