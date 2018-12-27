using System;

namespace RdpUploadClient
{
    internal class LineOrder
    {
        internal static int BackgroundColor;
        internal static int EndX;
        internal static int EndY;
        internal static int Mixmode;
        internal static int Opcode;
        internal static int PenColor;
        internal static int PenStyle;
        internal static int PenWidth;
        internal static int StartX;
        internal static int StartY;

        internal static void drawLine(int x1, int y1, int x2, int y2, int color, int opcode)
        {
            if ((x1 == x2) || (y1 == y2))
            {
                drawLineVerticalHorizontal(x1, y1, x2, y2, color, opcode);
            }
            else
            {
                int num5;
                int num6;
                int num7;
                int num8;
                int num9;
                int num10;
                int num11;
                int num12;
                int cx = Math.Abs((int) (x2 - x1));
                int cy = Math.Abs((int) (y2 - y1));
                int x = x1;
                int y = y1;
                if (x2 >= x1)
                {
                    num5 = 1;
                    num6 = 1;
                }
                else
                {
                    num5 = -1;
                    num6 = -1;
                }
                if (y2 >= y1)
                {
                    num7 = 1;
                    num8 = 1;
                }
                else
                {
                    num7 = -1;
                    num8 = -1;
                }
                if (cx >= cy)
                {
                    num5 = 0;
                    num8 = 0;
                    num10 = cx;
                    num9 = cx / 2;
                    num11 = cy;
                    num12 = cx;
                }
                else
                {
                    num6 = 0;
                    num7 = 0;
                    num10 = cy;
                    num9 = cy / 2;
                    num11 = cx;
                    num12 = cy;
                }
                ChangedRect.Invalidate(x, y, cx, cy);
                for (int i = 0; i <= num12; i++)
                {
                    setPixel(opcode, x, y, color);
                    num9 += num11;
                    if (num9 >= num10)
                    {
                        num9 -= num10;
                        x += num5;
                        y += num7;
                    }
                    x += num6;
                    y += num8;
                }
            }
        }

        internal static void drawLineOrder()
        {
            Options.Enter();
            try
            {
                drawLine(StartX, StartY, EndX, EndY, PenColor, Opcode);
            }
            finally
            {
                Options.Exit();
            }
        }

        internal static void drawLineVerticalHorizontal(int x1, int y1, int x2, int y2, int color, int opcode)
        {
            int num;
            int num2;
            if (y1 == y2)
            {
                if ((y1 >= Options.BoundsTop) && (y1 <= Options.BoundsBottom))
                {
                    if (x2 > x1)
                    {
                        if (x1 < Options.BoundsLeft)
                        {
                            x1 = Options.BoundsLeft;
                        }
                        if (x2 > Options.BoundsRight)
                        {
                            x2 = Options.BoundsRight;
                        }
                        num = (y1 * Options.Canvas.Width) + x1;
                        ChangedRect.Invalidate(x1, y1, x2 - x1, 1);
                        for (num2 = 0; num2 < (x2 - x1); num2++)
                        {
                            RasterOp.do_pixel(opcode, Options.Canvas, x1 + num2, y1, color);
                            num++;
                        }
                    }
                    else
                    {
                        if (x2 < Options.BoundsLeft)
                        {
                            x2 = Options.BoundsLeft;
                        }
                        if (x1 > Options.BoundsRight)
                        {
                            x1 = Options.BoundsRight;
                        }
                        num = (y1 * Options.Canvas.Width) + x1;
                        ChangedRect.Invalidate(x2, y1, x1 - x2, 1);
                        for (num2 = 0; num2 < (x1 - x2); num2++)
                        {
                            RasterOp.do_pixel(opcode, Options.Canvas, x2 + num2, y1, color);
                            num--;
                        }
                    }
                }
            }
            else if ((x1 >= Options.BoundsLeft) && (x1 <= Options.BoundsRight))
            {
                if (y2 > y1)
                {
                    if (y1 < Options.BoundsTop)
                    {
                        y1 = Options.BoundsTop;
                    }
                    if (y2 > Options.BoundsBottom)
                    {
                        y2 = Options.BoundsBottom;
                    }
                    num = (y1 * Options.Canvas.Width) + x1;
                    ChangedRect.Invalidate(x1, y1, 1, y2 - y1);
                    for (num2 = 0; num2 < (y2 - y1); num2++)
                    {
                        RasterOp.do_pixel(opcode, Options.Canvas, x1, y1 + num2, color);
                        num += Options.Canvas.Width;
                    }
                }
                else
                {
                    if (y2 < Options.BoundsTop)
                    {
                        y2 = Options.BoundsTop;
                    }
                    if (y1 > Options.BoundsBottom)
                    {
                        y1 = Options.BoundsBottom;
                    }
                    num = (y1 * Options.Canvas.Width) + x1;
                    ChangedRect.Invalidate(x1, y2, 1, y1 - y2);
                    for (num2 = 0; num2 < (y1 - y2); num2++)
                    {
                        RasterOp.do_pixel(opcode, Options.Canvas, x1, y2 + num2, color);
                        num -= Options.Canvas.Width;
                    }
                }
            }
        }

        internal static void Reset()
        {
            StartX = 0;
            StartY = 0;
            EndX = 0;
            EndY = 0;
            Mixmode = 0;
            BackgroundColor = 0;
            Opcode = 0;
            PenStyle = 0;
            PenWidth = 0;
            PenColor = 0;
        }

        private static void setPixel(int opcode, int x, int y, int color)
        {
            int bpp = Options.Bpp;
            if ((((x >= Options.BoundsLeft) && (x <= Options.BoundsRight)) && (y >= Options.BoundsTop)) && (y <= Options.BoundsBottom))
            {
                RasterOp.do_pixel(opcode, Options.Canvas, x, y, color);
            }
        }

    }
}