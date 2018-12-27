using System;
using System.Collections.Generic;
using System.IO;

namespace RdpUploadClient
{
    internal class Cache
    {
        public const int BITMAP_CACHE0_MAX = 120;
        public const int BITMAP_CACHE0_MAXCELL = 0x300;
        public const int BITMAP_CACHE1_MAX = 120;
        public const int BITMAP_CACHE1_MAXCELL = 0xc00;
        public const int BITMAP_CACHE2_MAX = 0x400;
        public const int BITMAP_CACHE2_MAXCELL = 0x2000;
        internal static RdpBitmap[][] m_BitmapCache = new RdpBitmap[3][];
        public static int m_BitmapCaches = 0;
        private static Glyph[,] m_FontCache = new Glyph[10, 0x100];
        private static int[] m_HighDeskCache = new int[0xe1000];
        private static Palette[] m_PaletteCache = new Palette[6];
        private static List<byte[]> m_TextCache = new List<byte[]>(0x100);
        private const int RDPCACHE_COLOURMAPSIZE = 6;

        private static void DumpBitmapCache(int cache, BinaryWriter writer)
        {
            int num = 0;
            foreach (RdpBitmap bitmap in m_BitmapCache[cache])
            {
                if ((bitmap != null) && bitmap.persists)
                {
                    num++;
                }
            }
            writer.Write(num);
            foreach (RdpBitmap bitmap2 in m_BitmapCache[cache])
            {
                if ((bitmap2 != null) && bitmap2.persists)
                {
                    bitmap2.Serialise(writer);
                }
            }
        }

        internal static Palette get_colourmap(int cache_id)
        {
            Palette palette = null;
            if (cache_id < m_PaletteCache.Length)
            {
                palette = m_PaletteCache[cache_id];
                if (palette != null)
                {
                    return palette;
                }
            }
            return null;
        }

        internal static RdpBitmap getBitmap(int cache_id, int cache_idx)
        {
            RdpBitmap bitmap = null;
            if ((cache_id < m_BitmapCache.Length) && (cache_idx < m_BitmapCache[cache_id].Length))
            {
                bitmap = m_BitmapCache[cache_id][cache_idx];
                if (bitmap != null)
                {
                    return bitmap;
                }
            }
            return null;
        }

        public static List<ulong> GetBitmapCache(int offset, int max, out int numCache0, out int numCache1, out int numCache2, out int numCache3, out int numCache4, out bool bMoreKeys)
        {
            int num2;
            int num3;
            int num4;
            numCache4 = num2 = 0;
            numCache3 = num3 = num2;
            numCache2 = num4 = num3;
            numCache0 = numCache1 = num4;
            int index = 0;
            bMoreKeys = false;
            List<ulong> list = new List<ulong>();
            while (index < m_BitmapCache.Length)
            {
                foreach (RdpBitmap bitmap in m_BitmapCache[index])
                {
                    if ((bitmap == null) || !bitmap.persists)
                    {
                        continue;
                    }
                    if (offset == 0)
                    {
                        if (list.Count < max)
                        {
                            switch (index)
                            {
                                case 0:
                                    numCache0++;
                                    break;

                                case 1:
                                    numCache1++;
                                    break;

                                case 2:
                                    numCache2++;
                                    break;

                                case 3:
                                    numCache3++;
                                    break;

                                case 4:
                                    numCache4++;
                                    break;
                            }
                            list.Add(bitmap.persist_key);
                            continue;
                        }
                        bMoreKeys = true;
                        return list;
                    }
                    offset--;
                }
                index++;
            }
            return list;
        }

        internal static int[] getDesktopInt(int offset, int cx, int cy)
        {
            int num = cx * cy;
            int destinationIndex = 0;
            int[] destinationArray = new int[num];
            if (offset > m_HighDeskCache.Length)
            {
                offset = 0;
            }
            if ((offset + num) > m_HighDeskCache.Length)
            {
                throw new RDFatalException("Could not get Bitmap");
            }
            for (int i = 0; i < cy; i++)
            {
                Array.Copy(m_HighDeskCache, offset, destinationArray, destinationIndex, cx);
                offset += cx;
                destinationIndex += cx;
            }
            return destinationArray;
        }

        internal static Glyph getFont(int font, int character)
        {
            return m_FontCache[font, character];
        }

        internal static byte[] getText(int A_0)
        {
            return m_TextCache[A_0];
        }

        internal static void put_colourmap(int cache_id, Palette map)
        {
            if (cache_id >= m_PaletteCache.Length)
            {
                throw new RDFatalException("Could not put Palette with cache_id=" + cache_id);
            }
            m_PaletteCache[cache_id] = map;
        }

        internal static void putBitmap(int cache_id, int cache_idx, RdpBitmap bitmap, int stamp)
        {
            try
            {
                if ((cache_id >= m_BitmapCache.Length) || (cache_idx >= m_BitmapCache[cache_id].Length))
                {
                    throw new RDFatalException(string.Concat(new object[] { "Bitmap cache out of range (", cache_id, ",", cache_idx, ")!" }));
                }
                m_BitmapCache[cache_id][cache_idx] = bitmap;
                if (bitmap == null)
                {
                    throw new Exception("Caching null bitmap!");
                }
                // Bitmap cache id: cache_id
            }
            catch { }
        }

        internal static void putDesktop(int offset, int cx, int cy, int[] data)
        {
            int num = cx * cy;
            int sourceIndex = 0;
            if (offset > m_HighDeskCache.Length)
            {
                offset = 0;
            }
            if ((offset + num) > m_HighDeskCache.Length)
            {
                throw new RDFatalException("Could not put Desktop");
            }
            for (int i = 0; i < cy; i++)
            {
                Array.Copy(data, sourceIndex, m_HighDeskCache, offset, cx);
                offset += cx;
                sourceIndex += cx;
            }
        }

        internal static void putFont(Glyph glyph)
        {
            m_FontCache[glyph.Font, glyph.Character] = glyph;
        }

        internal static void putText(int A_0, byte[] A_1)
        {
            m_TextCache[A_0] = A_1;
        }

        internal static void Reset(bool bResetBitmapCache)
        {
            m_BitmapCaches = 0;
            m_TextCache.Clear();
            for (int i = 0; i < 0x100; i++)
            {
                m_TextCache.Add(null);
            }
            if (bResetBitmapCache)
            {
                m_BitmapCache[0] = new RdpBitmap[120];
                m_BitmapCache[1] = new RdpBitmap[120];
                m_BitmapCache[2] = new RdpBitmap[0x400];
            }
        }

        public static void TotalBitmapCache(out int numCache0, out int numCache1, out int numCache2, out int numCache3, out int numCache4)
        {
            int num2;
            int num3;
            int num4;
            numCache4 = num2 = 0;
            numCache3 = num3 = num2;
            numCache2 = num4 = num3;
            numCache0 = numCache1 = num4;
            for (int i = 0; i < m_BitmapCache.Length; i++)
            {
                foreach (RdpBitmap bitmap in m_BitmapCache[i])
                {
                    if ((bitmap != null) && bitmap.persists)
                    {
                        switch (i)
                        {
                            case 0:
                                numCache0++;
                                break;

                            case 1:
                                numCache1++;
                                break;

                            case 2:
                                numCache2++;
                                break;

                            case 3:
                                numCache3++;
                                break;

                            case 4:
                                numCache4++;
                                break;
                        }
                    }
                }
            }
        }

    }
}