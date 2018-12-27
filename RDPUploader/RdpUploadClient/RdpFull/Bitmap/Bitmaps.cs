using System;

namespace RdpUploadClient
{
    internal class Bitmaps
    {
        private const int BITMAP_COMPRESSION = 1;
        private const int NO_BITMAP_COMPRESSION_HDR = 0x400;

        internal static void processBitmapUpdates(RdpPacket data)
        {
            int num = 0;
            int x = 0;
            int y = 0;
            int num4 = 0;
            int num5 = 0;
            int cx = 0;
            int cy = 0;
            int num8 = 0;
            int num9 = 0;
            int num10 = 0;
            int num11 = 0;
            int size = 0;
            byte[] buffer = null;
            num = data.ReadLittleEndian16();
            for (int i = 0; i < num; i++)
            {
                x = data.ReadLittleEndian16();
                y = data.ReadLittleEndian16();
                num4 = data.ReadLittleEndian16();
                num5 = data.ReadLittleEndian16();
                cx = data.ReadLittleEndian16();
                cy = data.ReadLittleEndian16();
                int bpp = (data.ReadLittleEndian16() + 7) / 8;
                num10 = data.ReadLittleEndian16();
                num11 = data.ReadLittleEndian16();
                num8 = (num4 - x) + 1;
                num9 = (num5 - y) + 1;
                if (num10 == 0)
                {
                    buffer = new byte[(cx * cy) * bpp];
                    for (int j = 0; j < cy; j++)
                    {
                        data.Read(buffer, ((cy - j) - 1) * (cx * bpp), cx * bpp);
                    }
                    uint[] src = RdpBitmap.convertImage(buffer, bpp);
                    ChangedRect.Invalidate(x, y, cx, cy);
                    Options.Canvas.SetPixels(x, y, num8, num9, src, 0, 0, cx);
                }
                else
                {
                    if ((num10 & 0x400) != 0)
                    {
                        size = num11;
                    }
                    else
                    {
                        data.Position += 2L;
                        size = data.ReadLittleEndian16();
                        data.Position += 4L;
                    }
                    if (bpp == 1)
                    {
                        buffer = RdpBitmap.decompress(cx, cy, size, data, bpp);
                        ChangedRect.Invalidate(x, y, num8, num9);
                        for (int k = 0; k < num9; k++)
                        {
                            int index = k * cx;
                            int num18 = 0;
                            while (num18 < num8)
                            {
                                int num19 = buffer[index];
                                Options.Canvas.SetPixel(x + num18, y + k, RdpBitmap.convertFrom8bit(num19));
                                num18++;
                                index++;
                            }
                        }
                    }
                    else
                    {
                        uint[] numArray2 = RdpBitmap.decompressInt(cx, cy, size, data, bpp);
                        ChangedRect.Invalidate(x, y, num8, num9);
                        Options.Canvas.SetPixels(x, y, num8, num9, numArray2, 0, 0, cx);
                    }
                }
            }
        }

    }
}