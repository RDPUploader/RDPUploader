using System;
using System.IO;
using System.Text;

namespace RdpUploadClient
{
    internal class RdpPacket : MemoryStream
    {
        public const byte DATA_TRANSFER = 240;
        public const byte DISCONNECT_REQUEST = 0x80;
        public const byte EOT = 0x80;
        public const byte FAST_PATH_OUTPUT = 0xff;
        public const byte FAST_PATH_OUTPUT_ENCRYPTED = 0xfe;
        private byte[] m_Buffer;
        private NetworkSocket m_Socket;

        public RdpPacket()
        {
        }

        public RdpPacket(NetworkSocket Socket, byte[] buffer)
        {
            this.m_Socket = Socket;
            this.m_Buffer = buffer;
        }

        public void Append(RdpPacket value)
        {
            byte[] buffer = new byte[value.Length - value.Position];
            value.Read(buffer, 0, buffer.Length);
            this.Write(buffer, 0, buffer.Length);
        }

        public void copyToByteArray(RdpPacket packet)
        {
            byte[] buffer = new byte[packet.Length];
            packet.Position = 0L;
            packet.Read(buffer, 0, (int) packet.Length);
            this.Write(buffer, 0, buffer.Length);
        }

        public void InsertByte(byte value)
        {
            long position = this.Position;
            this.Seek(0L, SeekOrigin.End);
            this.WriteByte(0);
            byte[] sourceArray = this.GetBuffer();
            Array.Copy(sourceArray, (int) position, sourceArray, ((int) position) + 1, (int) ((sourceArray.Length - position) - 1L));
            this.Position = position;
            this.WriteByte(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if ((this.Position + count) > this.Length)
            {
                this.ReadFromSocket(count);
            }

            return base.Read(buffer, offset, count);
        }

        public int ReadLittleEndian16()
        {
            byte[] buffer = new byte[2];
            this.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public int ReadLittleEndian32()
        {
            byte[] buffer = new byte[4];
            this.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public uint ReadLittleEndianU32()
        {
            byte[] buffer = new byte[4];
            this.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public int ReadBigEndian16()
        {
            byte[] buffer = new byte[2];
            this.Read(buffer, 0, 2);
            return BitConverter.ToInt16(this.Reverse(buffer), 0);
        }

        public int ReadBigEndian32()
        {
            byte[] buffer = new byte[4];
            this.Read(buffer, 0, 4);
            return BitConverter.ToInt32(this.Reverse(buffer), 0);
        }

        public override int ReadByte()
        {
            if (this.Position >= this.Length)
            {
                this.ReadFromSocket(1);
            }
            return base.ReadByte();
        }

        public int ReadEncoded32()
        {
            int num = this.ReadByte();
            int num2 = (num & 0xc0) >> 6;
            int num3 = num2 * 8;
            int num4 = (num & 0x3f) << num3;

            while (num2-- > 0)
            {
                num3 -= 8;
                num4 |= this.ReadByte() << num3;
            }

            return num4;
        }

        public int ReadEncodedSigned16()
        {
            int num2;
            int num = this.ReadByte();
            if ((num & 0x80) != 0)
            {
                num2 = ((num & -193) << 8) | this.ReadByte();
            }
            else
            {
                num2 = num & -193;
            }
            if ((num & 0x40) != 0)
            {
                return -num2;
            }
            return num2;
        }

        public int ReadEncodedSignedExtended16()
        {
            int num2;
            int num = this.ReadByte();
            if ((num & 0x40) != 0)
            {
                num2 = num | -64;
            }
            else
            {
                num2 = num & 0x3f;
            }
            if ((num & 0x80) != 0)
            {
                num2 = (num2 << 8) | this.ReadByte();
            }
            return num2;
        }

        public int ReadEncodedUnsigned16()
        {
            int num = this.ReadByte();
            if ((num & 0x80) != 0)
            {
                return (((num & -129) << 8) | this.ReadByte());
            }
            return (num & -129);
        }

        private void ReadFromSocket(int lengthRequired)
        {
            if (this.m_Socket == null)
            {
                throw new IOException("RdpPacket - Overrun!");
            }
            int count = this.m_Socket.Receive(this.m_Buffer, this.m_Buffer.Length);
            if (count <= 0)
            {
                throw new IOException("RdpPacket - Overrun!");
            }
            lengthRequired -= count;
            if (lengthRequired > 0)
            {
                throw new IOException("RdpPacket - Overrun!");
            }
            long position = this.Position;
            this.Write(this.m_Buffer, 0, count);
            this.Position = position;
        }

        public void ReadPad(int AlignBytes)
        {
            int num = ((int) this.Position) % AlignBytes;
            if (num != 0)
            {
                int num2 = AlignBytes - num;
                while (num2-- > 0)
                {
                    this.ReadByte();
                }
            }
        }

        public string ReadString(int Length)
        {
            byte[] buffer = new byte[Length];
            this.Read(buffer, 0, Length);
            return ASCIIEncoding.GetString(buffer, 0, Length);
        }

        public string ReadUnicodeString()
        {
            StringBuilder builder = new StringBuilder();
            int num = this.ReadLittleEndian32() - 2;
            for (int i = 0; i < num; i += 2)
            {
                int num3 = this.ReadLittleEndian16();
                builder.Append((char) num3);
            }
            this.ReadLittleEndian16();
            return builder.ToString();
        }

        private byte[] Reverse(byte[] data)
        {
            Array.Reverse(data);
            return data;
        }

        public void WriteBigEndian16(short Value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(Value)), 0, 2);
        }

        public void WriteBigEndian16(ushort Value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(Value)), 0, 2);
        }

