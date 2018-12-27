using System;
using System.IO;

namespace RdpUploadClient
{
    internal class RdpBitmap
    {
        public int _a;
        private static int _g;
        private int Bpp;
        private byte[] bytedata;
        private int height;
        private uint[] highdata;
        public ulong persist_key;
        public bool persists;
        private int width;
        private int x;
        private int y;

        public RdpBitmap(BinaryReader reader)
        {
            this.Deserialise(reader);
            this.x = this.y = 0;
        }

        public RdpBitmap(byte[] data, int width, int height, int Bpp, int x, int y)
        {
            this.bytedata = data;
            this.width = width;
            this.height = height;
            this.Bpp = Bpp;
            this.x = x;
            this.y = y;
        }

        public RdpBitmap(uint[] data, int width, int height, int Bpp, int x, int y)
        {
            this.highdata = data;
            this.width = width;
            this.height = height;
            this.Bpp = Bpp;
            this.x = x;
            this.y = y;
        }

        public static int convert15to24(int colour15)
        {
            int num = (colour15 >> 7) & 0xf8;
            int num2 = (colour15 >> 2) & 0xf8;
            int num3 = (colour15 << 3) & 0xff;
            num |= num >> 5;
            num2 |= num2 >> 5;
            num3 |= num3 >> 5;
            return ((((num << 0x10) | (num2 << 8)) | num3) | -16777216);
        }

        private static uint[] convert8bitImage(byte[] bitmap, int colorTableIndex)
        {
            uint[] numArray = new uint[bitmap.Length];
            Palette pal = Cache.get_colourmap(colorTableIndex);
            if (pal == null)
            {
                pal = Palette.m_Global;
            }
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = (uint) convertFrom8bit(bitmap[i] & 0xff, pal);
            }
            return numArray;
        }

        public static int convertColor(int color)
        {
            if (Options.server_bpp == 0x10)
            {
                return convertFrom16bit(color);
            }
            return convertFrom8bit(color);
        }

        public static int convertColor(int color, int bpp)
        {
            switch (bpp)
            {
                case 8:
                    return convertFrom8bit(color);

                case 0x10:
                    return convertFrom16bit(color);

                case 0x18:
                    return color;
            }
            throw new RDFatalException("Unsupported bpp " + bpp);
        }

        public static int convertColorBpp(int color, int Bpp)
        {
            return convertColor(color, Bpp * 8);
        }

        public static int convertFrom16bit(int colour16)
        {
            if (Options.client_bpp == 0x10)
            {
                return colour16;
            }
            int num = (colour16 >> 8) & 0xf8;
            int num2 = (colour16 >> 3) & 0xfc;
            int num3 = (colour16 << 3) & 0xff;
            num |= num >> 5;
            num2 |= num2 >> 6;
            num3 |= num3 >> 5;
            return ((((num << 0x10) | (num2 << 8)) | num3) | -16777216);
        }

        public static int convertFrom8bit(int index)
        {
            return convertFrom8bit(index, Palette.m_Global);
        }

        internal static int convertFrom8bit(int index, Palette pal)
        {
            if (Options.client_bpp == 0x10)
            {
                return (((-16777216 | ((pal.m_Red[index] >> 3) << 11)) | ((pal.m_Green[index] >> 2) << 5)) | (pal.m_Blue[index] >> 3));
            }
            return (((-16777216 | (pal.m_Red[index] << 0x10)) | (pal.m_Green[index] << 8)) | pal.m_Blue[index]);
        }

        public static uint[] convertImage(byte[] bitmap, int Bpp)
        {
            uint[] numArray = new uint[bitmap.Length / Bpp];
            for (int i = 0; i < numArray.Length; i++)
            {
                if (Bpp == 1)
                {
                    numArray[i] = (uint) (bitmap[i] & 0xff);
                }
                else if (Bpp == 2)
                {
                    numArray[i] = (uint) (((bitmap[(i * Bpp) + 1] & 0xff) << 8) | (bitmap[i * Bpp] & 0xff));
                }
                else if (Bpp == 3)
                {
                    numArray[i] = (uint) ((((bitmap[(i * Bpp) + 2] & 0xff) << 0x10) | ((bitmap[(i * Bpp) + 1] & 0xff) << 8)) | (bitmap[i * Bpp] & 0xff));
                }
                numArray[i] = (uint) convertColorBpp((int) numArray[i], Bpp);
            }
            return numArray;
        }

