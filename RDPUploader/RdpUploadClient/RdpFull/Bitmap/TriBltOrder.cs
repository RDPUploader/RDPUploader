using System;

namespace RdpUploadClient
{
    internal class TriBltOrder
    {
        public static int BackgroundColor;
        public static BrushOrder Brush = new BrushOrder();
        public static int CacheID;
        public static int CacheIDX;
        public static int ColorTable;
        public static int CX;
        public static int CY;
        public static int ForegroundColor;
        public static int Opcode;
        private const int ROP2_AND = 8;
        public const int ROP2_COPY = 12;
        private const int ROP2_NXOR = 9;
        private const int ROP2_OR = 14;
        private const int ROP2_XOR = 6;
        public static int SrcX;
        public static int SrcY;
        public static int Unknown;
        public static int X;
        public static int Y;

        internal static void drawTriBltOrder()
        {
            int x = X;
            int y = Y;
            int cX = CX;
            int cY = CY;
            int srcX = SrcX;
            int srcY = SrcY;
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
            int fgcolor = RdpBitmap.convertColor(ForegroundColor);
            int bgcolor = RdpBitmap.convertColor(BackgroundColor);
            Options.Enter();
            try
            {
                RdpBitmap bitmap = Cache.getBitmap(CacheID, CacheIDX);
                if (bitmap != null)
                {
                    ChangedRect.Invalidate(x, y, cX, cY);
                    switch (Opcode)
                    {
                        case 0x69:
                            RasterOp.do_array(6, Options.Canvas, Options.Canvas.Width, x, y, cX, cY, bitmap.getData(ColorTable), bitmap.getWidth(), srcX, srcY);
                            PatBltOrder.drawPatBltOrder(9, x, y, cX, cY, fgcolor, bgcolor, Brush);
                            return;

                        case 0xb8:
                            PatBltOrder.drawPatBltOrder(6, x, y, cX, cY, fgcolor, bgcolor, Brush);
                            RasterOp.do_array(8, Options.Canvas, Options.Canvas.Width, x, y, cX, cY, bitmap.getData(ColorTable), bitmap.getWidth(), srcX, srcY);
                            PatBltOrder.drawPatBltOrder(6, x, y, cX, cY, fgcolor, bgcolor, Brush);
                            return;

                        case 0xc0:
                            RasterOp.do_array(12, Options.Canvas, Options.Canvas.Width, x, y, cX, cY, bitmap.getData(ColorTable), bitmap.getWidth(), srcX, srcY);
                            PatBltOrder.drawPatBltOrder(8, x, y, cX, cY, fgcolor, bgcolor, Brush);
                            return;
                    }
                    // Unimplemented Triblt opcode: Opcode
                    RasterOp.do_array(12, Options.Canvas, Options.Canvas.Width, x, y, cX, cY, bitmap.getData(ColorTable), bitmap.getWidth(), srcX, srcY);
                }
            }
            finally
            {
                Options.Exit();
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
            BackgroundColor = 0;
            ForegroundColor = 0;
            Brush.Reset();
            CacheID = 0;
            CacheIDX = 0;
            ColorTable = 0;
        }

    }
}