        public void WriteBigEndian32(int value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(value)), 0, 4);
        }

        public void WriteBigEndian32(uint value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(value)), 0, 4);
        }

        public void WriteBigEndian64(long Value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(Value)), 0, 8);
        }

        public void WriteBigEndianU64(ulong Value)
        {
            base.Write(this.Reverse(BitConverter.GetBytes(Value)), 0, 8);
        }

        public void WriteByte(byte value)
        {
            base.WriteByte(value);
        }

        public void WriteEncodedUnsigned16(ushort Value)
        {
            if (Value < 0x80)
            {
                this.WriteByte((byte) (Value & 0x7f));
            }
            else
            {
                this.WriteByte((byte) (((Value >> 8) & 0x7f) | 0x80));
                this.WriteByte((byte) Value);
            }
        }

        public void WriteLittleEndian16(short Value)
        {
            base.Write(BitConverter.GetBytes(Value), 0, 2);
        }

        public void WriteLittleEndian16(ushort Value)
        {
            base.Write(BitConverter.GetBytes(Value), 0, 2);
        }

        public void WriteLittleEndian32(int value)
        {
            base.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void WriteLittleEndian32(uint value)
        {
            base.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void WriteLittleEndian64(long Value)
        {
            base.Write(BitConverter.GetBytes(Value), 0, 8);
        }

        public void WriteLittleEndianU32(uint value)
        {
            base.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void WriteLittleEndianU64(ulong Value)
        {
            base.Write(BitConverter.GetBytes(Value), 0, 8);
        }

        public void WritePad(int AlignBytes)
        {
            int num = ((int) this.Position) % AlignBytes;

            if (num != 0)
            {
                int num2 = AlignBytes - num;

                while (num2-- > 0)
                {
                    this.WriteByte(0);
                }
            }
        }

        public void WritePadding(int bytes)
        {
            for (int i = 0; i < bytes; i++)
            {
                this.WriteByte(0);
            }
        }

        public void WriteString(string sString, bool bQuiet)
        {
            byte[] bytes = ASCIIEncoding.GetBytes(sString, bQuiet);
            this.Write(bytes, 0, bytes.Length);
        }

        public void WriteUnicodeString(string sString)
        {
            if (sString.Length != 0)
            {
                char[] chArray = sString.ToCharArray();

                for (int i = 0; i < chArray.Length; i++)
                {
                    this.WriteLittleEndian16((short)chArray[i]);
                }

                this.WriteLittleEndian16((short)0);
            }
            else
            {
                this.WriteLittleEndian16((short)0);
            }
        }

    }
}