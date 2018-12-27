using System;

namespace RdpUploadClient
{
    internal class DestBltOrder
    {
        internal static int CX;
        internal static int CY;
        internal static int Opcode;
        internal static int X;
        internal static int Y;

        internal static void drawDestBltOrder()
        {
            drawDestBltOrder(X, Y, CX, CY, Opcode);
        }

        internal static void drawDestBltOrder(int x, int y, int cx, int cy, int Opcode)
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
            ChangedRect.Invalidate(x, x, cx, cy);
            RasterOp.do_array(Opcode, Options.Canvas, Options.Canvas.Width, x, y, cx, cy, null, 0, 0, 0);
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            CX = 0;
            CY = 0;
            Opcode = 0;
        }

    }
}