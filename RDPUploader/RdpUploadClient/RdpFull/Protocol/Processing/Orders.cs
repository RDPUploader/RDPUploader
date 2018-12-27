using System;
using System.IO;

namespace RdpUploadClient
{
    internal class Orders
    {
        private const int BUFSIZE_MASK = 0x3fff;
        private const int CG2_GLYPH_UNICODE_PRESENT = 1;
        private const int DO_NOT_CACHE = 0x800;
        private const int GLYPH_ORDER_REV2 = 2;
        private const int HEIGHT_SAME_AS_WIDTH = 0x80;
        private const int ID_MASK = 7;
        private const int LONG_FORMAT = 0x80;
        internal static int m_OrderType;
        private const int MIX_OPAQUE = 1;
        private const int MIX_TRANSPARENT = 0;
        private const int MODE_MASK = 0x38;
        private const int MODE_SHIFT = 3;
        private const int NO_BITMAP_COMPRESSION_HDR = 0x400;
        private const int NO_BITMAP_COMPRESSION_HDR2 = 0x400;
        private const int PERSISTENT_KEY_PRESENT = 0x100;
        private const int RDP_ORDER_BMPCACHE = 2;
        private const int RDP_ORDER_BMPCACHE2 = 5;
        private const int RDP_ORDER_BMPCACHE3 = 8;
        private const int RDP_ORDER_BOUNDS = 4;
        private const int RDP_ORDER_BRUSH = 7;
        private const int RDP_ORDER_CHANGE = 8;
        private const int RDP_ORDER_COLCACHE = 1;
        private const int RDP_ORDER_DELTA = 0x10;
        private const int RDP_ORDER_DESKSAVE = 11;
        private const int RDP_ORDER_DESTBLT = 0;
        private const int RDP_ORDER_FASTGLYPH = 0x18;
        private const int RDP_ORDER_FASTINDEX = 0x13;
        private const int RDP_ORDER_FONTCACHE = 3;
        private const int RDP_ORDER_GLYPHINDEX = 0x1b;
        private const int RDP_ORDER_LASTBOUNDS = 0x20;
        private const int RDP_ORDER_LINE = 9;
        private const int RDP_ORDER_MEMBLT = 13;
        private const int RDP_ORDER_MULTIDESTBLT = 15;
        private const int RDP_ORDER_MULTIPATBLT = 0x10;
        private const int RDP_ORDER_MULTIRECT = 0x12;
        private const int RDP_ORDER_MULTISCREENBLT = 0x11;
        private const int RDP_ORDER_PATBLT = 1;
        private const int RDP_ORDER_POLYLINE = 0x16;
        private const int RDP_ORDER_RAW_BMPCACHE = 0;
        private const int RDP_ORDER_RAW_BMPCACHE2 = 4;
        private const int RDP_ORDER_RECT = 10;
        private const int RDP_ORDER_SCREENBLT = 2;
        private const int RDP_ORDER_SECONDARY = 2;
        private const int RDP_ORDER_SMALL = 0x40;
        private const int RDP_ORDER_STANDARD = 1;
        private const int RDP_ORDER_TINY = 0x80;
        private const int RDP_ORDER_TRIBLT = 14;
        private const int TEXT2_IMPLICIT_X = 0x20;
        private const int TEXT2_VERTICAL = 4;

        private static int inPresent(RdpPacket data, int flags, int size)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;

            if ((flags & 0x40) != 0)
            {
                size--;
            }

            if ((flags & 0x80) != 0)
            {
                if (size < 2)
                {
                    size = 0;
                }
                else
                {
                    size -= 2;
                }
            }

            for (num3 = 0; num3 < size; num3++)
            {
                num2 = data.ReadByte();
                num |= num2 << (num3 * 8);
            }

            return num;
        }

        private static void parseBounds(RdpPacket data)
        {
            int num = 0;
            num = data.ReadByte();

            if ((num & 1) != 0)
            {
                SurfaceClip.Left = setCoordinate(data, SurfaceClip.Left, false);
            }
            else if ((num & 0x10) != 0)
            {
                SurfaceClip.Left = setCoordinate(data, SurfaceClip.Left, true);
            }

            if ((num & 2) != 0)
            {
                SurfaceClip.Top = setCoordinate(data, SurfaceClip.Top, false);
            }
            else if ((num & 0x20) != 0)
            {
                SurfaceClip.Top = setCoordinate(data, SurfaceClip.Top, true);
            }

            if ((num & 4) != 0)
            {
                SurfaceClip.Right = setCoordinate(data, SurfaceClip.Right, false);
            }
            else if ((num & 0x40) != 0)
            {
                SurfaceClip.Right = setCoordinate(data, SurfaceClip.Right, true);
            }

            if ((num & 8) != 0)
            {
                SurfaceClip.Bottom = setCoordinate(data, SurfaceClip.Bottom, false);
            }
            else if ((num & 0x80) != 0)
            {
                SurfaceClip.Bottom = setCoordinate(data, SurfaceClip.Bottom, true);
            }

            if (data.Position > data.Length)
            {
                throw new RDFatalException("Bad order bound packet flags!");
            }
        }

