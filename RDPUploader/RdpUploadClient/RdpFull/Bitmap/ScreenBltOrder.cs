using System;

namespace RdpUploadClient
{
    internal class ScreenBltOrder
    {
        public static int CX;
        public static int CY;
        public static int Opcode;
        public static int SrcX;
        public static int SrcY;
        public static int X;
        public static int Y;

        internal static void drawScreenBltOrder()
        {
            drawScreenBltOrder(X, Y, CX, CY, SrcX, SrcY, Opcode);
        }

        internal static void drawScreenBltOrder(int x, int y, int cx, int cy, int SrcX, int SrcY, int Opcode)
        {
            int num = x;
            int num2 = y;
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
            int srcx = (SrcX + x) - num;
            int srcy = (SrcY + y) - num2;
            // ScreenBltOrder
            ChangedRect.Invalidate(x, y, cx, cy);
            RasterOp.do_array(Opcode, Options.Canvas, Options.Canvas.Width, x, y, cx, cy, null, Options.Canvas.Width, srcx, srcy);
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
        }

    }
}