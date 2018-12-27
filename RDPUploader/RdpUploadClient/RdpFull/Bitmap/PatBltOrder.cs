using System;

namespace RdpUploadClient
{
    internal class PatBltOrder
    {
        internal static int BackgroundColor = 0;
        internal static BrushOrder Brush = new BrushOrder();
        internal static int CX = 0;
        internal static int CY = 0;
        internal static int ForegroundColor = 0;
        internal static int Opcode = 0;
        internal static int X = 0;
        internal static int Y = 0;

        internal static void drawPatBltOrder()
        {
            int x = X;
            int y = Y;
            int cX = CX;
            int cY = CY;
            int fgcolor = RdpBitmap.convertColor(ForegroundColor);
            int bgcolor = RdpBitmap.convertColor(BackgroundColor);
            drawPatBltOrder(Opcode, x, y, cX, cY, fgcolor, bgcolor, Brush);
        }

        internal static void drawPatBltOrder(int opcode, int x, int y, int cx, int cy, int fgcolor, int bgcolor, BrushOrder brush)
        {
            int num3;
            int boundsRight = (x + cx) - 1;
            if (boundsRight > Options.BoundsRight)
            {
                boundsRight = Options.BoundsRight;
            }
            if (x > Options.BoundsRight)
            {
                x = Options.BoundsRight;
            }
            if (x < Options.BoundsLeft)
            {
                x = Options.BoundsLeft;
            }
            cx = (boundsRight - x) + 1;
            if (cx < 0)
            {
                cx = 0;
            }
            int boundsBottom = (y + cy) - 1;
            if (boundsBottom > Options.BoundsBottom)
            {
                boundsBottom = Options.BoundsBottom;
            }
            if (y > Options.BoundsBottom)
            {
                y = Options.BoundsBottom;
            }
            if (y < Options.BoundsTop)
            {
                y = Options.BoundsTop;
            }
            cy = (boundsBottom - y) + 1;
            if (cy < 0)
            {
                cy = 0;
            }
            ChangedRect.Invalidate(x, y, cx, cy);
            uint[] src = null;
            switch (Brush.Style)
            {
                case 0:
                    src = new uint[cx * cy];
                    for (num3 = 0; num3 < src.Length; num3++)
                    {
                        src[num3] = (uint) fgcolor;
                    }
                    RasterOp.do_array(opcode, Options.Canvas, Options.Canvas.Width, x, y, cx, cy, src, cx, 0, 0);
                    break;

                case 1:
                case 2:
                    break;

                case 3:
                {
                    int xOrigin = Brush.XOrigin;
                    int yOrigin = Brush.YOrigin;
                    byte[] pattern = Brush.Pattern;
                    src = new uint[cx * cy];
                    int index = 0;
                    for (num3 = 0; num3 < cy; num3++)
                    {
                        for (int i = 0; i < cx; i++)
                        {
                            if ((pattern[(num3 + yOrigin) % 8] & (((int) 1) << ((i + xOrigin) % 8))) == 0)
                            {
                                src[index] = (uint) fgcolor;
                            }
                            else
                            {
                                src[index] = (uint) bgcolor;
                            }
                            index++;
                        }
                    }
                    RasterOp.do_array(opcode, Options.Canvas, Options.Canvas.Width, x, y, cx, cy, src, cx, 0, 0);
                    return;
                }
                default:
                    return;
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
        }

    }
}