        private static void parseBrush(RdpPacket data, BrushOrder brush, int present)
        {
            if ((present & 1) != 0)
            {
                brush.XOrigin = data.ReadByte();
            }

            if ((present & 2) != 0)
            {
                brush.YOrigin = data.ReadByte();
            }

            if ((present & 4) != 0)
            {
                brush.Style = data.ReadByte();
            }

            byte[] pattern = brush.Pattern;

            if ((present & 8) != 0)
            {
                pattern[0] = (byte) data.ReadByte();
            }

            if ((present & 0x10) != 0)
            {
                for (int i = 1; i < 8; i++)
                {
                    pattern[i] = (byte) data.ReadByte();
                }
            }
        }

        private static void process_bmpcache2(RdpPacket data, int flags, bool compressed)
        {
            RdpBitmap bitmap;
            int num3;
            int num4;
            byte[] buffer;
            ulong key = 0L;
            int num2 = flags & 7;
            int bpp = ((flags & 0x38) >> 3) - 2;

            if ((flags & 0x800) != 0)
            {
                throw new RDFatalException("DO_NOT_CACHE flag not supported!");
            }

            if ((flags & 0x100) != 0)
            {
                byte[] buffer2 = new byte[8];
                data.Read(buffer2, 0, buffer2.Length);
                key = BitConverter.ToUInt64(buffer2, 0);
            }

            if ((flags & 0x80) != 0)
            {
                num3 = data.ReadEncodedUnsigned16();
                num4 = num3;
            }
            else
            {
                num3 = data.ReadEncodedUnsigned16();
                num4 = data.ReadEncodedUnsigned16();
            }

            int size = data.ReadEncoded32();
            int num6 = data.ReadEncodedUnsigned16();

            if (compressed)
            {
                if ((flags & 0x400) == 0)
                {
                    data.Position += 8L;
                    size -= 8;
                }

                if (bpp == 1)
                {
                    buffer = RdpBitmap.decompress(num3, num4, size, data, bpp);

                    if (buffer == null)
                    {
                        return;
                    }

                    bitmap = new RdpBitmap(buffer, num3, num4, bpp, 0, 0);
                }
                else
                {
                    uint[] numArray = RdpBitmap.decompressInt(num3, num4, size, data, bpp);

                    if (numArray == null)
                    {
                        return;
                    }

                    bitmap = new RdpBitmap(numArray, num3, num4, bpp, 0, 0);
                }
            }
            else
            {
                buffer = new byte[(num3 * num4) * bpp];

                for (int i = 0; i < num4; i++)
                {
                    data.Read(buffer, ((num4 - i) - 1) * (num3 * bpp), num3 * bpp);
                }
                if (bpp == 1)
                {
                    bitmap = new RdpBitmap(buffer, num3, num4, bpp, 0, 0);
                }
                else
                {
                    bitmap = new RdpBitmap(RdpBitmap.convertImage(buffer, bpp), num3, num4, bpp, 0, 0);
                }
            }

            if (bitmap != null)
            {
                Cache.putBitmap(num2, num6, bitmap, 0);

                if ((flags & 0x100) != 0)
                {
                    bitmap.PersistCache(key);
                    Cache.m_BitmapCaches++;
                }
            }
        }

        private static void process_bmpcache3(RdpPacket data, int flags)
        {
            ulong num3 = 0L;
            int num = flags & 7;

            if ((flags & 0x800) != 0)
            {
                throw new RDFatalException("DO_NOT_CACHE flag not supported!");
            }

            int num2 = data.ReadEncodedUnsigned16();
            byte[] buffer = new byte[8];
            data.Read(buffer, 0, buffer.Length);
            num3 = BitConverter.ToUInt64(buffer, 0);
            process_bmpdata(data, flags, num, num2, num3);
        }

