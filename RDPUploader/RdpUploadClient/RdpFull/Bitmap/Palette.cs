using System;

namespace RdpUploadClient
{
    internal class Palette
    {
        internal int m_BitSize;
        internal byte[] m_Blue;
        internal int m_Count;
        internal static Palette m_Global = new Palette();
        internal byte[] m_Green;
        internal byte[] m_Red;

        public Palette()
        {
        }

        public Palette(int bitSize, int Count, byte[] r, byte[] g, byte[] b)
        {
            this.m_BitSize = bitSize;
            this.m_Count = Count;
            this.m_Red = r;
            this.m_Green = g;
            this.m_Blue = b;
        }

        internal static void processPalette(RdpPacket data)
        {
            int num = 0;
            byte[] buffer = null;
            int index = 0;
            data.Position += 2L;
            num = data.ReadLittleEndian16();
            data.Position += 2L;
            buffer = new byte[num * 3];
            m_Global.m_Red = new byte[num];
            m_Global.m_Green = new byte[num];
            m_Global.m_Blue = new byte[num];
            data.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < num; i++)
            {
                m_Global.m_Red[i] = buffer[index];
                m_Global.m_Green[i] = buffer[index + 1];
                m_Global.m_Blue[i] = buffer[index + 2];
                index += 3;
            }
        }

    }
}