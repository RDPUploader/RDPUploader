using System;

namespace RdpUploadClient
{
    internal class RectangleOrder
    {
        internal static int ColourB;
        internal static int ColourG;
        internal static int ColourR;
        internal static int CX;
        internal static int CY;
        internal static int X;
        internal static int Y;

        internal static void drawRectangleOrder()
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
            fillRectangle(X, Y, CX, CY, num);
        }

        internal static void fillRectangle(int x, int y, int cx, int cy, int color)
        {
            if ((x <= Options.BoundsRight) && (y <= Options.BoundsBottom))
            {
                int boundsRight = (x + cx) - 1;
                if (boundsRight > Options.BoundsRight)
                {
                    boundsRight = Options.BoundsRight;
                }
                if (x < Options.BoundsLeft)
                {
                    x = Options.BoundsLeft;
                }
                cx = (boundsRight - x) + 1;
                int boundsBottom = (y + cy) - 1;
                if (boundsBottom > Options.BoundsBottom)
                {
                    boundsBottom = Options.BoundsBottom;
                }
                if (y < Options.BoundsTop)
                {
                    y = Options.BoundsTop;
                }
                cy = (boundsBottom - y) + 1;
                ChangedRect.Invalidate(x, y, cx, cy);
                Options.Canvas.SetPixels(x, y, cx, cy, color);
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
        }

    }
}