        private static void process_bmpdata(RdpPacket data, int flags, int cache_id, int cache_idx, ulong persist_key)
        {
            RdpBitmap bitmap;
            byte[] buffer;
            int bpp = data.ReadByte();
            data.ReadByte();
            data.ReadByte();
            int num2 = data.ReadByte();
            int width = data.ReadLittleEndian16();
            int height = data.ReadLittleEndian16();
            int size = data.ReadLittleEndian32();

            if (num2 == 1)
            {
                if (bpp == 1)
                {
                    buffer = RdpBitmap.decompress(width, height, size, data, bpp);

                    if (buffer == null)
                    {
                        return;
                    }

                    bitmap = new RdpBitmap(buffer, width, height, bpp, 0, 0);
                }
                else
                {
                    uint[] numArray = RdpBitmap.decompressInt(width, height, size, data, bpp);

                    if (numArray == null)
                    {
                        return;
                    }

                    bitmap = new RdpBitmap(numArray, width, height, bpp, 0, 0);
                }
            }
            else
            {
                buffer = new byte[(width * height) * bpp];

                for (int i = 0; i < height; i++)
                {
                    data.Read(buffer, ((height - i) - 1) * (width * bpp), width * bpp);
                }

                if (bpp == 1)
                {
                    bitmap = new RdpBitmap(buffer, width, height, bpp, 0, 0);
                }
                else
                {
                    bitmap = new RdpBitmap(RdpBitmap.convertImage(buffer, bpp), width, height, bpp, 0, 0);
                }
            }

            if (bitmap != null)
            {
                Cache.putBitmap(cache_id, cache_idx, bitmap, 0);

                if ((flags & 0x100) != 0)
                {
                    bitmap.PersistCache(persist_key);
                    Cache.m_BitmapCaches++;
                }
            }
        }

        private static void processBitmapCache(RdpPacket data, bool bCompressed, bool bNoCompressionHdr)
        {
            int num2;
            int num = num2 = 0;
            int num3 = data.ReadByte();
            data.ReadByte();
            int width = data.ReadByte();
            int height = data.ReadByte();
            int bpp = (data.ReadByte() + 7) / 8;
            num = data.ReadLittleEndian16();
            int num8 = data.ReadLittleEndian16();

            if (!bCompressed)
            {
                byte[] buffer = new byte[(width * height) * bpp];
                int offset = (height - 1) * (width * bpp);

                for (int i = 0; i < height; i++)
                {
                    data.Read(buffer, offset, width * bpp);
                    offset -= width * bpp;
                }

                Cache.putBitmap(num3, num8, new RdpBitmap(buffer, width, height, bpp, 0, 0), 0);
            }
            else
            {
                if (!bNoCompressionHdr)
                {
                    data.ReadLittleEndian16();
                    num2 = data.ReadLittleEndian16();
                    data.ReadLittleEndian16();
                    data.ReadLittleEndian16();
                }
                else
                {
                    num2 = num;
                }

                if (bpp == 1)
                {
                    byte[] buffer2 = RdpBitmap.decompress(width, height, num2, data, bpp);

                    if (buffer2 != null)
                    {
                        Cache.putBitmap(num3, num8, new RdpBitmap(buffer2, width, height, bpp, 0, 0), 0);
                    }
                }
                else
                {
                    uint[] numArray = RdpBitmap.decompressInt(width, height, num2, data, bpp);

                    if (numArray != null)
                    {
                        Cache.putBitmap(num3, num8, new RdpBitmap(numArray, width, height, bpp, 0, 0), 0);
                    }
                }
            }
        }

        private static void processColorCache(RdpPacket data)
        {
            byte[] buffer = null;
            byte[] r = null;
            byte[] g = null;
            byte[] b = null;
            int index = 0;
            int num2 = data.ReadByte();
            int count = data.ReadLittleEndian16();
            buffer = new byte[count * 4];
            r = new byte[count];
            g = new byte[count];
            b = new byte[count];
            data.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < count; i++)
            {
                b[i] = buffer[index];
                g[i] = buffer[index + 1];
                r[i] = buffer[index + 2];
                index += 4;
            }

