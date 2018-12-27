using System;

namespace RdpUploadClient
{
    internal class PolylineOrder
    {
        internal static byte[] Data;
        internal static int DataSize;
        internal static int Lines;
        internal static int Opcode;
        internal static int PenColor;
        internal static int X;
        internal static int Y;

        internal static void drawPolyLineOrder()
        {
            Options.Enter();
            try
            {
                int x = X;
                int y = Y;
                int penColor = PenColor;
                int dataSize = DataSize;
                byte[] data = Data;
                int lines = Lines;
                int offset = ((lines - 1) / 4) + 1;
                int num7 = 0;
                int num8 = 0;
                int opcode = Opcode - 1;
                for (int i = 0; (i < lines) && (offset < dataSize); i++)
                {
                    int num11 = x;
                    int num12 = y;
                    if ((i % 4) == 0)
                    {
                        num7 = data[num8++];
                    }
                    if ((num7 & 0xc0) == 0)
                    {
                        num7 |= 0xc0;
                    }
                    if ((num7 & 0x40) != 0)
                    {
                        x += parse_delta(data, ref offset);
                    }
                    if ((num7 & 0x80) != 0)
                    {
                        y += parse_delta(data, ref offset);
                    }
                    ChangedRect.Invalidate(num11, num12, x - num11, y - num12);
                    LineOrder.drawLine(num11, num12, x, y, penColor, opcode);
                    num7 = num7 << 2;
                }
            }
            finally
            {
                Options.Exit();
            }
        }

        internal static int parse_delta(byte[] buffer, ref int offset)
        {
            int num = buffer[offset++] & 0xff;
            int num2 = num & 0x80;
            if ((num & 0x40) != 0)
            {
                num |= -64;
            }
            else
            {
                num &= 0x3f;
            }
            if (num2 != 0)
            {
                num = (num << 8) | (buffer[offset++] & 0xff);
            }
            return num;
        }

        internal static void Reset()
        {
            X = 0;
            Y = 0;
            Opcode = 0;
            PenColor = 0;
            Lines = 0;
            DataSize = 0;
            Data = null;
        }

    }
}