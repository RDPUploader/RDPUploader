using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;

namespace RdpUploadClient
{
    internal class RdpCanvas
    {
        private bool m_bCached;
        private bool m_bChanged;
        private Bitmap m_Bitmap;
        private int[] m_BitmapCache;
        private int m_BlackOpaque = BitConverter.ToInt32(BitConverter.GetBytes((uint) 0xff000000), 0);
        private int m_Height;
        private object m_LockObj = new object();
        private int m_Stride;
        private int[] m_StrideTable;
        private int m_Width;

        public RdpCanvas(int Width, int Height)
        {
            this.m_Width = Width;
            this.m_Height = Height;
            this.m_Bitmap = new Bitmap(Width, Height);
        }

        private void CachePixels()
        {
            this.m_bChanged = true;
            this.m_bCached = true;

            lock (this.m_LockObj)
            {
                if (this.m_BitmapCache == null)
                {
                    this.m_Stride = this.Width;
                    this.m_BitmapCache = new int[this.Width * this.Height];
                    this.m_StrideTable = new int[this.m_Stride];

                    for (int i = 0; i < this.m_BitmapCache.Length; i++)
                    {
                        this.m_BitmapCache[i] = this.m_BlackOpaque;
                    }

                    for (int j = 0; j < this.m_Stride; j++)
                    {
                        this.m_StrideTable[j] = j * this.m_Stride;
                    }
                }
            }
        }

        public void CopyPixels(int SrcX, int SrcY, int DstX, int DstY, int Width, int Height)
        {
            this.CachePixels();
            int[] destinationArray = new int[Width * Height];
            int num = SrcX;
            int length = Width;

            for (int i = 0; i < Height; i++)
            {
                Array.Copy(this.m_BitmapCache, this.m_StrideTable[SrcY + i] + num, destinationArray, i * length, length);
            }

            int num4 = DstX;

            for (int j = 0; j < Height; j++)
            {
                Array.Copy(destinationArray, j * length, this.m_BitmapCache, this.m_StrideTable[DstY + j] + num4, length);
            }
        }

        public int GetPixel(int x, int y)
        {
            this.CachePixels();
            return this.m_BitmapCache[this.m_StrideTable[y] + x];
        }

        public int[] GetPixels(int x, int y, int cx, int cy)
        {
            int[] numArray = new int[cx * cy];
            int index = 0;

            for (int i = 0; i < cy; i++)
            {
                int num3 = 0;

                while (num3 < cx)
                {
                    numArray[index] = this.GetPixel(x + num3, y + i);
                    num3++;
                    index++;
                }
            }

            return numArray;
        }

        public Bitmap Invalidate()
        {
            if (this.m_bChanged)
            {
                lock (this.m_LockObj)
                {
                    this.m_Bitmap = CreateBitmap24bppRgb(ConvertIntArrayToByteArray(this.m_BitmapCache), this.m_Width, this.m_Height);
                    this.m_bCached = false;
                }
            }

            return this.m_Bitmap;
        }

        internal static Bitmap CreateBitmap24bppRgb(byte[] data, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bmpData = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        internal static byte[] ConvertIntArrayToByteArray(int[] array)
        {
            byte[] newarray = new byte[array.Length * 3];

            for (int i = 0; i < array.Length; i++)
            {
                newarray[i * 3] = (byte)array[i];
                newarray[i * 3 + 1] = (byte)(array[i] >> 8);
                newarray[i * 3 + 2] = (byte)(array[i] >> 16);
            }

            return newarray;
        }

        public void SetPixel(int x, int y, int Color)
        {
            this.CachePixels();
            this.m_BitmapCache[this.m_StrideTable[y] + x] = Color | this.m_BlackOpaque;
        }

        public void SetPixels(int x, int y, int cx, int cy, int Color)
        {
            Color |= this.m_BlackOpaque;

            for (int i = x; i < (x + cx); i++)
            {
                for (int j = y; j < (y + cy); j++)
                {
                    this.SetPixel(i, j, Color);
                }
            }
        }

        public void SetPixels(int x, int y, int cx, int cy, int[] src, int srcx, int srcy, int srcpitch)
        {
            for (int i = 0; i < cy; i++)
            {
                int index = ((srcy + i) * srcpitch) + srcx;
                int num3 = y + i;
                int num4 = x;

                while (num4 < (x + cx))
                {
                    this.m_BitmapCache[this.m_StrideTable[num3] + num4] = src[index] | this.m_BlackOpaque;
                    num4++;
                    index++;
                }
            }
        }

        public void SetPixels(int x, int y, int cx, int cy, uint[] src, int srcx, int srcy, int srcpitch)
        {
            for (int i = 0; i < cy; i++)
            {
                int index = ((srcy + i) * srcpitch) + srcx;
                int num3 = y + i;
                int num4 = x;

                while (num4 < (x + cx))
                {
                    this.m_BitmapCache[this.m_StrideTable[num3] + num4] = ((int)src[index]) | this.m_BlackOpaque;
                    num4++;
                    index++;
                }
            }
        }

        public int Height
        {
            get
            {
                return this.m_Height;
            }
        }

        public int Width
        {
            get
            {
                return this.m_Width;
            }
        }

    }
}