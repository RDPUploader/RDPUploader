using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdpUploadClient
{
    internal static class BER
    {
        internal static int BERIntSize(int data)
        {
            if (data > 0xff)
            {
                return 4;
            }

            return 3;
        }

        internal static int berParseHeader(RdpPacket data, BER_Header eTagVal)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = (int)eTagVal;
            if (num4 > 0xff)
            {
                num = data.ReadBigEndian16();
            }
            else
            {
                num = data.ReadByte();
            }
            if (num != num4)
            {
                throw new RDFatalException("Bad tag " + num.ToString() + " but need " + eTagVal.ToString());
            }
            num3 = data.ReadByte();
            if (num3 <= 0x80)
            {
                return num3;
            }
            num3 -= 0x80;
            num2 = 0;
            while (num3-- != 0)
            {
                num2 = (num2 << 8) + data.ReadByte();
            }
            return num2;
        }

        internal static int domainParamSize(int param0, int param1, int param2, int param3)
        {
            int num = ((((((BERIntSize(param0) + BERIntSize(param1)) + BERIntSize(param2)) 
                + BERIntSize(1)) + BERIntSize(0)) + BERIntSize(1)) + BERIntSize(param3)) + BERIntSize(2);

            return c(0x30, num) + num;
        }

        internal static int c(int param0, int param1)
        {
            int num = 0;

            if (param0 > 0xff)
            {
                num += 2;
            }
            else
            {
                num++;
            }

            if (param1 >= 0x80)
            {
                return (num + 3);
            }

            return ++num;
        }

        internal static void sendBerHeader(RdpPacket data, BER_Header berHeader, int param)
        {
            int num = (int)berHeader;
            if (num > 0xff)
            {
                data.WriteBigEndian16((short)num);
            }
            else
            {
                data.WriteByte((byte)num);
            }
            if (param >= 0x80)
            {
                data.WriteByte(130);
                data.WriteBigEndian16((short)param);
            }
            else
            {
                data.WriteByte((byte)param);
            }
        }

        internal static void sendBerInteger(RdpPacket buffer, int value)
        {
            int num = 1;

            if (value > 0xff)
            {
                num = 2;
            }

            sendBerHeader(buffer, BER_Header.BER_TAG_INTEGER, num);

            if (value > 0xff)
            {
                buffer.WriteBigEndian16((short)value);
            }
            else
            {
                buffer.WriteByte((byte)value);
            }
        }

        [Flags]
        internal enum BER_Header
        {
            BER_TAG_BOOLEAN = 1,
            BER_TAG_INTEGER = 2,
            BER_TAG_OCTET_STRING = 4,
            BER_TAG_RESULT = 10,
            CONNECT_INITIAL = 0x7f65,
            CONNECT_RESPONSE = 0x7f66,
            TAG_DOMAIN_PARAMS = 0x30
        }

    }
}