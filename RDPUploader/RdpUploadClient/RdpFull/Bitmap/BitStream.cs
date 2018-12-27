using System;
using System.IO;

namespace RdpUploadClient
{
    internal class BitStream : MemoryStream
    {
        private bool m_bHighOrder;
        private byte? m_Current;
        private int m_Index;

        public BitStream(bool bHighOrder)
        {
            this.m_bHighOrder = bHighOrder;
        }

        public byte ReadNextBit()
        {
            if (!this.m_Current.HasValue)
            {
                this.m_Current = new byte?(this.ReadNextByte());
                this.m_Index = this.m_bHighOrder ? 7 : 0;
            }
            byte num = (byte)((this.m_Current.Value >> (this.m_Index & 0x1f)) & 1);
            if (this.m_bHighOrder)
            {
                this.m_Index--;
                if (this.m_Index < 0)
                {
                    this.m_Current = null;
                }
                return num;
            }
            this.m_Index++;
            if (this.m_Index == 8)
            {
                this.m_Current = null;
            }
            return num;
        }

        public byte ReadNextByte()
        {
            return (byte) this.ReadByte();
        }

        public void Write(RdpPacket packet, int Length)
        {
            byte[] buffer = new byte[Length];
            packet.Read(buffer, 0, buffer.Length);
            this.Write(buffer, 0, Length);
            this.Position = 0L;
        }

        public byte WriteNextBit(byte value)
        {
            if (!this.m_Current.HasValue)
            {
                this.m_Current = 0;
                this.m_Index = this.m_bHighOrder ? 7 : 0;
            }
            byte num = (byte)(this.m_Current.Value | ((value & 1) << this.m_Index));
            this.m_Current = new byte?(num);
            if (this.m_bHighOrder)
            {
                this.m_Index--;
                if (this.m_Index < 0)
                {
                    this.WriteByte(this.m_Current.Value);
                    this.m_Current = null;
                }
                return value;
            }
            this.m_Index++;
            if (this.m_Index == 8)
            {
                this.WriteByte(this.m_Current.Value);
                this.m_Current = null;
            }
            return value;
        }

    }
}