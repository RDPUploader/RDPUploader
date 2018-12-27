using System;

namespace RdpUploadClient
{
    internal class MemBltOrder
    {
        internal static int CacheID;
        internal static int CacheIDX;
        internal static int ColorTable;
        internal static int CX;
        internal static int CY;
        internal static int Opcode;
        internal static int SrcX;
        internal static int SrcY;
        internal static int X;
        internal static int Y;

        internal static void drawMemBltOrder()
        {
            int x = X;
            int y = Y;
            int cX = CX;
            int cY = CY;
            int srcX = SrcX;
            int srcY = SrcY;
            RdpBitmap bitmap = Cache.getBitmap(CacheID, CacheIDX);
            if (bitmap != null)
            {
                int boundsRight = (x + cX) - 1;
                if (boundsRight > Options.BoundsRight)
                {
                    boundsRight = Options.BoundsRight;
                }
                if (x < Options.BoundsLeft)
                {
                    x = Options.BoundsLeft;
                }
                cX = (boundsRight - x) + 1;
                int boundsBottom = (y + cY) - 1;
                if (boundsBottom > Options.BoundsBottom)
                {
                    boundsBottom = Options.BoundsBottom;
                }
                if (y < Options.BoundsTop)
                {
                    y = Options.BoundsTop;
                }
                cY = (boundsBottom - y) + 1;
                srcX += x - X;
                srcY += y - Y;
                Options.Enter();
                try
                {
                    ChangedRect.Invalidate(x, y, cX, cY);
                    RasterOp.do_array(Opcode, Options.Canvas, Options.Canvas.Width, x, y, cX, cY, bitmap.getData(ColorTable), bitmap.getWidth(), srcX, srcY);
                }
                finally
                {
                    Options.Exit();
                }
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
            CacheID = 0;
            CacheIDX = 0;
            ColorTable = 0;
        }

    }
}