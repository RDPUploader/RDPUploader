using System;

namespace RdpUploadClient
{
    internal class BrushOrder
    {
        internal byte[] Pattern = new byte[8];
        internal int Style;
        internal int XOrigin;
        internal int YOrigin;

        public void Reset()
        {
            this.XOrigin = 0;
            this.YOrigin = 0;
            this.Style = 0;
        }

    }
}