            Cache.put_colourmap(num2, new Palette(8, count, r, g, b));
        }

        private static void processDeskSave(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                DeskSaveOrder.Offset = data.ReadLittleEndian32();
            }

            if ((present & 2) != 0)
            {
                DeskSaveOrder.Left = setCoordinate(data, DeskSaveOrder.Left, delta);
            }

            if ((present & 4) != 0)
            {
                DeskSaveOrder.Top = setCoordinate(data, DeskSaveOrder.Top, delta);
            }

            if ((present & 8) != 0)
            {
                DeskSaveOrder.Right = setCoordinate(data, DeskSaveOrder.Right, delta);
            }

            if ((present & 0x10) != 0)
            {
                DeskSaveOrder.Bottom = setCoordinate(data, DeskSaveOrder.Bottom, delta);
            }

            if ((present & 0x20) != 0)
            {
                DeskSaveOrder.Action = data.ReadByte();
            }

            int cx = (DeskSaveOrder.Right - DeskSaveOrder.Left) + 1;
            int cy = (DeskSaveOrder.Bottom - DeskSaveOrder.Top) + 1;

            if (DeskSaveOrder.Action == 0)
            {
                int[] numArray = Options.Canvas.GetPixels(DeskSaveOrder.Left, DeskSaveOrder.Top, cx, cy);
                Cache.putDesktop(DeskSaveOrder.Offset, cx, cy, numArray);
            }
            else
            {
                int[] src = Cache.getDesktopInt(DeskSaveOrder.Offset, cx, cy);
                Options.Canvas.SetPixels(DeskSaveOrder.Left, DeskSaveOrder.Top, cx, cy, src, 0, 0, cx);
            }
        }

        private static void processDestBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                DestBltOrder.X = setCoordinate(data, DestBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                DestBltOrder.Y = setCoordinate(data, DestBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                DestBltOrder.CX = setCoordinate(data, DestBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                DestBltOrder.CY = setCoordinate(data, DestBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                DestBltOrder.Opcode = ROP2_S(data.ReadByte());
            }

            DestBltOrder.drawDestBltOrder();
        }

        private static void processFontCache(RdpPacket data, int extraFlags)
        {
            int font = 0;
            int num2 = 0;
            int character = 0;
            int offset = 0;
            int baseline = 0;
            int width = 0;
            int height = 0;
            int count = 0;
            byte[] buffer = null;
            int num9 = (extraFlags >> 4) & 15;

            if ((num9 & 2) != 0)
            {
                font = extraFlags & 15;
                num2 = extraFlags >> 8;
            }
            else
            {
                font = data.ReadByte();
                num2 = data.ReadByte();
            }

            for (int i = 0; i < num2; i++)
            {
                if ((num9 & 2) != 0)
                {
                    character = data.ReadByte();
                    offset = data.ReadEncodedSigned16();
                    baseline = data.ReadEncodedSigned16();
                    width = data.ReadEncodedUnsigned16();
                    height = data.ReadEncodedUnsigned16();
                }
                else
                {
                    character = data.ReadLittleEndian16();
                    offset = data.ReadLittleEndian16();
                    baseline = data.ReadLittleEndian16();
                    width = data.ReadLittleEndian16();
                    height = data.ReadLittleEndian16();
                }

                count = ((height * ((width + 7) / 8)) + 3) & -4;
                buffer = new byte[count];
                data.Read(buffer, 0, count);
                Glyph glyph = new Glyph(font, character, offset, baseline, width, height, buffer);
                Cache.putFont(glyph);
            }
        }

        private static void processGlyphIndex(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                Text2Order.Font = data.ReadByte();
            }

            if ((present & 2) != 0)
            {
                Text2Order.Flags = data.ReadByte();
            }

            if ((present & 4) != 0)
            {
                Text2Order.Opcode = data.ReadByte();
            }

            if ((present & 8) != 0)
            {
                Text2Order.Mixmode = data.ReadByte();
            }

            if ((present & 0x10) != 0)
            {
                Text2Order.ForegroundColor = setColor(data);
            }

            if ((present & 0x20) != 0)
            {
                Text2Order.BackgroundColor = setColor(data);
            }

            if ((present & 0x40) != 0)
            {
                Text2Order.ClipLeft = data.ReadLittleEndian16();
            }

            if ((present & 0x80) != 0)
            {
                Text2Order.ClipTop = data.ReadLittleEndian16();
            }

            if ((present & 0x100) != 0)
            {
                Text2Order.ClipRight = data.ReadLittleEndian16();
            }

            if ((present & 0x200) != 0)
            {
                Text2Order.ClipBottom = data.ReadLittleEndian16();
            }

            if ((present & 0x400) != 0)
            {
                Text2Order.BoxLeft = data.ReadLittleEndian16();
            }

            if ((present & 0x800) != 0)
            {
                Text2Order.BoxTop = data.ReadLittleEndian16();
            }

            if ((present & 0x1000) != 0)
            {
                Text2Order.BoxRight = data.ReadLittleEndian16();
            }

            if ((present & 0x2000) != 0)
            {
                Text2Order.BoxBottom = data.ReadLittleEndian16();
            }

            parseBrush(data, Text2Order.Brush, present >> 14);

            if ((present & 0x80000) != 0)
            {
                Text2Order.X = data.ReadLittleEndian16();
            }

            if ((present & 0x100000) != 0)
            {
                Text2Order.Y = data.ReadLittleEndian16();
            }

            if ((present & 0x200000) != 0)
            {
                int num = data.ReadByte();
                Text2Order.GlyphLength = num;
                byte[] buffer = new byte[num];
                data.Read(buffer, 0, buffer.Length);
                Text2Order.GlyphIndices = buffer;
            }

            Text2Order.drawText();
        }

        private static void processLine(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                LineOrder.Mixmode = data.ReadLittleEndian16();
            }

            if ((present & 2) != 0)
            {
                LineOrder.StartX = setCoordinate(data, LineOrder.StartX, delta);
            }

            if ((present & 4) != 0)
            {
                LineOrder.StartY = setCoordinate(data, LineOrder.StartY, delta);
            }

            if ((present & 8) != 0)
            {
                LineOrder.EndX = setCoordinate(data, LineOrder.EndX, delta);
            }

            if ((present & 0x10) != 0)
            {
                LineOrder.EndY = setCoordinate(data, LineOrder.EndY, delta);
            }

            if ((present & 0x20) != 0)
            {
                LineOrder.BackgroundColor = setColor(data);
            }

            if ((present & 0x40) != 0)
            {
                LineOrder.Opcode = data.ReadByte();
            }

            present = present >> 7;

            if ((present & 1) != 0)
            {
                LineOrder.PenStyle = data.ReadByte();
            }

            if ((present & 2) != 0)
            {
                LineOrder.PenWidth = data.ReadByte();
            }

            if ((present & 4) != 0)
            {
                LineOrder.PenColor = setColor(data);
            }

            LineOrder.drawLineOrder();
        }

        private static void processMemBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                MemBltOrder.CacheID = data.ReadByte();
                MemBltOrder.ColorTable = data.ReadByte();
            }

            if ((present & 2) != 0)
            {
                MemBltOrder.X = setCoordinate(data, MemBltOrder.X, delta);
            }

            if ((present & 4) != 0)
            {
                MemBltOrder.Y = setCoordinate(data, MemBltOrder.Y, delta);
            }

            if ((present & 8) != 0)
            {
                MemBltOrder.CX = setCoordinate(data, MemBltOrder.CX, delta);
            }

            if ((present & 0x10) != 0)
            {
                MemBltOrder.CY = setCoordinate(data, MemBltOrder.CY, delta);
            }

            if ((present & 0x20) != 0)
            {
                MemBltOrder.Opcode = ROP2_S(data.ReadByte());
            }

            if ((present & 0x40) != 0)
            {
                MemBltOrder.SrcX = setCoordinate(data, MemBltOrder.SrcX, delta);
            }

            if ((present & 0x80) != 0)
            {
                MemBltOrder.SrcY = setCoordinate(data, MemBltOrder.SrcY, delta);
            }

            if ((present & 0x100) != 0)
            {
                MemBltOrder.CacheIDX = data.ReadLittleEndian16();
            }

            MemBltOrder.drawMemBltOrder();
        }

        private static void processMultiDestBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                MultiDestBltOrder.X = setCoordinate(data, MultiDestBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                MultiDestBltOrder.Y = setCoordinate(data, MultiDestBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                MultiDestBltOrder.CX = setCoordinate(data, MultiDestBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                MultiDestBltOrder.CY = setCoordinate(data, MultiDestBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                MultiDestBltOrder.Opcode = ROP2_S(data.ReadByte());
            }

            if ((present & 0x20) != 0)
            {
                MultiDestBltOrder.DeltaEntries = data.ReadByte();
            }

            if ((present & 0x40) != 0)
            {
                MultiDestBltOrder.DeltaList = readEncodedDeltaRects(data, MultiDestBltOrder.DeltaEntries);
            }

            MultiDestBltOrder.drawMultiDestBltOrder();
        }

        private static void processMultiPatBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                MultiPatBltOrder.X = setCoordinate(data, MultiPatBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                MultiPatBltOrder.Y = setCoordinate(data, MultiPatBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                MultiPatBltOrder.CX = setCoordinate(data, MultiPatBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                MultiPatBltOrder.CY = setCoordinate(data, MultiPatBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                MultiPatBltOrder.Opcode = ROP2_P(data.ReadByte());
            }

            if ((present & 0x20) != 0)
            {
                MultiPatBltOrder.BackgroundColor = setColor(data);
            }

            if ((present & 0x40) != 0)
            {
                MultiPatBltOrder.ForegroundColor = setColor(data);
            }

            parseBrush(data, MultiPatBltOrder.Brush, present >> 7);

            if ((present & 0x1000) != 0)
            {
                MultiPatBltOrder.DeltaEntries = data.ReadByte();
            }

            if ((present & 0x2000) != 0)
            {
                MultiPatBltOrder.DeltaList = readEncodedDeltaRects(data, MultiPatBltOrder.DeltaEntries);
            }

            MultiPatBltOrder.drawMultiPatBltOrder();
        }

        private static void processMultiRectangle(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                MultiRectangleOrder.X = setCoordinate(data, MultiRectangleOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                MultiRectangleOrder.Y = setCoordinate(data, MultiRectangleOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                MultiRectangleOrder.CX = setCoordinate(data, MultiRectangleOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                MultiRectangleOrder.CY = setCoordinate(data, MultiRectangleOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                MultiRectangleOrder.ColourR = data.ReadByte();
            }

            if ((present & 0x20) != 0)
            {
                MultiRectangleOrder.ColourG = data.ReadByte();
            }

            if ((present & 0x40) != 0)
            {
                MultiRectangleOrder.ColourB = data.ReadByte();
            }

            if ((present & 0x80) != 0)
            {
                MultiRectangleOrder.DeltaEntries = data.ReadByte();
            }

            if ((present & 0x100) != 0)
            {
                MultiRectangleOrder.DeltaList = readEncodedDeltaRects(data, MultiRectangleOrder.DeltaEntries);
            }

            MultiRectangleOrder.drawMultiRectangleOrder();
        }

        private static void processMultiScreenBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                MultiScreenBltOrder.X = setCoordinate(data, MultiScreenBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                MultiScreenBltOrder.Y = setCoordinate(data, MultiScreenBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                MultiScreenBltOrder.CX = setCoordinate(data, MultiScreenBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                MultiScreenBltOrder.CY = setCoordinate(data, MultiScreenBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                MultiScreenBltOrder.Opcode = ROP2_S(data.ReadByte());
            }

            if ((present & 0x20) != 0)
            {
                MultiScreenBltOrder.SrcX = setCoordinate(data, MultiScreenBltOrder.SrcX, delta);
            }

            if ((present & 0x40) != 0)
            {
                MultiScreenBltOrder.SrcY = setCoordinate(data, MultiScreenBltOrder.SrcY, delta);
            }

            if ((present & 0x80) != 0)
            {
                MultiScreenBltOrder.DeltaEntries = data.ReadByte();
            }

            if ((present & 0x100) != 0)
            {
                MultiScreenBltOrder.DeltaList = readEncodedDeltaRects(data, MultiScreenBltOrder.DeltaEntries);
            }

            MultiScreenBltOrder.drawMultiScreenBltOrder();
        }

        public static void processOrders(RdpPacket data, int next_packet, int n_orders)
        {
            int present = 0;
            int flags = 0;
            int size = 0;
            long position = data.Position;

            for (int i = 0; i < n_orders; i++)
            {
                flags = data.ReadByte();

                if ((flags & 1) == 0)
                {
                    throw new RDFatalException("Bad order flags: " + flags.ToString() + "!");
                }

                if ((flags & 2) != 0)
                {
                    // processSecondaryOrders
                    processSecondaryOrders(data);
                    continue;
                }

                if ((flags & 8) != 0)
                {
                    m_OrderType = data.ReadByte();
                }

                switch (m_OrderType)
                {
                    case 0x18:
                    case 9:
                    case 13:
                    case 0x10:
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 1:
                        size = 2;
                        break;

                    case 0x1b:
                    case 14:
                        size = 3;
                        break;

                    default:
                        size = 1;
                        break;
                }
                present = inPresent(data, flags, size);

                if ((flags & 4) != 0)
                {
                    if ((flags & 0x20) == 0)
                    {
                        // parseBounds
                        parseBounds(data);
                    }

                    Options.BoundsLeft = SurfaceClip.Left;
                    Options.BoundsRight = SurfaceClip.Right;
                    Options.BoundsTop = SurfaceClip.Top;
                    Options.BoundsBottom = SurfaceClip.Bottom;
                }

                bool delta = (flags & 0x10) != 0;

                switch (m_OrderType)
                {
                    case 0:
                        // processDestBlt
                        processDestBlt(data, present, delta);
                        break;

                    case 1:
                        // processPatBlt
                        processPatBlt(data, present, delta);
                        break;

                    case 2:
                        // processScreenBlt
                        processScreenBlt(data, present, delta);
                        break;

                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 12:
                        return;

                    case 9:
                        // processLine
                        processLine(data, present, delta);
                        break;

                    case 10:
                        // processRect
                        processRectangle(data, present, delta);
                        break;

                    case 11:
                        // processDeskSave
                        processDeskSave(data, present, delta);
                        break;

                    case 13:
                        // processMemBlt
                        processMemBlt(data, present, delta);
                        break;

                    case 14:
                        // processTriBlt
                        processTriBlt(data, present, delta);
                        break;

                    case 15:
                        // processMultiDestBlt
                        processMultiDestBlt(data, present, delta);
                        break;

                    case 0x10:
                        // processMultiPatBlt
                        processMultiPatBlt(data, present, delta);
                        break;

                    case 0x11:
                        // processMultiScreenBlt
                        processMultiScreenBlt(data, present, delta);
                        break;

                    case 0x12:
                        processMultiRectangle(data, present, delta);
                        break;

                    case 0x16:
                        // processPolyLine
                        processPolyLine(data, present, delta);
                        break;

                    case 0x1b:
                        // processGlyphIndex
                        processGlyphIndex(data, present, delta);
                        break;

                    default:
                        // processOrders unknown type
                        return;
                }

                if ((flags & 4) != 0)
                {
                    Options.BoundsLeft = 0;
                    Options.BoundsRight = Options.width - 1;
                    Options.BoundsTop = 0;
                    Options.BoundsBottom = Options.height - 1;
                }
            }

            if (data.Position != next_packet)
            {
                throw new RDFatalException("Bad order packet!");
            }
        }

        private static void processPatBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                PatBltOrder.X = setCoordinate(data, PatBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                PatBltOrder.Y = setCoordinate(data, PatBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                PatBltOrder.CX = setCoordinate(data, PatBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                PatBltOrder.CY = setCoordinate(data, PatBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                PatBltOrder.Opcode = ROP2_P(data.ReadByte());
            }

            if ((present & 0x20) != 0)
            {
                PatBltOrder.BackgroundColor = setColor(data);
            }

            if ((present & 0x40) != 0)
            {
                PatBltOrder.ForegroundColor = setColor(data);
            }

            parseBrush(data, PatBltOrder.Brush, present >> 7);

            PatBltOrder.drawPatBltOrder();
        }

        private static void processPolyLine(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                PolylineOrder.X = setCoordinate(data, PolylineOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                PolylineOrder.Y = setCoordinate(data, PolylineOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                PolylineOrder.Opcode = data.ReadByte();
            }

            if ((present & 8) != 0)
            {
                data.ReadLittleEndian16();
            }

            if ((present & 0x10) != 0)
            {
                PolylineOrder.PenColor = setColor(data);
            }

            if ((present & 0x20) != 0)
            {
                PolylineOrder.Lines = data.ReadByte();
            }

            if ((present & 0x40) != 0)
            {
                int num = data.ReadByte();
                PolylineOrder.DataSize = num;
                byte[] buffer = new byte[num];
                data.Read(buffer, 0, buffer.Length);
                PolylineOrder.Data = buffer;
            }

            PolylineOrder.drawPolyLineOrder();
        }

        private static void processRectangle(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                RectangleOrder.X = setCoordinate(data, RectangleOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                RectangleOrder.Y = setCoordinate(data, RectangleOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                RectangleOrder.CX = setCoordinate(data, RectangleOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                RectangleOrder.CY = setCoordinate(data, RectangleOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                RectangleOrder.ColourR = data.ReadByte();
            }

            if ((present & 0x20) != 0)
            {
                RectangleOrder.ColourG = data.ReadByte();
            }

            if ((present & 0x40) != 0)
            {
                RectangleOrder.ColourB = data.ReadByte();
            }

            RectangleOrder.drawRectangleOrder();
        }

        private static void processScreenBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                ScreenBltOrder.X = setCoordinate(data, ScreenBltOrder.X, delta);
            }

            if ((present & 2) != 0)
            {
                ScreenBltOrder.Y = setCoordinate(data, ScreenBltOrder.Y, delta);
            }

            if ((present & 4) != 0)
            {
                ScreenBltOrder.CX = setCoordinate(data, ScreenBltOrder.CX, delta);
            }

            if ((present & 8) != 0)
            {
                ScreenBltOrder.CY = setCoordinate(data, ScreenBltOrder.CY, delta);
            }

            if ((present & 0x10) != 0)
            {
                ScreenBltOrder.Opcode = ROP2_S(data.ReadByte());
            }

            if ((present & 0x20) != 0)
            {
                ScreenBltOrder.SrcX = setCoordinate(data, ScreenBltOrder.SrcX, delta);
            }

            if ((present & 0x40) != 0)
            {
                ScreenBltOrder.SrcY = setCoordinate(data, ScreenBltOrder.SrcY, delta);
            }

            ScreenBltOrder.drawScreenBltOrder();
        }

        private static void processSecondaryOrders(RdpPacket data)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            num = data.ReadLittleEndian16();
            int extraFlags = data.ReadLittleEndian16();
            num2 = data.ReadByte();
            num3 = (((int) data.Position) + num) + 7;

            switch (num2)
            {
                case 0:
                    // Raw BitmapCache Order
                    processBitmapCache(data, false, false);
                    goto Label_019B;

                case 1:
                    // ColourCache Order
                    processColorCache(data);
                    goto Label_019B;

                case 2:
                    // BitmapCache Order
                    processBitmapCache(data, true, (extraFlags & 0x400) != 0);
                    goto Label_019B;

                case 3:
                    // FontCache Order
                    processFontCache(data, extraFlags);
                    goto Label_019B;

                case 4:
                    // Raw BitmapCacheV2 Order
                    try
                    {
                        process_bmpcache2(data, extraFlags, false);
                        goto Label_019B;
                    }
                    catch (IOException exception)
                    {
                        throw new RDFatalException(exception.Message);
                    }
                    break;

                case 5:
                    break;

                case 7:
                    goto Label_0132;

                case 8:
                    goto Label_015C;

                default:
                    goto Label_0185;
            }

            // BitmapCacheV2 Order
            try
            {
                process_bmpcache2(data, extraFlags, true);
                goto Label_019B;
            }
            catch (IOException exception2)
            {
                throw new RDFatalException(exception2.Message);
            }

        Label_0132:
            try
            {
                // BrushCache Order
                process_bmpcache2(data, extraFlags, true);
                goto Label_019B;
            }
            catch (IOException exception3)
            {
                throw new RDFatalException(exception3.Message);
            }

        Label_015C:
            try
            {
                // BitmapCacheV3 Order
                process_bmpcache3(data, extraFlags);
                goto Label_019B;
            }
            catch (IOException exception4)
            {
                throw new RDFatalException(exception4.Message);
            }

        Label_0185:
            throw new RDFatalException("Unsupported secondary order: " + num2);

        Label_019B:
            if (data.Position > num3)
            {
                throw new Exception("Secondary Order overflow!");
            }

            data.Position = num3;
        }

        private static void processTriBlt(RdpPacket data, int present, bool delta)
        {
            if ((present & 1) != 0)
            {
                TriBltOrder.CacheID = data.ReadByte();
                TriBltOrder.ColorTable = data.ReadByte();
            }

            if ((present & 2) != 0)
            {
                TriBltOrder.X = setCoordinate(data, TriBltOrder.X, delta);
            }

            if ((present & 4) != 0)
            {
                TriBltOrder.Y = setCoordinate(data, TriBltOrder.Y, delta);
            }

            if ((present & 8) != 0)
            {
                TriBltOrder.CX = setCoordinate(data, TriBltOrder.CX, delta);
            }

            if ((present & 0x10) != 0)
            {
                TriBltOrder.CY = setCoordinate(data, TriBltOrder.CY, delta);
            }

            if ((present & 0x20) != 0)
            {
                TriBltOrder.Opcode = data.ReadByte();
            }

            if ((present & 0x40) != 0)
            {
                TriBltOrder.SrcX = setCoordinate(data, TriBltOrder.SrcX, delta);
            }

            if ((present & 0x80) != 0)
            {
                TriBltOrder.SrcY = setCoordinate(data, TriBltOrder.SrcY, delta);
            }

            if ((present & 0x100) != 0)
            {
                TriBltOrder.BackgroundColor = setColor(data);
            }

            if ((present & 0x200) != 0)
            {
                TriBltOrder.ForegroundColor = setColor(data);
            }

            parseBrush(data, TriBltOrder.Brush, present >> 10);

            if ((present & 0x8000) != 0)
            {
                TriBltOrder.CacheIDX = data.ReadLittleEndian16();
            }

            if ((present & 0x10000) != 0)
            {
                TriBltOrder.Unknown = data.ReadLittleEndian16();
            }

            TriBltOrder.drawTriBltOrder();
        }

        private static Rectangle[] readEncodedDeltaRects(RdpPacket data, int DeltaEntries)
        {
            Rectangle[] rectangleArray = new Rectangle[DeltaEntries];
            int num = data.ReadLittleEndian16();
            long position = data.Position;
            BitStream stream = new BitStream(true);
            stream.Write(data, (DeltaEntries + 1) >> 1);
            Rectangle rectangle = new Rectangle(0, 0, 0, 0);

            for (int i = 0; i < DeltaEntries; i++)
            {
                if (stream.ReadNextBit() == 0)
                {
                    rectangle.X += data.ReadEncodedSignedExtended16();
                }
                if (stream.ReadNextBit() == 0)
                {
                    rectangle.Y += data.ReadEncodedSignedExtended16();
                }
                if (stream.ReadNextBit() == 0)
                {
                    rectangle.Width = data.ReadEncodedSignedExtended16();
                }
                if (stream.ReadNextBit() == 0)
                {
                    rectangle.Height = data.ReadEncodedSignedExtended16();
                }
                rectangleArray[i] = rectangle.Clone();
            }

            long num5 = data.Position - position;

            if (num5 != num)
            {
                throw new Exception("Invalid Encoded Delta Rects!");
            }

            return rectangleArray;
        }

        internal static void Reset()
        {
            m_OrderType = 1;
        }

        private static int ROP2_P(int rop3)
        {
            return ((rop3 & 3) | ((rop3 & 0x30) >> 2));
        }

        private static int ROP2_S(int rop3)
        {
            return (rop3 & 15);
        }

        private static int setColor(RdpPacket data)
        {
            int num = 0;
            int num2 = 0;
            num = data.ReadByte();
            num2 = data.ReadByte();
            num |= num2 << 8;
            num2 = data.ReadByte();

            return (num | (num2 << 0x10));
        }

        private static int setCoordinate(RdpPacket data, int coordinate, bool delta)
        {
            sbyte num = 0;

            if (delta)
            {
                num = (sbyte) data.ReadByte();
                coordinate += num;
                return coordinate;
            }

            coordinate = data.ReadLittleEndian16();

            return coordinate;
        }

    }
}