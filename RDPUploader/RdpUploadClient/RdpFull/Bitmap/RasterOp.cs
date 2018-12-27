using System;

namespace RdpUploadClient
{
    internal class RasterOp
    {
        public static void do_array(int opcode, RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy)
        {
            int bpp = Options.Bpp;
            switch (opcode)
            {
                case 0:
                    ropClear(biDst, dstwidth, x, y, cx, cy, bpp);
                    return;

                case 1:
                    ropNor(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 2:
                    ropAndInverted(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 3:
                    ropInvert(biDst, src, srcwidth, srcx, srcy, cx, cy, bpp);
                    ropCopy(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 4:
                    ropInvert(biDst, null, dstwidth, x, y, cx, cy, bpp);
                    ropAnd(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 5:
                    ropInvert(biDst, null, dstwidth, x, y, cx, cy, bpp);
                    return;

                case 6:
                    ropXor(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 7:
                    ropNand(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 8:
                    ropAnd(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 9:
                    ropEquiv(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 10:
                    break;

                case 11:
                    ropOrInverted(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 12:
                    ropCopy(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 13:
                    ropInvert(biDst, null, dstwidth, x, y, cx, cy, bpp);
                    ropOr(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 14:
                    ropOr(biDst, dstwidth, x, y, cx, cy, src, srcwidth, srcx, srcy, bpp);
                    return;

                case 15:
                    ropSet(biDst, dstwidth, x, y, cx, cy, bpp);
                    return;

                default:
                    // do_array unsupported opcode
                    break;
            }
        }

        public static void do_pixel(int opcode, RdpCanvas dst, int x, int y, int color)
        {
            int num = Options.bpp_mask;
            if (dst != null)
            {
                int pixel = dst.GetPixel(x, y);
                switch (opcode)
                {
                    case 0:
                        dst.SetPixel(x, y, 0);
                        return;

                    case 1:
                        dst.SetPixel(x, y, ~(pixel | color) & num);
                        return;

                    case 2:
                        dst.SetPixel(x, y, pixel & (~color & num));
                        return;

                    case 3:
                        dst.SetPixel(x, y, ~color & num);
                        return;

                    case 4:
                        dst.SetPixel(x, y, (~pixel & color) * num);
                        return;

                    case 5:
                        dst.SetPixel(x, y, ~pixel & num);
                        return;

                    case 6:
                        dst.SetPixel(x, y, pixel ^ (color & num));
                        return;

                    case 7:
                        dst.SetPixel(x, y, (~pixel & color) & num);
                        return;

                    case 8:
                        dst.SetPixel(x, y, pixel & (color & num));
                        return;

                    case 9:
                        dst.SetPixel(x, y, pixel ^ (~color & num));
                        return;

                    case 10:
                        break;

                    case 11:
                        dst.SetPixel(x, y, pixel | (~color & num));
                        return;

                    case 12:
                        dst.SetPixel(x, y, color);
                        return;

                    case 13:
                        dst.SetPixel(x, y, (~pixel | color) & num);
                        return;

                    case 14:
                        dst.SetPixel(x, y, pixel | (color & num));
                        return;

                    case 15:
                        dst.SetPixel(x, y, num);
                        return;

                    default:
                        // do_byte unsupported opcode:  opcode
                        break;
                }
            }
        }

        private static void ropAnd(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, pixel & (((int) src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropAndInverted(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + cx, y + cy);
                    biDst.SetPixel(x + cx, y + cy, pixel & (((int) ~src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropClear(RdpCanvas biDst, int width, int x, int y, int cx, int cy, int Bpp)
        {
            for (int i = x; i < (x + cx); i++)
            {
                for (int j = y; j < (y + cy); j++)
                {
                    biDst.SetPixel(i, j, 0);
                }
            }
        }

        private static void ropCopy(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            if (src == null)
            {
                biDst.CopyPixels(srcx, srcy, x, y, cx, cy);
            }
            else
            {
                int index = (srcy * srcwidth) + srcx;
                for (int i = 0; i < cy; i++)
                {
                    int num3 = 0;
                    while (num3 < cx)
                    {
                        if (index >= src.Length)
                        {
                            index = src.Length - 1;
                        }
                        biDst.SetPixel(x + num3, y + i, (int) src[index]);
                        num3++;
                        index++;
                    }
                    index += srcwidth - cx;
                }
            }
        }

        private static void ropEquiv(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, pixel ^ (((int) ~src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropInvert(RdpCanvas biDst, uint[] dest, int width, int x, int y, int cx, int cy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (y * width) + x;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    if (biDst != null)
                    {
                        int pixel = biDst.GetPixel(x + j, y + i);
                        biDst.SetPixel(x + j, y + i, ~pixel & num);
                    }
                    else
                    {
                        dest[index] = ~dest[index] & ((uint) num);
                    }
                    index++;
                }
                index += width - cx;
            }
        }

        private static void ropNand(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, ((int) ~(pixel & src[index])) & num);
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropNor(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    biDst.SetPixel(x + cx, y + cy, ((int) ~(biDst.GetPixel(x + cx, y + cy) | src[index])) & num);
                }
                index += srcwidth - cx;
            }
        }

        private static void ropOr(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, pixel | (((int) src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropOrInverted(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, pixel | (((int) ~src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

        private static void ropSet(RdpCanvas biDst, int width, int x, int y, int cx, int cy, int Bpp)
        {
            int color = Options.bpp_mask;
            for (int i = x; i < (x + cx); i++)
            {
                for (int j = y; j < (y + cy); j++)
                {
                    biDst.SetPixel(i, j, color);
                }
            }
        }

        private static void ropXor(RdpCanvas biDst, int dstwidth, int x, int y, int cx, int cy, uint[] src, int srcwidth, int srcx, int srcy, int Bpp)
        {
            int num = Options.bpp_mask;
            int index = (srcy * srcwidth) + srcx;
            for (int i = 0; i < cy; i++)
            {
                for (int j = 0; j < cx; j++)
                {
                    int pixel = biDst.GetPixel(x + j, y + i);
                    biDst.SetPixel(x + j, y + i, pixel ^ (((int) src[index]) & num));
                    index++;
                }
                index += srcwidth - cx;
            }
        }

    }
}