        private static uint[] convertImage(uint[] bitmap, int colorTableIndex)
        {
            uint[] numArray = new uint[bitmap.Length];
            Palette pal = Cache.get_colourmap(colorTableIndex);
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = (uint) convertFrom8bit(((int) bitmap[i]) & 0xff, pal);
            }
            return numArray;
        }

        private static uint cvalx(byte[] data, int offset, int Bpp)
        {
            if (Bpp == 2)
            {
                uint num = (uint) (data[offset] & 0xff);
                uint num2 = ((uint) ((data[offset + 1] & 0xff) << 8)) | num;
                return (uint) convertFrom16bit((int) num2);
            }
            return data[offset];
        }

        public static byte[] decompress(int width, int height, int size, RdpPacket data, int Bpp)
        {
            byte[] buffer = new byte[size];
            data.Read(buffer, 0, size);
            int startOffset = 0;
            int startlocation = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = size;
            int num6 = 0;
            int num7 = 0;
            int num8 = 0;
            int offset = width;
            int num10 = -1;
            int num11 = 0;
            int num12 = 0;
            int num13 = 0;
            int num14 = 0;
            byte num15 = 0;
            int num16 = 0;
            uint maxValue = uint.MaxValue;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            byte[] input = new byte[width * height];
            while (num3 < num5)
            {
                num11 = 0;
                num12 = buffer[num3++] & 0xff;
                num6 = num12 >> 4;
                switch (num6)
                {
                    case 12:
                    case 13:
                    case 14:
                        num6 -= 6;
                        num7 = num12 & 15;
                        num8 = 0x10;
                        goto Label_00F8;

                    case 15:
                        num6 = num12 & 15;
                        if (num6 >= 9)
                        {
                            break;
                        }
                        num7 = buffer[num3++] & 0xff;
                        num7 |= (buffer[num3++] & 0xff) << 8;
                        goto Label_00F5;

                    default:
                        num6 = num6 >> 1;
                        num7 = num12 & 0x1f;
                        num8 = 0x20;
                        goto Label_00F8;
                }
                num7 = (num6 < 11) ? 8 : 1;
            Label_00F5:
                num8 = 0;
            Label_00F8:
                if (num8 != 0)
                {
                    flag3 = (num6 == 2) || (num6 == 7);
                    if (num7 == 0)
                    {
                        if (flag3)
                        {
                            num7 = (buffer[num3++] & 0xff) + 1;
                        }
                        else
                        {
                            num7 = (buffer[num3++] & 0xff) + num8;
                        }
                    }
                    else if (flag3)
                    {
                        num7 = num7 << 3;
                    }
                }
                switch (num6)
                {
                    case 0:
                        if ((num10 == num6) && ((offset != width) || (startOffset != 0)))
                        {
                            flag = true;
                        }
                        break;

                    case 3:
                        num14 = buffer[num3++] & 0xff;
                        break;

                    case 6:
                    case 7:
                        maxValue = buffer[num3++];
                        num6 -= 5;
                        break;

                    case 8:
                        num13 = buffer[num3++] & 0xff;
                        num14 = buffer[num3++] & 0xff;
                        break;

                    case 9:
                        num16 = 3;
                        num6 = 2;
                        num11 = 3;
                        break;

                    case 10:
                        num16 = 5;
                        num6 = 2;
                        num11 = 5;
                        break;
                }
                num10 = num6;
                num15 = 0;
                while (num7 > 0)
                {
                    int num18;
                    int num19;
                    int num20;
                    int num21;
                    int num22;
                    int num23;
                    int num24;
                    int num25;
                    int num26;
                    int num27;
                    int num28;
                    if (offset >= width)
                    {
                        if (height <= 0)
                        {
                            throw new RDFatalException("Decompressing bitmap failed! Height = " + height.ToString());
                        }
                        offset = 0;
                        height--;
                        startOffset = startlocation;
                        startlocation = num4 + (height * width);
                    }
                    switch (num6)
                    {
                        case 0:
                            if (!flag)
                            {
                                goto Label_02CE;
                            }
                            if (startOffset != 0)
                            {
                                break;
                            }
                            input[startlocation + offset] = (byte) maxValue;
                            goto Label_02BF;

                        case 1:
                            if (startOffset == 0)
                            {
                                goto Label_03BC;
                            }
                            goto Label_0429;

                        case 2:
                            if (startOffset == 0)
                            {
                                goto Label_04C7;
                            }
                            goto Label_0596;

                        case 3:
                            goto Label_0634;

                        case 4:
                            goto Label_0696;

                        case 8:
                            goto Label_0714;

                        case 13:
                            goto Label_0790;

                        case 14:
                            goto Label_07E8;

                        default:
                            throw new RDFatalException("Incorrect decompress opcode " + num6.ToString());
                    }
                    input[startlocation + offset] = (byte) (input[startOffset + offset] ^ ((byte) maxValue));
                Label_02BF:
                    flag = false;
                    num7--;
                    offset++;
                Label_02CE:
                    if (startOffset == 0)
                    {
                        goto Label_02FA;
                    }
                    goto Label_035A;
                Label_02D6:
                    num28 = 0;
                    while (num28 < 8)
                    {
                        input[startlocation + offset] = 0;
                        num7--;
                        offset++;
                        num28++;
                    }
                Label_02FA:
                    if (((num7 & -8) != 0) && ((offset + 8) < width))
                    {
                        goto Label_02D6;
                    }
                    while ((num7 > 0) && (offset < width))
                    {
                        input[startlocation + offset] = 0;
                        num7--;
                        offset++;
                    }
                    continue;
                Label_0330:
                    num18 = 0;
                    while (num18 < 8)
                    {
                        input[startlocation + offset] = input[startOffset + offset];
                        num7--;
                        offset++;
                        num18++;
                    }
                Label_035A:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_0384;
                    }
                    goto Label_0330;
                Label_036A:
                    input[startlocation + offset] = input[startOffset + offset];
                    num7--;
                    offset++;
                Label_0384:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_036A;
                    }
                    continue;
                Label_0396:
                    num19 = 0;
                    while (num19 < 8)
                    {
                        input[startlocation + offset] = (byte) maxValue;
                        num7--;
                        offset++;
                        num19++;
                    }
                Label_03BC:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_03E2;
                    }
                    goto Label_0396;
                Label_03CC:
                    input[startlocation + offset] = (byte) maxValue;
                    num7--;
                    offset++;
                Label_03E2:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_03CC;
                    }
                    continue;
                Label_03F4:
                    num20 = 0;
                    while (num20 < 8)
                    {
                        setli(input, startlocation, offset, getli(input, startOffset, offset, 1) ^ maxValue, 1);
                        num7--;
                        offset++;
                        num20++;
                    }
                Label_0429:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_045E;
                    }
                    goto Label_03F4;
                Label_0439:
                    setli(input, startlocation, offset, getli(input, startOffset, offset, 1) ^ maxValue, 1);
                    num7--;
                    offset++;
                Label_045E:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_0439;
                    }
                    continue;
                Label_0470:
                    num21 = 0;
                    while (num21 < 8)
                    {
                        num15 = (byte) (num15 << 1);
                        if (num15 == 0)
                        {
                            num16 = (num11 != 0) ? ((byte) num11) : buffer[num3++];
                            num15 = 1;
                        }
                        if ((num16 & num15) != 0)
                        {
                            input[startlocation + offset] = (byte) maxValue;
                        }
                        else
                        {
                            input[startlocation + offset] = 0;
                        }
                        num7--;
                        offset++;
                        num21++;
                    }
                Label_04C7:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_051E;
                    }
                    goto Label_0470;
                Label_04D7:
                    num15 = (byte) (num15 << 1);
                    if (num15 == 0)
                    {
                        num16 = (num11 != 0) ? ((byte) num11) : buffer[num3++];
                        num15 = 1;
                    }
                    if ((num16 & num15) != 0)
                    {
                        input[startlocation + offset] = (byte) maxValue;
                    }
                    else
                    {
                        input[startlocation + offset] = 0;
                    }
                    num7--;
                    offset++;
                Label_051E:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_04D7;
                    }
                    continue;
                Label_0530:
                    num22 = 0;
                    while (num22 < 8)
                    {
                        num15 = (byte) (num15 << 1);
                        if (num15 == 0)
                        {
                            num16 = (num11 != 0) ? ((byte) num11) : buffer[num3++];
                            num15 = 1;
                        }
                        if ((num16 & num15) != 0)
                        {
                            input[startlocation + offset] = (byte) (input[startOffset + offset] ^ ((byte) maxValue));
                        }
                        else
                        {
                            input[startlocation + offset] = input[startOffset + offset];
                        }
                        num7--;
                        offset++;
                        num22++;
                    }
                Label_0596:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_05FC;
                    }
                    goto Label_0530;
                Label_05A6:
                    num15 = (byte) (num15 << 1);
                    if (num15 == 0)
                    {
                        num16 = (num11 != 0) ? ((byte) num11) : buffer[num3++];
                        num15 = 1;
                    }
                    if ((num16 & num15) != 0)
                    {
                        input[startlocation + offset] = (byte) (input[startOffset + offset] ^ ((byte) maxValue));
                    }
                    else
                    {
                        input[startlocation + offset] = input[startOffset + offset];
                    }
                    num7--;
                    offset++;
                Label_05FC:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_05A6;
                    }
                    continue;
                Label_060E:
                    num23 = 0;
                    while (num23 < 8)
                    {
                        input[startlocation + offset] = (byte) num14;
                        num7--;
                        offset++;
                        num23++;
                    }
                Label_0634:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_065A;
                    }
                    goto Label_060E;
                Label_0644:
                    input[startlocation + offset] = (byte) num14;
                    num7--;
                    offset++;
                Label_065A:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_0644;
                    }
                    continue;
                Label_066C:
                    num24 = 0;
                    while (num24 < 8)
                    {
                        input[startlocation + offset] = buffer[num3++];
                        num7--;
                        offset++;
                        num24++;
                    }
                Label_0696:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_06C0;
                    }
                    goto Label_066C;
                Label_06A6:
                    input[startlocation + offset] = buffer[num3++];
                    num7--;
                    offset++;
                Label_06C0:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_06A6;
                    }
                    continue;
                Label_06D2:
                    num25 = 0;
                    while (num25 < 8)
                    {
                        if (flag2)
                        {
                            input[startlocation + offset] = (byte) num14;
                            flag2 = false;
                        }
                        else
                        {
                            input[startlocation + offset] = (byte) num13;
                            flag2 = true;
                            num7++;
                        }
                        num7--;
                        offset++;
                        num25++;
                    }
                Label_0714:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_0756;
                    }
                    goto Label_06D2;
                Label_0724:
                    if (flag2)
                    {
                        input[startlocation + offset] = (byte) num14;
                        flag2 = false;
                    }
                    else
                    {
                        input[startlocation + offset] = (byte) num13;
                        flag2 = true;
                        num7++;
                    }
                    num7--;
                    offset++;
                Label_0756:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_0724;
                    }
                    continue;
                Label_0768:
                    num26 = 0;
                    while (num26 < 8)
                    {
                        input[startlocation + offset] = 0xff;
                        num7--;
                        offset++;
                        num26++;
                    }
                Label_0790:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_07B8;
                    }
                    goto Label_0768;
                Label_07A0:
                    input[startlocation + offset] = 0xff;
                    num7--;
                    offset++;
                Label_07B8:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_07A0;
                    }
                    continue;
                Label_07C4:
                    num27 = 0;
                    while (num27 < 8)
                    {
                        input[startlocation + offset] = 0;
                        num7--;
                        offset++;
                        num27++;
                    }
                Label_07E8:
                    if (((num7 & -8) == 0) || ((offset + 8) >= width))
                    {
                        goto Label_080C;
                    }
                    goto Label_07C4;
                Label_07F8:
                    input[startlocation + offset] = 0;
                    num7--;
                    offset++;
                Label_080C:
                    if ((num7 > 0) && (offset < width))
                    {
                        goto Label_07F8;
                    }
                }
            }
            _g++;
            return input;
        }

        public static uint[] decompressInt(int width, int height, int size, RdpPacket data, int Bpp)
        {
            byte[] buffer = new byte[size];
            data.Read(buffer, 0, size);
            int num = -1;
            int num2 = 0;
            int offset = 0;
            int num4 = 0;
            int num5 = size;
            int num6 = 0;
            uint num7 = 0;
            int num8 = 0;
            int num9 = width;
            int num10 = -1;
            int num11 = 0;
            int num12 = 0;
            uint num13 = 0;
            uint num14 = 0;
            byte num15 = 0;
            int num16 = 0;
            uint maxValue = uint.MaxValue;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            uint[] numArray = new uint[width * height];
            try
            {
                while (offset < num5)
                {
                    num11 = 0;
                    num12 = buffer[offset++] & 0xff;
                    num6 = num12 >> 4;
                    switch (num6)
                    {
                        case 12:
                        case 13:
                        case 14:
                            num6 -= 6;
                            num7 = (uint) (num12 & 15);
                            num8 = 0x10;
                            goto Label_00F8;

                        case 15:
                            num6 = num12 & 15;
                            if (num6 >= 9)
                            {
                                break;
                            }
                            num7 = (uint) (buffer[offset++] & 0xff);
                            num7 |= (uint) ((buffer[offset++] & 0xff) << 8);
                            goto Label_00E2;

                        default:
                            num6 = num6 >> 1;
                            num7 = (uint) (num12 & 0x1f);
                            num8 = 0x20;
                            goto Label_00F8;
                    }
                    num7 = (uint)(num6 < 11 ? 8 : 1);
                Label_00E2:
                    num8 = 0;
                Label_00F8:
                    if (num8 != 0)
                    {
                        flag3 = (num6 == 2) || (num6 == 7);
                        if (num7 == 0)
                        {
                            if (flag3)
                            {
                                num7 = (uint) ((buffer[offset++] & 0xff) + 1);
                            }
                            else
                            {
                                num7 = (uint) ((buffer[offset++] & 0xff) + num8);
                            }
                        }
                        else if (flag3)
                        {
                            num7 = num7 << 3;
                        }
                    }
                    switch (num6)
                    {
                        case 0:
                            if ((num10 == num6) && ((num9 != width) || (num != -1)))
                            {
                                flag = true;
                            }
                            goto Label_01DF;

                        case 3:
                            break;

                        case 6:
                        case 7:
                            maxValue = cvalx(buffer, offset, Bpp);
                            offset += Bpp;
                            num6 -= 5;
                            goto Label_01DF;

                        case 8:
                            num13 = cvalx(buffer, offset, Bpp);
                            offset += Bpp;
                            break;

                        case 9:
                            num16 = 3;
                            num6 = 2;
                            num11 = 3;
                            goto Label_01DF;

                        case 10:
                            num16 = 5;
                            num6 = 2;
                            num11 = 5;
                            goto Label_01DF;

                        default:
                            goto Label_01DF;
                    }
                    num14 = cvalx(buffer, offset, Bpp);
                    offset += Bpp;
                Label_01DF:
                    num10 = num6;
                    num15 = 0;
                    while (num7 > 0)
                    {
                        int num19;
                        int num20;
                        int num21;
                        int num22;
                        int num23;
                        int num24;
                        int num25;
                        int num26;
                        int num27;
                        int num28;
                        if (num9 >= width)
                        {
                            if (height <= 0)
                            {
                                throw new RDFatalException("Illegal int bitmap height: " + height.ToString());
                            }
                            num9 = 0;
                            height--;
                            num = num2;
                            num2 = num4 + (height * width);
                        }
                        switch (num6)
                        {
                            case 0:
                                if (!flag)
                                {
                                    goto Label_029B;
                                }
                                if (num != -1)
                                {
                                    break;
                                }
                                numArray[num2 + num9] = maxValue;
                                goto Label_028C;

                            case 1:
                                if (num != -1)
                                {
                                    goto Label_0402;
                                }
                                goto Label_039A;

                            case 2:
                                if (num != -1)
                                {
                                    goto Label_0571;
                                }
                                goto Label_04A3;

                            case 3:
                                goto Label_060F;

                            case 4:
                                goto Label_067B;

                            case 8:
                                goto Label_0702;

                            case 13:
                                goto Label_0780;

                            case 14:
                                goto Label_07DC;

                            default:
                                throw new RDFatalException("Illegal int bitmap opcode in " + num6.ToString());
                        }
                        numArray[num2 + num9] = numArray[num + num9] ^ maxValue;
                    Label_028C:
                        flag = false;
                        num7--;
                        num9++;
                    Label_029B:
                        if (num != -1)
                        {
                            goto Label_032C;
                        }
                        while (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                numArray[num2 + num9] = 0;
                                num7--;
                                num9++;
                            }
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = 0;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_0302:
                        num19 = 0;
                        while (num19 < 8)
                        {
                            numArray[num2 + num9] = numArray[num + num9];
                            num7--;
                            num9++;
                            num19++;
                        }
                    Label_032C:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_0302;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = numArray[num + num9];
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_0375:
                        num20 = 0;
                        while (num20 < 8)
                        {
                            numArray[num2 + num9] = maxValue;
                            num7--;
                            num9++;
                            num20++;
                        }
                    Label_039A:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_0375;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = maxValue;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_03D5:
                        num21 = 0;
                        while (num21 < 8)
                        {
                            numArray[num2 + num9] = numArray[num + num9] ^ maxValue;
                            num7--;
                            num9++;
                            num21++;
                        }
                    Label_0402:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_03D5;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = numArray[num + num9] ^ maxValue;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_044E:
                        num22 = 0;
                        while (num22 < 8)
                        {
                            num15 = (byte) (num15 << 1);
                            if (num15 == 0)
                            {
                                num16 = (num11 != 0) ? num11 : buffer[offset++];
                                num15 = 1;
                            }
                            if ((num16 & num15) != 0)
                            {
                                numArray[num2 + num9] = maxValue;
                            }
                            else
                            {
                                numArray[num2 + num9] = 0;
                            }
                            num7--;
                            num9++;
                            num22++;
                        }
                    Label_04A3:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_044E;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            num15 = (byte) (num15 << 1);
                            if (num15 == 0)
                            {
                                num16 = (num11 != 0) ? num11 : buffer[offset++];
                                num15 = 1;
                            }
                            if ((num16 & num15) != 0)
                            {
                                numArray[num2 + num9] = maxValue;
                            }
                            else
                            {
                                numArray[num2 + num9] = 0;
                            }
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_050E:
                        num23 = 0;
                        while (num23 < 8)
                        {
                            num15 = (byte) (num15 << 1);
                            if (num15 == 0)
                            {
                                num16 = (num11 != 0) ? num11 : buffer[offset++];
                                num15 = 1;
                            }
                            if ((num16 & num15) != 0)
                            {
                                numArray[num2 + num9] = numArray[num + num9] ^ maxValue;
                            }
                            else
                            {
                                numArray[num2 + num9] = numArray[num + num9];
                            }
                            num7--;
                            num9++;
                            num23++;
                        }
                    Label_0571:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_050E;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            num15 = (byte) (num15 << 1);
                            if (num15 == 0)
                            {
                                num16 = (num11 != 0) ? num11 : buffer[offset++];
                                num15 = 1;
                            }
                            if ((num16 & num15) != 0)
                            {
                                numArray[num2 + num9] = numArray[num + num9] ^ maxValue;
                            }
                            else
                            {
                                numArray[num2 + num9] = numArray[num + num9];
                            }
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_05EA:
                        num24 = 0;
                        while (num24 < 8)
                        {
                            numArray[num2 + num9] = num14;
                            num7--;
                            num9++;
                            num24++;
                        }
                    Label_060F:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_05EA;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = num14;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_064A:
                        num25 = 0;
                        while (num25 < 8)
                        {
                            numArray[num2 + num9] = cvalx(buffer, offset, Bpp);
                            offset += Bpp;
                            num7--;
                            num9++;
                            num25++;
                        }
                    Label_067B:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_064A;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = cvalx(buffer, offset, Bpp);
                            offset += Bpp;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_06C2:
                        num26 = 0;
                        while (num26 < 8)
                        {
                            if (flag2)
                            {
                                numArray[num2 + num9] = num14;
                                flag2 = false;
                            }
                            else
                            {
                                numArray[num2 + num9] = num13;
                                flag2 = true;
                                num7++;
                            }
                            num7--;
                            num9++;
                            num26++;
                        }
                    Label_0702:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_06C2;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            if (flag2)
                            {
                                numArray[num2 + num9] = num14;
                                flag2 = false;
                            }
                            else
                            {
                                numArray[num2 + num9] = num13;
                                flag2 = true;
                                num7++;
                            }
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_0758:
                        num27 = 0;
                        while (num27 < 8)
                        {
                            numArray[num2 + num9] = 0xffffff;
                            num7--;
                            num9++;
                            num27++;
                        }
                    Label_0780:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_0758;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = 0xffffff;
                            num7--;
                            num9++;
                        }
                        continue;
                    Label_07B8:
                        num28 = 0;
                        while (num28 < 8)
                        {
                            numArray[num2 + num9] = 0;
                            num7--;
                            num9++;
                            num28++;
                        }
                    Label_07DC:
                        if (((num7 & 18446744073709551608L) != 0L) && ((num9 + 8) < width))
                        {
                            goto Label_07B8;
                        }
                        while ((num7 > 0) && (num9 < width))
                        {
                            numArray[num2 + num9] = 0;
                            num7--;
                            num9++;
                        }
                    }
                }
            }
            catch
            {
            }
            return numArray;
        }

        public void Deserialise(BinaryReader reader)
        {
            byte num = reader.ReadByte();
            bool flag = reader.ReadBoolean();
            reader.ReadByte();
            this.Bpp = reader.ReadByte();
            this.width = reader.ReadInt32();
            this.height = reader.ReadInt32();
            if (num >= 2)
            {
                this.persist_key = reader.ReadUInt64();
                this.persists = true;
            }
            if (flag)
            {
                int num2 = reader.ReadInt32();
                this.highdata = new uint[num2];
                for (int i = 0; i < num2; i++)
                {
                    this.highdata[i] = reader.ReadUInt32();
                }
            }
            else
            {
                int count = reader.ReadInt32();
                this.bytedata = new byte[count];
                reader.Read(this.bytedata, 0, count);
            }
        }

        public int getBPP()
        {
            return this.Bpp;
        }

        public uint[] getData(int colorTableIndex)
        {
            if (this.Bpp != 1)
            {
                return this.highdata;
            }
            if (this.highdata == null)
            {
                return convert8bitImage(this.bytedata, this.Bpp);
            }
            return convertImage(this.highdata, colorTableIndex);
        }

        public int getHeight()
        {
            return this.height;
        }

        private static uint getli(byte[] input, int startOffset, int offset, int Bpp)
        {
            uint num = 0;
            int num2 = startOffset + (offset * Bpp);
            for (int i = 0; i < Bpp; i++)
            {
                num = num << 8;
                num |= (uint) (input[num2 + ((Bpp - i) - 1)] & 0xff);
            }
            return num;
        }

        public int getWidth()
        {
            return this.width;
        }

        public int getX()
        {
            return this.x;
        }

        public int getY()
        {
            return this.y;
        }

        public void PersistCache(ulong key)
        {
            this.persists = true;
            this.persist_key = key;
        }

        public void Serialise(BinaryWriter writer)
        {
            writer.Write((byte) 2);
            writer.Write(this.highdata != null);
            writer.Write((byte) 1);
            writer.Write((byte) this.Bpp);
            writer.Write(this.width);
            writer.Write(this.height);
            writer.Write(this.persist_key);
            if (this.highdata != null)
            {
                writer.Write(this.highdata.Length);
                foreach (uint num in this.highdata)
                {
                    writer.Write(num);
                }
            }
            else
            {
                writer.Write(this.bytedata.Length);
                writer.Write(this.bytedata);
            }
        }

        private static void setli(byte[] input, int startlocation, int offset, uint value, int Bpp)
        {
            int index = startlocation + (offset * Bpp);
            input[index] = (byte) (value & 0xff);
            if (Bpp > 1)
            {
                input[index + 1] = (byte) ((value & 0xff00) >> 8);
            }
            if (Bpp > 2)
            {
                input[index + 2] = (byte) ((value & 0xff0000) >> 0x10);
            }
        }

    }
}