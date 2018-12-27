using System;

namespace RdpUploadClient
{
    internal class Text2Order
    {
        internal static int BackgroundColor;
        internal static int BoxBottom;
        internal static int BoxLeft;
        internal static int BoxRight;
        internal static int BoxTop;
        internal static BrushOrder Brush = new BrushOrder();
        internal static int ClipBottom;
        internal static int ClipLeft;
        internal static int ClipRight;
        internal static int ClipTop;
        internal static int Flags;
        internal static int Font;
        internal static int ForegroundColor;
        internal static byte[] GlyphIndices;
        internal static int GlyphLength;
        private const int MIX_OPAQUE = 1;
        private const int MIX_TRANSPARENT = 0;
        internal static int Mixmode;
        internal static int Opcode;
        private const int TEXT2_IMPLICIT_X = 0x20;
        private const int TEXT2_VERTICAL = 4;
        internal static int X;
        internal static int Y;

        internal static void drawGlyph(int mixmode, int x, int y, int cx, int cy, byte[] data, int bgcolor, int fgcolor)
        {
            int index = 0;
            int num2 = 0x80;
            int num1 = (cx - 1) / 8;
            if ((x <= Options.BoundsRight) && (y <= Options.BoundsBottom))
            {
                int boundsLeft;
                int boundsTop;
                int boundsRight = (x + cx) - 1;
                if (boundsRight > Options.BoundsRight)
                {
                    boundsRight = Options.BoundsRight;
                }
                if (x < Options.BoundsLeft)
                {
                    boundsLeft = Options.BoundsLeft;
                }
                else
                {
                    boundsLeft = x;
                }
                int num6 = (boundsRight - x) + 1;
                int boundsBottom = (y + cy) - 1;
                if (boundsBottom > Options.BoundsBottom)
                {
                    boundsBottom = Options.BoundsBottom;
                }
                if (y < Options.BoundsTop)
                {
                    boundsTop = Options.BoundsTop;
                }
                else
                {
                    boundsTop = y;
                }
                int num8 = (boundsBottom - boundsTop) + 1;
                if (mixmode == 0)
                {
                    for (int i = 0; i < num8; i++)
                    {
                        for (int j = 0; j < num6; j++)
                        {
                            if (num2 == 0)
                            {
                                index++;
                                num2 = 0x80;
                            }
                            if ((((data[index] & num2) != 0) && ((x + j) >= boundsLeft)) && (((boundsLeft + j) > 0) && ((boundsTop + i) > 0)))
                            {
                                Options.Canvas.SetPixel(boundsLeft + j, boundsTop + i, fgcolor);
                            }
                            num2 = num2 >> 1;
                        }
                        index++;
                        num2 = 0x80;
                        if (index == data.Length)
                        {
                            index = 0;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < num8; k++)
                    {
                        for (int m = 0; m < num6; m++)
                        {
                            if (num2 == 0)
                            {
                                index++;
                                num2 = 0x80;
                            }
                            if ((((x + m) >= boundsLeft) && ((x + m) > 0)) && ((y + k) > 0))
                            {
                                if ((data[index] & num2) != 0)
                                {
                                    Options.Canvas.SetPixel(boundsLeft + m, boundsTop + k, fgcolor);
                                }
                                else
                                {
                                    Options.Canvas.SetPixel(boundsLeft + m, boundsTop + k, bgcolor);
                                }
                            }
                            num2 = num2 >> 1;
                        }
                        index++;
                        num2 = 0x80;
                        if (index == data.Length)
                        {
                            index = 0;
                        }
                    }
                }
            }
        }

        internal static void drawText()
        {
            int x = X;
            int y = Y;
            int num5 = 0;
            int sourceIndex = 0;
            int cx = BoxRight - BoxLeft;
            int cy = BoxBottom - BoxTop;
            int num9 = ClipRight - ClipLeft;
            int num10 = ClipBottom - ClipTop;
            int color = RdpBitmap.convertColor(BackgroundColor);
            int fgcolor = RdpBitmap.convertColor(ForegroundColor);
            Options.Enter();
            try
            {
                ChangedRect.Invalidate(BoxLeft, BoxTop, num9, num10);
                if (cx > 1)
                {
                    fillRectangle(BoxLeft, BoxTop, cx, cy, color);
                }
                else if (Mixmode == 1)
                {
                    fillRectangle(ClipLeft, ClipTop, num9, num10, color);
                }
                int num11 = 0;
                while (num11 < GlyphLength)
                {
                    Glyph glyph;
                    byte[] buffer;
                    switch ((GlyphIndices[sourceIndex + num11] & 0xff))
                    {
                        case 0xfe:
                            buffer = Cache.getText(GlyphIndices[(sourceIndex + num11) + 1] & 0xff);
                            if (((buffer != null) && (buffer[1] == 0)) && ((Flags & 0x20) == 0))
                            {
                                if ((Flags & 4) == 0)
                                {
                                    goto Label_01C7;
                                }
                                y += GlyphIndices[(sourceIndex + num11) + 2] & 0xff;
                            }
                            goto Label_01DD;

                        case 0xff:
                            if ((num11 + 2) >= GlyphLength)
                            {
                                throw new RDFatalException("Text order is incorrect");
                            }
                            break;

                        default:
                            goto Label_0339;
                    }
                    byte[] destinationArray = new byte[GlyphIndices[(sourceIndex + num11) + 2] & 0xff];
                    Array.Copy(GlyphIndices, sourceIndex, destinationArray, 0, GlyphIndices[(sourceIndex + num11) + 2] & 0xff);
                    Cache.putText(GlyphIndices[(sourceIndex + num11) + 1] & 0xff, destinationArray);
                    GlyphLength -= num11 + 3;
                    sourceIndex = num11 + 3;
                    num11 = 0;
                    continue;
                Label_01C7:
                    x += GlyphIndices[(sourceIndex + num11) + 2] & 0xff;
                Label_01DD:
                    if ((num11 + 2) < GlyphLength)
                    {
                        num11 += 3;
                    }
                    else
                    {
                        num11 += 2;
                    }
                    GlyphLength -= num11;
                    sourceIndex = num11;
                    num11 = 0;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        glyph = Cache.getFont(Font, buffer[i] & 0xff);
                        if ((Flags & 0x20) == 0)
                        {
                            num5 = buffer[++i] & 0xff;
                            if ((num5 & 0x80) != 0)
                            {
                                if ((Flags & 4) != 0)
                                {
                                    int num13 = twosComplement16((buffer[i + 1] & 0xff) | ((buffer[i + 2] & 0xff) << 8));
                                    y += num13;
                                    i += 2;
                                }
                                else
                                {
                                    int num14 = twosComplement16((buffer[i + 1] & 0xff) | ((buffer[i + 2] & 0xff) << 8));
                                    x += num14;
                                    i += 2;
                                }
                            }
                            else if ((Flags & 4) != 0)
                            {
                                y += num5;
                            }
                            else
                            {
                                x += num5;
                            }
                        }
                        if (glyph != null)
                        {
                            drawGlyph(Mixmode, X + glyph.Offset, Y + glyph.BaseLine, glyph.Width, glyph.Height, glyph.FontData, color, fgcolor);
                            if ((Flags & 0x20) != 0)
                            {
                                x += Options.e;
                            }
                        }
                    }
                    continue;
                Label_0339:
                    glyph = Cache.getFont(Font, GlyphIndices[sourceIndex + num11] & 0xff);
                    if ((Flags & 0x20) == 0)
                    {
                        num5 = GlyphIndices[sourceIndex + ++num11] & 0xff;
                        if ((num5 & 0x80) != 0)
                        {
                            if ((Flags & 4) != 0)
                            {
                                int num15 = twosComplement16((GlyphIndices[(sourceIndex + num11) + 1] & 0xff) | ((GlyphIndices[(sourceIndex + num11) + 2] & 0xff) << 8));
                                y += num15;
                                num11 += 2;
                            }
                            else
                            {
                                int num16 = twosComplement16((GlyphIndices[(sourceIndex + num11) + 1] & 0xff) | ((GlyphIndices[(sourceIndex + num11) + 2] & 0xff) << 8));
                                x += num16;
                                num11 += 2;
                            }
                        }
                        else if ((Flags & 4) != 0)
                        {
                            y += num5;
                        }
                        else
                        {
                            x += num5;
                        }
                    }
                    if (glyph != null)
                    {
                        int offset = glyph.Offset;
                        drawGlyph(Mixmode, x + ((short) glyph.Offset), y + ((short) glyph.BaseLine), glyph.Width, glyph.Height, glyph.FontData, color, fgcolor);
                        if ((Flags & 0x20) != 0)
                        {
                            x += glyph.Width;
                        }
                    }
                    num11++;
                }
            }
            finally
            {
                Options.Exit();
            }
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
                Options.Canvas.SetPixels(x, y, cx, cy, color);
            }
        }

        internal static void Reset()
        {
            Font = 0;
            Flags = 0;
            Opcode = 0;
            Mixmode = 0;
            ForegroundColor = 0;
            BackgroundColor = 0;
            ClipLeft = ClipRight = ClipTop = ClipBottom = 0;
            BoxLeft = BoxRight = BoxTop = BoxBottom = 0;
            X = Y = 0;
            GlyphLength = 0;
        }

        private static int twosComplement16(int val)
        {
            if ((val & 0x8000) == 0)
            {
                return val;
            }
            return -((~val & 0xffff) + 1);
        }

    }
}