using System;

namespace RdpUploadClient
{
    internal class Glyph
    {
        internal int BaseLine;
        internal int Character;
        internal int Font;
        internal byte[] FontData;
        internal int Height;
        internal int Offset;
        internal int Width;

        public Glyph(int font, int character, int offset, int baseline, int width, int height, byte[] fontdata)
        {
            this.Font = font;
            this.Character = character;
            this.Offset = offset;
            this.BaseLine = baseline;
            this.Width = width;
            this.Height = height;
            this.FontData = fontdata;
        }

        internal static void Reset()
        {
            Cache.Reset(false);
        }

    }
}