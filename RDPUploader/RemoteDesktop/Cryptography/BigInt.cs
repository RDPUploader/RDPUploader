using System;

namespace RemoteDesktop
{
    public class  BigInt
    {
        private const int maxLength = 300;

        internal static readonly int[] primesBelow2000 = new int[]
        {
            2,
            3,
            5,
            7,
            11,
            13,
            17,
            19,
            23,
            29,
            31,
            37,
            41,
            43,
            47,
            53,
            59,
            61,
            67,
            71,
            73,
            79,
            83,
            89,
            97,
            101,
            103,
            107,
            109,
            113,
            127,
            131,
			137,
			139,
			149,
			151,
			157,
			163,
			167,
			173,
			179,
			181,
			191,
			193,
			197,
			199,
			211,
			223,
			227,
			229,
			233,
			239,
			241,
			251,
			257,
			263,
			269,
			271,
			277,
			281,
			283,
			293,
			307,
			311,
			313,
			317,
			331,
			337,
			347,
			349,
			353,
			359,
			367,
			373,
			379,
			383,
			389,
			397,
			401,
			409,
			419,
			421,
			431,
			433,
			439,
			443,
			449,
			457,
			461,
			463,
			467,
			479,
			487,
			491,
			499,
			503,
			509,
			521,
			523,
			541,
			547,
			557,
			563,
			569,
			571,
			577,
			587,
			593,
			599,
			601,
			607,
			613,
			617,
			619,
			631,
			641,
			643,
			647,
			653,
			659,
			661,
			673,
			677,
			683,
			691,
			701,
			709,
			719,
			727,
			733,
			739,
			743,
			751,
			757,
			761,
			769,
			773,
			787,
			797,
			809,
			811,
			821,
			823,
			827,
			829,
			839,
			853,
			857,
			859,
			863,
			877,
			881,
			883,
			887,
			907,
			911,
			919,
			929,
			937,
			941,
			947,
			953,
			967,
			971,
			977,
			983,
			991,
			997,
			1009,
			1013,
			1019,
			1021,
			1031,
			1033,
			1039,
			1049,
			1051,
			1061,
			1063,
			1069,
			1087,
			1091,
			1093,
			1097,
			1103,
			1109,
			1117,
			1123,
			1129,
			1151,
			1153,
			1163,
			1171,
			1181,
			1187,
			1193,
			1201,
			1213,
			1217,
			1223,
			1229,
			1231,
			1237,
			1249,
			1259,
			1277,
			1279,
			1283,
			1289,
			1291,
			1297,
			1301,
			1303,
			1307,
			1319,
			1321,
			1327,
			1361,
			1367,
			1373,
			1381,
			1399,
			1409,
			1423,
			1427,
			1429,
			1433,
			1439,
			1447,
			1451,
			1453,
			1459,
			1471,
			1481,
			1483,
			1487,
			1489,
			1493,
			1499,
			1511,
			1523,
			1531,
			1543,
			1549,
			1553,
			1559,
			1567,
			1571,
			1579,
			1583,
			1597,
			1601,
			1607,
			1609,
			1613,
			1619,
			1621,
			1627,
			1637,
			1657,
			1663,
			1667,
			1669,
			1693,
			1697,
			1699,
			1709,
			1721,
			1723,
			1733,
			1741,
			1747,
			1753,
			1759,
			1777,
			1783,
			1787,
			1789,
			1801,
			1811,
			1823,
			1831,
			1847,
			1861,
			1867,
			1871,
			1873,
			1877,
			1879,
			1889,
			1901,
			1907,
			1913,
			1931,
			1933,
			1949,
			1951,
			1973,
			1979,
			1987,
			1993,
			1997,
			1999
		};
        private uint[] data;
        internal int dataLength;
        public BigInt()
        {
            this.data = new uint[300];
            this.dataLength = 1;
        }
        public BigInt(long value)
        {
            this.data = new uint[300];
            long num = value;
            this.dataLength = 0;
            while (value != 0L && this.dataLength < 300)
            {
                this.data[this.dataLength] = (uint)(value & -1L);
                value >>= 32;
                this.dataLength++;
            }
            if (num > 0L)
            {
                if (value != 0L || (this.data[299] & 2147483648u) != 0u)
                {
                    throw new ArithmeticException("Positive overflow in constructor.");
                }
            }
            else
            {
                if (num < 0L && (value != -1L || (this.data[this.dataLength - 1] & 2147483648u) == 0u))
                {
                    throw new ArithmeticException("Negative underflow in constructor.");
                }
            }
            if (this.dataLength == 0)
            {
                this.dataLength = 1;
            }
        }
        public BigInt(ulong value)
        {
            this.data = new uint[300];
            this.dataLength = 0;
            while (value != 0uL && this.dataLength < 300)
            {
                this.data[this.dataLength] = (uint)(value & 18446744073709551615uL);
                value >>= 32;
                this.dataLength++;
            }
            if (value != 0uL || (this.data[299] & 2147483648u) != 0u)
            {
                throw new ArithmeticException("+ve overflow.");
            }
            if (this.dataLength == 0)
            {
                this.dataLength = 1;
            }
        }
        public BigInt(BigInt bi)
        {
            this.data = new uint[300];
            this.dataLength = bi.dataLength;
            for (int i = 0; i < this.dataLength; i++)
            {
                this.data[i] = bi.data[i];
            }
        }
        public BigInt(string value, int radix)
        {
            BigInt bi = new BigInt(1L);
            BigInt bigInt = new BigInt();
            value = value.ToUpper().Trim();
            int num = 0;
            if (value[0] == '-')
            {
                num = 1;
            }
            for (int i = value.Length - 1; i >= num; i--)
            {
                int num2 = (int)value[i];
                if (num2 >= 48 && num2 <= 57)
                {
                    num2 -= 48;
                }
                else
                {
                    if (num2 >= 65 && num2 <= 90)
                    {
                        num2 = num2 - 65 + 10;
                    }
                    else
                    {
                        num2 = 9999999;
                    }
                }
                if (num2 >= radix)
                {
                    throw new ArithmeticException("Invalid string.");
                }
                if (value[0] == '-')
                {
                    num2 = -num2;
                }
                bigInt += bi * num2;
                if (i - 1 >= num)
                {
                    bi *= radix;
                }
            }
            if (value[0] == '-')
            {
                if ((bigInt.data[299] & 2147483648u) == 0u)
                {
                    throw new ArithmeticException("-ve underflow.");
                }
            }
            else
            {
                if ((bigInt.data[299] & 2147483648u) != 0u)
                {
                    throw new ArithmeticException("+ve overflow.");
                }
            }
            this.data = new uint[300];
            for (int j = 0; j < bigInt.dataLength; j++)
            {
                this.data[j] = bigInt.data[j];
            }
            this.dataLength = bigInt.dataLength;
        }
        public BigInt(byte[] inData)
        {
            this.dataLength = inData.Length >> 2;
            int num = inData.Length & 3;
            if (num != 0)
            {
                this.dataLength++;
            }
            if (this.dataLength > 300)
            {
                throw new ArithmeticException("Bytes overflow.");
            }
            this.data = new uint[300];
            int i = inData.Length - 1;
            int num2 = 0;
            while (i >= 3)
            {
                this.data[num2] = (uint)(((int)inData[i - 3] << 24) + ((int)inData[i - 2] << 16) + ((int)inData[i - 1] << 8) + (int)inData[i]);
                i -= 4;
                num2++;
            }
            if (num == 1)
            {
                this.data[this.dataLength - 1] = (uint)inData[0];
            }
            else
            {
                if (num == 2)
                {
                    this.data[this.dataLength - 1] = (uint)(((int)inData[0] << 8) + (int)inData[1]);
                }
                else
                {
                    if (num == 3)
                    {
                        this.data[this.dataLength - 1] = (uint)(((int)inData[0] << 16) + ((int)inData[1] << 8) + (int)inData[2]);
                    }
                }
            }
            while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0u)
            {
                this.dataLength--;
            }
        }
        public BigInt(byte[] inData, int inLen)
        {
            this.dataLength = inLen >> 2;
            int num = inLen & 3;
            if (num != 0)
            {
                this.dataLength++;
            }
            if (this.dataLength > 300 || inLen > inData.Length)
            {
                throw new ArithmeticException("Bytes overflow.");
            }
            this.data = new uint[300];
            int i = inLen - 1;
            int num2 = 0;
            while (i >= 3)
            {
                this.data[num2] = (uint)(((int)inData[i - 3] << 24) + ((int)inData[i - 2] << 16) + ((int)inData[i - 1] << 8) + (int)inData[i]);
                i -= 4;
                num2++;
            }
            if (num == 1)
            {
                this.data[this.dataLength - 1] = (uint)inData[0];
            }
            else
            {
                if (num == 2)
                {
                    this.data[this.dataLength - 1] = (uint)(((int)inData[0] << 8) + (int)inData[1]);
                }
                else
                {
                    if (num == 3)
                    {
                        this.data[this.dataLength - 1] = (uint)(((int)inData[0] << 16) + ((int)inData[1] << 8) + (int)inData[2]);
                    }
                }
            }
            if (this.dataLength == 0)
            {
                this.dataLength = 1;
            }
            while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0u)
            {
                this.dataLength--;
            }
        }
        public BigInt(uint[] inData)
        {
            this.dataLength = inData.Length;
            if (this.dataLength > 300)
            {
                throw new ArithmeticException("Bytes overflow.");
            }
            this.data = new uint[300];
            int i = this.dataLength - 1;
            int num = 0;
            while (i >= 0)
            {
                this.data[num] = inData[i];
                i--;
                num++;
            }
            while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0u)
            {
                this.dataLength--;
            }
        }
        public static implicit operator BigInt(long value)
        {
            return new BigInt(value);
        }
        public static implicit operator BigInt(ulong value)
        {
            return new BigInt(value);
        }
        public static implicit operator BigInt(int value)
        {
            return new BigInt((long)value);
        }
        public static implicit operator BigInt(uint value)
        {
            return new BigInt((ulong)value);
        }
        public static BigInt operator +(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            bigInt.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
            long num = 0L;
            for (int i = 0; i < bigInt.dataLength; i++)
            {
                long num2 = (long)((ulong)bi1.data[i] + (ulong)bi2.data[i] + (ulong)num);
                num = num2 >> 32;
                bigInt.data[i] = (uint)(num2 & -1L);
            }
            if (num != 0L && bigInt.dataLength < 300)
            {
                bigInt.data[bigInt.dataLength] = (uint)num;
                bigInt.dataLength++;
            }
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            int num3 = 299;
            if ((bi1.data[num3] & 2147483648u) == (bi2.data[num3] & 2147483648u) && (bigInt.data[num3] & 2147483648u) != (bi1.data[num3] & 2147483648u))
            {
                throw new ArithmeticException();
            }
            return bigInt;
        }
        public static BigInt operator ++(BigInt bi1)
        {
            BigInt bigInt = new BigInt(bi1);
            long num = 1L;
            int num2 = 0;
            while (num != 0L && num2 < 300)
            {
                long num3 = (long)((ulong)bigInt.data[num2]);
                num3 += 1L;
                bigInt.data[num2] = (uint)(num3 & -1L);
                num = num3 >> 32;
                num2++;
            }
            if (num2 > bigInt.dataLength)
            {
                bigInt.dataLength = num2;
            }
            else
            {
                while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
                {
                    bigInt.dataLength--;
                }
            }
            int num4 = 299;
            if ((bi1.data[num4] & 2147483648u) == 0u && (bigInt.data[num4] & 2147483648u) != (bi1.data[num4] & 2147483648u))
            {
                throw new ArithmeticException("Overflow in ++.");
            }
            return bigInt;
        }
        public static BigInt operator -(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            bigInt.dataLength = ((bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength);
            long num = 0L;
            for (int i = 0; i < bigInt.dataLength; i++)
            {
                long num2 = (long)((ulong)bi1.data[i] - (ulong)bi2.data[i] - (ulong)num);
                bigInt.data[i] = (uint)(num2 & -1L);
                if (num2 < 0L)
                {
                    num = 1L;
                }
                else
                {
                    num = 0L;
                }
            }
            if (num != 0L)
            {
                for (int j = bigInt.dataLength; j < 300; j++)
                {
                    bigInt.data[j] = 4294967295u;
                }
                bigInt.dataLength = 300;
            }
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            int num3 = 299;
            if ((bi1.data[num3] & 2147483648u) != (bi2.data[num3] & 2147483648u) && (bigInt.data[num3] & 2147483648u) != (bi1.data[num3] & 2147483648u))
            {
                throw new ArithmeticException();
            }
            return bigInt;
        }
        public static BigInt operator --(BigInt bi1)
        {
            BigInt bigInt = new BigInt(bi1);
            bool flag = true;
            int num = 0;
            while (flag && num < 300)
            {
                long num2 = (long)((ulong)bigInt.data[num]);
                num2 -= 1L;
                bigInt.data[num] = (uint)(num2 & -1L);
                if (num2 >= 0L)
                {
                    flag = false;
                }
                num++;
            }
            if (num > bigInt.dataLength)
            {
                bigInt.dataLength = num;
            }
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            int num3 = 299;
            if ((bi1.data[num3] & 2147483648u) != 0u && (bigInt.data[num3] & 2147483648u) != (bi1.data[num3] & 2147483648u))
            {
                throw new ArithmeticException("Underflow in --.");
            }
            return bigInt;
        }
        public static BigInt operator *(BigInt bi1, BigInt bi2)
        {
            int num = 299;
            bool flag = false;
            bool flag2 = false;
            try
            {
                if ((bi1.data[num] & 2147483648u) != 0u)
                {
                    flag = true;
                    bi1 = -bi1;
                }
                if ((bi2.data[num] & 2147483648u) != 0u)
                {
                    flag2 = true;
                    bi2 = -bi2;
                }
            }
            catch (Exception)
            {
            }
            BigInt bigInt = new BigInt();
            try
            {
                for (int i = 0; i < bi1.dataLength; i++)
                {
                    if (bi1.data[i] != 0u)
                    {
                        ulong num2 = 0uL;
                        int j = 0;
                        int num3 = i;
                        while (j < bi2.dataLength)
                        {
                            ulong num4 = (ulong)bi1.data[i] * (ulong)bi2.data[j] + (ulong)bigInt.data[num3] + num2;
                            bigInt.data[num3] = (uint)(num4 & 18446744073709551615uL);
                            num2 = num4 >> 32;
                            j++;
                            num3++;
                        }
                        if (num2 != 0uL)
                        {
                            bigInt.data[i + bi2.dataLength] = (uint)num2;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new ArithmeticException("Multiplication overflow.");
            }
            bigInt.dataLength = bi1.dataLength + bi2.dataLength;
            if (bigInt.dataLength > 300)
            {
                bigInt.dataLength = 300;
            }
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            if ((bigInt.data[num] & 2147483648u) != 0u)
            {
                if (flag != flag2 && bigInt.data[num] == 2147483648u)
                {
                    if (bigInt.dataLength == 1)
                    {
                        return bigInt;
                    }
                    bool flag3 = true;
                    int num5 = 0;
                    while (num5 < bigInt.dataLength - 1 && flag3)
                    {
                        if (bigInt.data[num5] != 0u)
                        {
                            flag3 = false;
                        }
                        num5++;
                    }
                    if (flag3)
                    {
                        return bigInt;
                    }
                }
                throw new ArithmeticException("Multiplication overflow.");
            }
            if (flag != flag2)
            {
                return -bigInt;
            }
            return bigInt;
        }
        public static BigInt operator <<(BigInt bi1, int shiftVal)
        {
            BigInt bigInt = new BigInt(bi1);
            bigInt.dataLength = BigInt.shiftLeft(bigInt.data, shiftVal);
            return bigInt;
        }
        private static int shiftLeft(uint[] buffer, int shiftVal)
        {
            int num = 32;
            int num2 = buffer.Length;
            while (num2 > 1 && buffer[num2 - 1] == 0u)
            {
                num2--;
            }
            for (int i = shiftVal; i > 0; i -= num)
            {
                if (i < num)
                {
                    num = i;
                }
                ulong num3 = 0uL;
                for (int j = 0; j < num2; j++)
                {
                    ulong num4 = (ulong)buffer[j] << num;
                    num4 |= num3;
                    buffer[j] = (uint)(num4 & 18446744073709551615uL);
                    num3 = num4 >> 32;
                }
                if (num3 != 0uL && num2 + 1 <= buffer.Length)
                {
                    buffer[num2] = (uint)num3;
                    num2++;
                }
            }
            return num2;
        }
        public static BigInt operator >>(BigInt bi1, int shiftVal)
        {
            BigInt bigInt = new BigInt(bi1);
            bigInt.dataLength = BigInt.shiftRight(bigInt.data, shiftVal);
            if ((bi1.data[299] & 2147483648u) != 0u)
            {
                for (int i = 299; i >= bigInt.dataLength; i--)
                {
                    bigInt.data[i] = 4294967295u;
                }
                uint num = 2147483648u;
                int num2 = 0;
                while (num2 < 32 && (bigInt.data[bigInt.dataLength - 1] & num) == 0u)
                {
                    bigInt.data[bigInt.dataLength - 1] |= num;
                    num >>= 1;
                    num2++;
                }
                bigInt.dataLength = 300;
            }
            return bigInt;
        }
        private static int shiftRight(uint[] buffer, int shiftVal)
        {
            int num = 32;
            int num2 = 0;
            int num3 = buffer.Length;
            while (num3 > 1 && buffer[num3 - 1] == 0u)
            {
                num3--;
            }
            for (int i = shiftVal; i > 0; i -= num)
            {
                if (i < num)
                {
                    num = i;
                    num2 = 32 - num;
                }
                ulong num4 = 0uL;
                for (int j = num3 - 1; j >= 0; j--)
                {
                    ulong num5 = (ulong)buffer[j] >> num;
                    num5 |= num4;
                    num4 = ((ulong)buffer[j] << num2 & 18446744073709551615uL);
                    buffer[j] = (uint)num5;
                }
            }
            while (num3 > 1 && buffer[num3 - 1] == 0u)
            {
                num3--;
            }
            return num3;
        }
        public static BigInt operator ~(BigInt bi1)
        {
            BigInt bigInt = new BigInt(bi1);
            for (int i = 0; i < 300; i++)
            {
                bigInt.data[i] = ~bi1.data[i];
            }
            bigInt.dataLength = 300;
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            return bigInt;
        }
        public static BigInt operator -(BigInt bi1)
        {
            if (bi1.dataLength == 1 && bi1.data[0] == 0u)
            {
                return new BigInt();
            }
            BigInt bigInt = new BigInt(bi1);
            for (int i = 0; i < 300; i++)
            {
                bigInt.data[i] = ~bi1.data[i];
            }
            long num = 1L;
            int num2 = 0;
            while (num != 0L && num2 < 300)
            {
                long num3 = (long)((ulong)bigInt.data[num2]);
                num3 += 1L;
                bigInt.data[num2] = (uint)(num3 & -1L);
                num = num3 >> 32;
                num2++;
            }
            if ((bi1.data[299] & 2147483648u) == (bigInt.data[299] & 2147483648u))
            {
                throw new ArithmeticException("Overflow in negation.");
            }
            bigInt.dataLength = 300;
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            return bigInt;
        }
        public static bool operator ==(BigInt bi1, BigInt bi2)
        {
            return bi1.Equals(bi2);
        }
        public static bool operator !=(BigInt bi1, BigInt bi2)
        {
            return !bi1.Equals(bi2);
        }
        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }
            BigInt bigInt = (BigInt)o;
            if (this.dataLength != bigInt.dataLength)
            {
                return false;
            }
            for (int i = 0; i < this.dataLength; i++)
            {
                if (this.data[i] != bigInt.data[i])
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        public static bool operator >(BigInt bi1, BigInt bi2)
        {
            int num = 299;
            if ((bi1.data[num] & 2147483648u) != 0u && (bi2.data[num] & 2147483648u) == 0u)
            {
                return false;
            }
            if ((bi1.data[num] & 2147483648u) == 0u && (bi2.data[num] & 2147483648u) != 0u)
            {
                return true;
            }
            int num2 = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
            num = num2 - 1;
            while (num >= 0 && bi1.data[num] == bi2.data[num])
            {
                num--;
            }
            return num >= 0 && bi1.data[num] > bi2.data[num];
        }
        public static bool operator <(BigInt bi1, BigInt bi2)
        {
            int num = 299;
            if ((bi1.data[num] & 2147483648u) != 0u && (bi2.data[num] & 2147483648u) == 0u)
            {
                return true;
            }
            if ((bi1.data[num] & 2147483648u) == 0u && (bi2.data[num] & 2147483648u) != 0u)
            {
                return false;
            }
            int num2 = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
            num = num2 - 1;
            while (num >= 0 && bi1.data[num] == bi2.data[num])
            {
                num--;
            }
            return num >= 0 && bi1.data[num] < bi2.data[num];
        }
        public static bool operator >=(BigInt bi1, BigInt bi2)
        {
            return bi1 == bi2 || bi1 > bi2;
        }
        public static bool operator <=(BigInt bi1, BigInt bi2)
        {
            return bi1 == bi2 || bi1 < bi2;
        }
        private static void multiByteDivide(BigInt bi1, BigInt bi2, BigInt outQuotient, BigInt outRemainder)
        {
            uint[] array = new uint[300];
            int num = bi1.dataLength + 1;
            uint[] array2 = new uint[num];
            uint num2 = 2147483648u;
            uint num3 = bi2.data[bi2.dataLength - 1];
            int num4 = 0;
            int num5 = 0;
            while (num2 != 0u && (num3 & num2) == 0u)
            {
                num4++;
                num2 >>= 1;
            }
            for (int i = 0; i < bi1.dataLength; i++)
            {
                array2[i] = bi1.data[i];
            }
            BigInt.shiftLeft(array2, num4);
            bi2 <<= num4;
            int j = num - bi2.dataLength;
            int num6 = num - 1;
            ulong num7 = (ulong)bi2.data[bi2.dataLength - 1];
            ulong num8 = (ulong)bi2.data[bi2.dataLength - 2];
            int num9 = bi2.dataLength + 1;
            uint[] array3 = new uint[num9];
            while (j > 0)
            {
                ulong num10 = ((ulong)array2[num6] << 32) + (ulong)array2[num6 - 1];
                ulong num11 = num10 / num7;
                ulong num12 = num10 % num7;
                bool flag = false;
                while (!flag)
                {
                    flag = true;
                    if (num11 == 4294967296uL || num11 * num8 > (num12 << 32) + (ulong)array2[num6 - 2])
                    {
                        num11 -= 1uL;
                        num12 += num7;
                        if (num12 < 4294967296uL)
                        {
                            flag = false;
                        }
                    }
                }
                for (int k = 0; k < num9; k++)
                {
                    array3[k] = array2[num6 - k];
                }
                BigInt bigInt = new BigInt(array3);
                BigInt bigInt2 = bi2 * (long)num11;
                while (bigInt2 > bigInt)
                {
                    num11 -= 1uL;
                    bigInt2 -= bi2;
                }
                BigInt bigInt3 = bigInt - bigInt2;
                for (int l = 0; l < num9; l++)
                {
                    array2[num6 - l] = bigInt3.data[bi2.dataLength - l];
                }
                array[num5++] = (uint)num11;
                num6--;
                j--;
            }
            outQuotient.dataLength = num5;
            int m = 0;
            int n = outQuotient.dataLength - 1;
            while (n >= 0)
            {
                outQuotient.data[m] = array[n];
                n--;
                m++;
            }
            while (m < 300)
            {
                outQuotient.data[m] = 0u;
                m++;
            }
            while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0u)
            {
                outQuotient.dataLength--;
            }
            if (outQuotient.dataLength == 0)
            {
                outQuotient.dataLength = 1;
            }
            outRemainder.dataLength = BigInt.shiftRight(array2, num4);
            for (m = 0; m < outRemainder.dataLength; m++)
            {
                outRemainder.data[m] = array2[m];
            }
            while (m < 300)
            {
                outRemainder.data[m] = 0u;
                m++;
            }
        }
        private static void singleByteDivide(BigInt bi1, BigInt bi2, BigInt outQuotient, BigInt outRemainder)
        {
            uint[] array = new uint[300];
            int num = 0;
            for (int i = 0; i < 300; i++)
            {
                outRemainder.data[i] = bi1.data[i];
            }
            outRemainder.dataLength = bi1.dataLength;
            while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0u)
            {
                outRemainder.dataLength--;
            }
            ulong num2 = (ulong)bi2.data[0];
            int j = outRemainder.dataLength - 1;
            ulong num3 = (ulong)outRemainder.data[j];
            if (num3 >= num2)
            {
                ulong num4 = num3 / num2;
                array[num++] = (uint)num4;
                outRemainder.data[j] = (uint)(num3 % num2);
            }
            j--;
            while (j >= 0)
            {
                num3 = ((ulong)outRemainder.data[j + 1] << 32) + (ulong)outRemainder.data[j];
                ulong num5 = num3 / num2;
                array[num++] = (uint)num5;
                outRemainder.data[j + 1] = 0u;
                outRemainder.data[j--] = (uint)(num3 % num2);
            }
            outQuotient.dataLength = num;
            int k = 0;
            int l = outQuotient.dataLength - 1;
            while (l >= 0)
            {
                outQuotient.data[k] = array[l];
                l--;
                k++;
            }
            while (k < 300)
            {
                outQuotient.data[k] = 0u;
                k++;
            }
            while (outQuotient.dataLength > 1 && outQuotient.data[outQuotient.dataLength - 1] == 0u)
            {
                outQuotient.dataLength--;
            }
            if (outQuotient.dataLength == 0)
            {
                outQuotient.dataLength = 1;
            }
            while (outRemainder.dataLength > 1 && outRemainder.data[outRemainder.dataLength - 1] == 0u)
            {
                outRemainder.dataLength--;
            }
        }
        public static BigInt operator /(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            BigInt outRemainder = new BigInt();
            int num = 299;
            bool flag = false;
            bool flag2 = false;
            if ((bi1.data[num] & 2147483648u) != 0u)
            {
                bi1 = -bi1;
                flag2 = true;
            }
            if ((bi2.data[num] & 2147483648u) != 0u)
            {
                bi2 = -bi2;
                flag = true;
            }
            if (bi1 < bi2)
            {
                return bigInt;
            }
            if (bi2.dataLength == 1)
            {
                BigInt.singleByteDivide(bi1, bi2, bigInt, outRemainder);
            }
            else
            {
                BigInt.multiByteDivide(bi1, bi2, bigInt, outRemainder);
            }
            if (flag2 != flag)
            {
                return -bigInt;
            }
            return bigInt;
        }
        public static BigInt operator %(BigInt bi1, BigInt bi2)
        {
            BigInt outQuotient = new BigInt();
            BigInt bigInt = new BigInt(bi1);
            int num = 299;
            bool flag = false;
            if ((bi1.data[num] & 2147483648u) != 0u)
            {
                bi1 = -bi1;
                flag = true;
            }
            if ((bi2.data[num] & 2147483648u) != 0u)
            {
                bi2 = -bi2;
            }
            if (bi1 < bi2)
            {
                return bigInt;
            }
            if (bi2.dataLength == 1)
            {
                BigInt.singleByteDivide(bi1, bi2, outQuotient, bigInt);
            }
            else
            {
                BigInt.multiByteDivide(bi1, bi2, outQuotient, bigInt);
            }
            if (flag)
            {
                return -bigInt;
            }
            return bigInt;
        }
        public static BigInt operator &(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            int num = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
            for (int i = 0; i < num; i++)
            {
                uint num2 = bi1.data[i] & bi2.data[i];
                bigInt.data[i] = num2;
            }
            bigInt.dataLength = 300;
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            return bigInt;
        }
        public static BigInt operator |(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            int num = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
            for (int i = 0; i < num; i++)
            {
                uint num2 = bi1.data[i] | bi2.data[i];
                bigInt.data[i] = num2;
            }
            bigInt.dataLength = 300;
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            return bigInt;
        }
        public static BigInt operator ^(BigInt bi1, BigInt bi2)
        {
            BigInt bigInt = new BigInt();
            int num = (bi1.dataLength > bi2.dataLength) ? bi1.dataLength : bi2.dataLength;
            for (int i = 0; i < num; i++)
            {
                uint num2 = bi1.data[i] ^ bi2.data[i];
                bigInt.data[i] = num2;
            }
            bigInt.dataLength = 300;
            while (bigInt.dataLength > 1 && bigInt.data[bigInt.dataLength - 1] == 0u)
            {
                bigInt.dataLength--;
            }
            return bigInt;
        }
        internal BigInt max(BigInt bi)
        {
            if (this > bi)
            {
                return new BigInt(this);
            }
            return new BigInt(bi);
        }
        internal BigInt min(BigInt bi)
        {
            if (this < bi)
            {
                return new BigInt(this);
            }
            return new BigInt(bi);
        }
        internal BigInt abs()
        {
            if ((this.data[299] & 2147483648u) != 0u)
            {
                return -this;
            }
            return new BigInt(this);
        }
        public override string ToString()
        {
            return this.ToString(10);
        }
        internal string ToString(int radix)
        {
            if (radix < 2 || radix > 36)
            {
                throw new ArgumentException("Radix must be between 2 and 36");
            }
            string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string text2 = "";
            BigInt bigInt = this;
            bool flag = false;
            if ((bigInt.data[299] & 2147483648u) != 0u)
            {
                flag = true;
                try
                {
                    bigInt = -bigInt;
                }
                catch (Exception)
                {
                }
            }
            BigInt bigInt2 = new BigInt();
            BigInt bigInt3 = new BigInt();
            BigInt bi = new BigInt((long)radix);
            if (bigInt.dataLength == 1 && bigInt.data[0] == 0u)
            {
                text2 = "0";
            }
            else
            {
                while (bigInt.dataLength > 1 || (bigInt.dataLength == 1 && bigInt.data[0] != 0u))
                {
                    BigInt.singleByteDivide(bigInt, bi, bigInt2, bigInt3);
                    if (bigInt3.data[0] < 10u)
                    {
                        text2 = bigInt3.data[0] + text2;
                    }
                    else
                    {
                        text2 = text[(int)(bigInt3.data[0] - 10u)] + text2;
                    }
                    bigInt = bigInt2;
                }
                if (flag)
                {
                    text2 = "-" + text2;
                }
            }
            return text2;
        }
        internal string ToHexString()
        {
            string text = this.data[this.dataLength - 1].ToString("X");
            for (int i = this.dataLength - 2; i >= 0; i--)
            {
                text += this.data[i].ToString("X8");
            }
            return text;
        }
        internal BigInt modPow(BigInt exp, BigInt n)
        {
            if ((exp.data[299] & 2147483648u) != 0u)
            {
                throw new ArithmeticException("Exponent should be positive.");
            }
            BigInt bigInt = 1;
            bool flag = false;
            BigInt bigInt2;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt2 = -this % n;
                flag = true;
            }
            else
            {
                bigInt2 = this % n;
            }
            if ((n.data[299] & 2147483648u) != 0u)
            {
                n = -n;
            }
            BigInt bigInt3 = new BigInt();
            int num = n.dataLength << 1;
            bigInt3.data[num] = 1u;
            bigInt3.dataLength = num + 1;
            bigInt3 /= n;
            int num2 = exp.bitCount();
            int num3 = 0;
            for (int i = 0; i < exp.dataLength; i++)
            {
                uint num4 = 1u;
                int j = 0;
                while (j < 32)
                {
                    if ((exp.data[i] & num4) != 0u)
                    {
                        bigInt = this.BarrettReduction(bigInt * bigInt2, n, bigInt3);
                    }
                    num4 <<= 1;
                    bigInt2 = this.BarrettReduction(bigInt2 * bigInt2, n, bigInt3);
                    if (bigInt2.dataLength == 1 && bigInt2.data[0] == 1u)
                    {
                        if (flag && (exp.data[0] & 1u) != 0u)
                        {
                            return -bigInt;
                        }
                        return bigInt;
                    }
                    else
                    {
                        num3++;
                        if (num3 == num2)
                        {
                            break;
                        }
                        j++;
                    }
                }
            }
            if (flag && (exp.data[0] & 1u) != 0u)
            {
                return -bigInt;
            }
            return bigInt;
        }

        private BigInt BarrettReduction(BigInt x, BigInt n, BigInt constant)
        {
            int num = n.dataLength;
            int num2 = num + 1;
            int num3 = num - 1;
            BigInt bigInt = new BigInt();
            int i = num3;
            int num4 = 0;
            while (i < x.dataLength)
            {
                bigInt.data[num4] = x.data[i];
                i++;
                num4++;
            }
            bigInt.dataLength = x.dataLength - num3;
            if (bigInt.dataLength <= 0)
            {
                bigInt.dataLength = 1;
            }
            BigInt bigInt2 = bigInt * constant;
            BigInt bigInt3 = new BigInt();
            int j = num2;
            int num5 = 0;
            while (j < bigInt2.dataLength)
            {
                bigInt3.data[num5] = bigInt2.data[j];
                j++;
                num5++;
            }
            bigInt3.dataLength = bigInt2.dataLength - num2;
            if (bigInt3.dataLength <= 0)
            {
                bigInt3.dataLength = 1;
            }
            BigInt bigInt4 = new BigInt();
            int num6 = (x.dataLength > num2) ? num2 : x.dataLength;
            for (int k = 0; k < num6; k++)
            {
                bigInt4.data[k] = x.data[k];
            }
            bigInt4.dataLength = num6;
            BigInt bigInt5 = new BigInt();
            for (int l = 0; l < bigInt3.dataLength; l++)
            {
                if (bigInt3.data[l] != 0u)
                {
                    ulong num7 = 0uL;
                    int num8 = l;
                    int num9 = 0;
                    while (num9 < n.dataLength && num8 < num2)
                    {
                        ulong num10 = (ulong)bigInt3.data[l] * (ulong)n.data[num9] + (ulong)bigInt5.data[num8] + num7;
                        bigInt5.data[num8] = (uint)(num10 & 18446744073709551615uL);
                        num7 = num10 >> 32;
                        num9++;
                        num8++;
                    }
                    if (num8 < num2)
                    {
                        bigInt5.data[num8] = (uint)num7;
                    }
                }
            }
            bigInt5.dataLength = num2;
            while (bigInt5.dataLength > 1 && bigInt5.data[bigInt5.dataLength - 1] == 0u)
            {
                bigInt5.dataLength--;
            }
            bigInt4 -= bigInt5;
            if ((bigInt4.data[299] & 2147483648u) != 0u)
            {
                BigInt bigInt6 = new BigInt();
                bigInt6.data[num2] = 1u;
                bigInt6.dataLength = num2 + 1;
                bigInt4 += bigInt6;
            }
            while (bigInt4 >= n)
            {
                bigInt4 -= n;
            }
            return bigInt4;
        }

        internal BigInt gcd(BigInt bi)
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            BigInt bigInt2;
            if ((bi.data[299] & 2147483648u) != 0u)
            {
                bigInt2 = -bi;
            }
            else
            {
                bigInt2 = bi;
            }
            BigInt bigInt3 = bigInt2;
            while (bigInt.dataLength > 1 || (bigInt.dataLength == 1 && bigInt.data[0] != 0u))
            {
                bigInt3 = bigInt;
                bigInt = bigInt2 % bigInt;
                bigInt2 = bigInt3;
            }
            return bigInt3;
        }

        internal void genRandomBits(int bits, Random rand)
        {
            int num = bits >> 5;
            int num2 = bits & 31;
            if (num2 != 0)
            {
                num++;
            }
            if (num > 300)
            {
                throw new ArithmeticException("Number of bits > 300.");
            }
            for (int i = 0; i < num; i++)
            {
                this.data[i] = (uint)(rand.NextDouble() * 4294967296.0);
            }
            for (int j = num; j < 300; j++)
            {
                this.data[j] = 0u;
            }
            if (num2 != 0)
            {
                uint num3 = 1u << num2 - 1;
                this.data[num - 1] |= num3;
                num3 = 4294967295u >> 32 - num2;
                this.data[num - 1] &= num3;
            }
            else
            {
                this.data[num - 1] |= 2147483648u;
            }
            this.dataLength = num;
            if (this.dataLength == 0)
            {
                this.dataLength = 1;
            }
        }

        internal bool FermatLittleTest(int confidence)
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            if (bigInt.dataLength == 1)
            {
                if (bigInt.data[0] == 0u || bigInt.data[0] == 1u)
                {
                    return false;
                }
                if (bigInt.data[0] == 2u || bigInt.data[0] == 3u)
                {
                    return true;
                }
            }
            if ((bigInt.data[0] & 1u) == 0u)
            {
                return false;
            }
            int num = bigInt.bitCount();
            BigInt bigInt2 = new BigInt();
            BigInt exp = bigInt - new BigInt(1L);
            Random random = new Random();
            for (int i = 0; i < confidence; i++)
            {
                bool flag = false;
                while (!flag)
                {
                    int j;
                    for (j = 0; j < 2; j = (int)(random.NextDouble() * (double)num))
                    {
                    }
                    bigInt2.genRandomBits(j, random);
                    int num2 = bigInt2.dataLength;
                    if (num2 > 1 || (num2 == 1 && bigInt2.data[0] != 1u))
                    {
                        flag = true;
                    }
                }
                BigInt bigInt3 = bigInt2.gcd(bigInt);
                if (bigInt3.dataLength == 1 && bigInt3.data[0] != 1u)
                {
                    return false;
                }
                BigInt bigInt4 = bigInt2.modPow(exp, bigInt);
                int num3 = bigInt4.dataLength;
                if (num3 > 1 || (num3 == 1 && bigInt4.data[0] != 1u))
                {
                    return false;
                }
            }
            return true;
        }
        internal bool RabinMillerTest(int confidence)
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            if (bigInt.dataLength == 1)
            {
                if (bigInt.data[0] == 0u || bigInt.data[0] == 1u)
                {
                    return false;
                }
                if (bigInt.data[0] == 2u || bigInt.data[0] == 3u)
                {
                    return true;
                }
            }
            if ((bigInt.data[0] & 1u) == 0u)
            {
                return false;
            }
            BigInt bigInt2 = bigInt - new BigInt(1L);
            int num = 0;
            for (int i = 0; i < bigInt2.dataLength; i++)
            {
                uint num2 = 1u;
                for (int j = 0; j < 32; j++)
                {
                    if ((bigInt2.data[i] & num2) != 0u)
                    {
                        i = bigInt2.dataLength;
                        break;
                    }
                    num2 <<= 1;
                    num++;
                }
            }
            BigInt exp = bigInt2 >> num;
            int num3 = bigInt.bitCount();
            BigInt bigInt3 = new BigInt();
            Random random = new Random();
            for (int k = 0; k < confidence; k++)
            {
                bool flag = false;
                while (!flag)
                {
                    int l;
                    for (l = 0; l < 2; l = (int)(random.NextDouble() * (double)num3))
                    {
                    }
                    bigInt3.genRandomBits(l, random);
                    int num4 = bigInt3.dataLength;
                    if (num4 > 1 || (num4 == 1 && bigInt3.data[0] != 1u))
                    {
                        flag = true;
                    }
                }
                BigInt bigInt4 = bigInt3.gcd(bigInt);
                if (bigInt4.dataLength == 1 && bigInt4.data[0] != 1u)
                {
                    return false;
                }
                BigInt bigInt5 = bigInt3.modPow(exp, bigInt);
                bool flag2 = false;
                if (bigInt5.dataLength == 1 && bigInt5.data[0] == 1u)
                {
                    flag2 = true;
                }
                int num5 = 0;
                while (!flag2 && num5 < num)
                {
                    if (bigInt5 == bigInt2)
                    {
                        flag2 = true;
                        break;
                    }
                    bigInt5 = bigInt5 * bigInt5 % bigInt;
                    num5++;
                }
                if (!flag2)
                {
                    return false;
                }
            }
            return true;
        }
        internal bool SolovayStrassenTest(int confidence)
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            if (bigInt.dataLength == 1)
            {
                if (bigInt.data[0] == 0u || bigInt.data[0] == 1u)
                {
                    return false;
                }
                if (bigInt.data[0] == 2u || bigInt.data[0] == 3u)
                {
                    return true;
                }
            }
            if ((bigInt.data[0] & 1u) == 0u)
            {
                return false;
            }
            int num = bigInt.bitCount();
            BigInt bigInt2 = new BigInt();
            BigInt bigInt3 = bigInt - 1;
            BigInt exp = bigInt3 >> 1;
            Random random = new Random();
            for (int i = 0; i < confidence; i++)
            {
                bool flag = false;
                while (!flag)
                {
                    int j;
                    for (j = 0; j < 2; j = (int)(random.NextDouble() * (double)num))
                    {
                    }
                    bigInt2.genRandomBits(j, random);
                    int num2 = bigInt2.dataLength;
                    if (num2 > 1 || (num2 == 1 && bigInt2.data[0] != 1u))
                    {
                        flag = true;
                    }
                }
                BigInt bigInt4 = bigInt2.gcd(bigInt);
                if (bigInt4.dataLength == 1 && bigInt4.data[0] != 1u)
                {
                    return false;
                }
                BigInt bi = bigInt2.modPow(exp, bigInt);
                if (bi == bigInt3)
                {
                    bi = -1;
                }
                BigInt bi2 = BigInt.Jacobi(bigInt2, bigInt);
                if (bi != bi2)
                {
                    return false;
                }
            }
            return true;
        }
        internal bool LucasStrongTest()
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            if (bigInt.dataLength == 1)
            {
                if (bigInt.data[0] == 0u || bigInt.data[0] == 1u)
                {
                    return false;
                }
                if (bigInt.data[0] == 2u || bigInt.data[0] == 3u)
                {
                    return true;
                }
            }
            return (bigInt.data[0] & 1u) != 0u && this.LucasStrongTestHelper(bigInt);
        }
        internal bool LucasStrongTestHelper(BigInt thisVal)
        {
            long num = 5L;
            long num2 = -1L;
            long num3 = 0L;
            bool flag = false;
            while (!flag)
            {
                int num4 = BigInt.Jacobi(num, thisVal);
                if (num4 == -1)
                {
                    flag = true;
                }
                else
                {
                    if (num4 == 0 && Math.Abs(num) < thisVal)
                    {
                        return false;
                    }
                    if (num3 == 20L)
                    {
                        BigInt bigInt = thisVal.sqrt();
                        if (bigInt * bigInt == thisVal)
                        {
                            return false;
                        }
                    }
                    num = (Math.Abs(num) + 2L) * num2;
                    num2 = -num2;
                }
                num3 += 1L;
            }
            long num5 = 1L - num >> 2;
            BigInt bigInt2 = thisVal + 1;
            int num6 = 0;
            for (int i = 0; i < bigInt2.dataLength; i++)
            {
                uint num7 = 1u;
                for (int j = 0; j < 32; j++)
                {
                    if ((bigInt2.data[i] & num7) != 0u)
                    {
                        i = bigInt2.dataLength;
                        break;
                    }
                    num7 <<= 1;
                    num6++;
                }
            }
            BigInt k = bigInt2 >> num6;
            BigInt bigInt3 = new BigInt();
            int num8 = thisVal.dataLength << 1;
            bigInt3.data[num8] = 1u;
            bigInt3.dataLength = num8 + 1;
            bigInt3 /= thisVal;
            BigInt[] array = BigInt.LucasSequenceHelper(1, num5, k, thisVal, bigInt3, 0);
            bool flag2 = false;
            if ((array[0].dataLength == 1 && array[0].data[0] == 0u) || (array[1].dataLength == 1 && array[1].data[0] == 0u))
            {
                flag2 = true;
            }
            for (int l = 1; l < num6; l++)
            {
                if (!flag2)
                {
                    array[1] = thisVal.BarrettReduction(array[1] * array[1], thisVal, bigInt3);
                    array[1] = (array[1] - (array[2] << 1)) % thisVal;
                    if (array[1].dataLength == 1 && array[1].data[0] == 0u)
                    {
                        flag2 = true;
                    }
                }
                array[2] = thisVal.BarrettReduction(array[2] * array[2], thisVal, bigInt3);
            }
            if (flag2)
            {
                BigInt bigInt4 = thisVal.gcd(num5);
                if (bigInt4.dataLength == 1 && bigInt4.data[0] == 1u)
                {
                    if ((array[2].data[299] & 2147483648u) != 0u)
                    {
                        BigInt[] array2;
                        (array2 = array)[2] = array2[2] + thisVal;
                    }
                    BigInt bigInt5 = num5 * (long)BigInt.Jacobi(num5, thisVal) % thisVal;
                    if ((bigInt5.data[299] & 2147483648u) != 0u)
                    {
                        bigInt5 += thisVal;
                    }
                    if (array[2] != bigInt5)
                    {
                        flag2 = false;
                    }
                }
            }
            return flag2;
        }
        internal bool isProbablePrime(int confidence)
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            for (int i = 0; i < BigInt.primesBelow2000.Length; i++)
            {
                BigInt bigInt2 = BigInt.primesBelow2000[i];
                if (bigInt2 >= bigInt)
                {
                    break;
                }
                BigInt bigInt3 = bigInt % bigInt2;
                if (bigInt3.IntValue() == 0)
                {
                    return false;
                }
            }
            return bigInt.RabinMillerTest(confidence);
        }
        internal bool isProbablePrime()
        {
            BigInt bigInt;
            if ((this.data[299] & 2147483648u) != 0u)
            {
                bigInt = -this;
            }
            else
            {
                bigInt = this;
            }
            if (bigInt.dataLength == 1)
            {
                if (bigInt.data[0] == 0u || bigInt.data[0] == 1u)
                {
                    return false;
                }
                if (bigInt.data[0] == 2u || bigInt.data[0] == 3u)
                {
                    return true;
                }
            }
            if ((bigInt.data[0] & 1u) == 0u)
            {
                return false;
            }
            for (int i = 0; i < BigInt.primesBelow2000.Length; i++)
            {
                BigInt bigInt2 = BigInt.primesBelow2000[i];
                if (bigInt2 >= bigInt)
                {
                    break;
                }
                BigInt bigInt3 = bigInt % bigInt2;
                if (bigInt3.IntValue() == 0)
                {
                    return false;
                }
            }
            BigInt bigInt4 = bigInt - new BigInt(1L);
            int num = 0;
            for (int j = 0; j < bigInt4.dataLength; j++)
            {
                uint num2 = 1u;
                for (int k = 0; k < 32; k++)
                {
                    if ((bigInt4.data[j] & num2) != 0u)
                    {
                        j = bigInt4.dataLength;
                        break;
                    }
                    num2 <<= 1;
                    num++;
                }
            }
            BigInt exp = bigInt4 >> num;
            bigInt.bitCount();
            BigInt bigInt5 = 2;
            BigInt bigInt6 = bigInt5.modPow(exp, bigInt);
            bool flag = false;
            if (bigInt6.dataLength == 1 && bigInt6.data[0] == 1u)
            {
                flag = true;
            }
            int num3 = 0;
            while (!flag && num3 < num)
            {
                if (bigInt6 == bigInt4)
                {
                    flag = true;
                    break;
                }
                bigInt6 = bigInt6 * bigInt6 % bigInt;
                num3++;
            }
            if (flag)
            {
                flag = this.LucasStrongTestHelper(bigInt);
            }
            return flag;
        }
        internal int IntValue()
        {
            return (int)this.data[0];
        }
        internal long LongValue()
        {
            long num = 0L;
            num = (long)((ulong)this.data[0]);
            try
            {
                num |= (long)((long)((ulong)this.data[1]) << 32);
            }
            catch (Exception)
            {
                if ((this.data[0] & 2147483648u) != 0u)
                {
                    num = (long)((ulong)this.data[0]);
                }
            }
            return num;
        }
        internal static int Jacobi(BigInt a, BigInt b)
        {
            if ((b.data[0] & 1u) == 0u)
            {
                throw new ArgumentException("Jacobi deals with only odd integers.");
            }
            if (a >= b)
            {
                a %= b;
            }
            if (a.dataLength == 1 && a.data[0] == 0u)
            {
                return 0;
            }
            if (a.dataLength == 1 && a.data[0] == 1u)
            {
                return 1;
            }
            if (a < 0)
            {
                if (((b - 1).data[0] & 2u) == 0u)
                {
                    return BigInt.Jacobi(-a, b);
                }
                return -BigInt.Jacobi(-a, b);
            }
            else
            {
                int num = 0;
                for (int i = 0; i < a.dataLength; i++)
                {
                    uint num2 = 1u;
                    for (int j = 0; j < 32; j++)
                    {
                        if ((a.data[i] & num2) != 0u)
                        {
                            i = a.dataLength;
                            break;
                        }
                        num2 <<= 1;
                        num++;
                    }
                }
                BigInt bigInt = a >> num;
                int num3 = 1;
                if ((num & 1) != 0 && ((b.data[0] & 7u) == 3u || (b.data[0] & 7u) == 5u))
                {
                    num3 = -1;
                }
                if ((b.data[0] & 3u) == 3u && (bigInt.data[0] & 3u) == 3u)
                {
                    num3 = -num3;
                }
                if (bigInt.dataLength == 1 && bigInt.data[0] == 1u)
                {
                    return num3;
                }
                return num3 * BigInt.Jacobi(b % bigInt, bigInt);
            }
        }
        internal static BigInt genPseudoPrime(int bits, int confidence, Random rand)
        {
            BigInt bigInt = new BigInt();
            bool flag = false;
            while (!flag)
            {
                bigInt.genRandomBits(bits, rand);
                bigInt.data[0] |= 1u;
                flag = bigInt.isProbablePrime(confidence);
            }
            return bigInt;
        }
        internal static BigInt genPseudoPrime(int bits, Random rand)
        {
            BigInt bigInt = new BigInt();
            bool flag = false;
            while (!flag)
            {
                bigInt.genRandomBits(bits, rand);
                bigInt.data[0] |= 1u;
                flag = bigInt.isProbablePrime();
            }
            return bigInt;
        }
        internal BigInt genCoPrime(int bits, Random rand)
        {
            bool flag = false;
            BigInt bigInt = new BigInt();
            while (!flag)
            {
                bigInt.genRandomBits(bits, rand);
                BigInt bigInt2 = bigInt.gcd(this);
                if (bigInt2.dataLength == 1 && bigInt2.data[0] == 1u)
                {
                    flag = true;
                }
            }
            return bigInt;
        }
        internal BigInt modInverse(BigInt modulus)
        {
            BigInt[] array = new BigInt[]
			{
				0,
				1
			};
            BigInt[] array2 = new BigInt[2];
            BigInt[] array3 = new BigInt[]
			{
				0,
				0
			};
            int num = 0;
            BigInt bi = modulus;
            BigInt bigInt = this;
            while (bigInt.dataLength > 1 || (bigInt.dataLength == 1 && bigInt.data[0] != 0u))
            {
                BigInt bigInt2 = new BigInt();
                BigInt bigInt3 = new BigInt();
                if (num > 1)
                {
                    BigInt bigInt4 = (array[0] - array[1] * array2[0]) % modulus;
                    array[0] = array[1];
                    array[1] = bigInt4;
                }
                if (bigInt.dataLength == 1)
                {
                    BigInt.singleByteDivide(bi, bigInt, bigInt2, bigInt3);
                }
                else
                {
                    BigInt.multiByteDivide(bi, bigInt, bigInt2, bigInt3);
                }
                array2[0] = array2[1];
                array3[0] = array3[1];
                array2[1] = bigInt2;
                array3[1] = bigInt3;
                bi = bigInt;
                bigInt = bigInt3;
                num++;
            }
            if (array3[0].dataLength > 1 || (array3[0].dataLength == 1 && array3[0].data[0] != 1u))
            {
                throw new ArithmeticException("No inverse.");
            }
            BigInt bigInt5 = (array[0] - array[1] * array2[0]) % modulus;
            if ((bigInt5.data[299] & 2147483648u) != 0u)
            {
                bigInt5 += modulus;
            }
            return bigInt5;
        }
        internal int bitCount()
        {
            while (this.dataLength > 1 && this.data[this.dataLength - 1] == 0u)
            {
                this.dataLength--;
            }
            uint num = this.data[this.dataLength - 1];
            uint num2 = 2147483648u;
            int num3 = 32;
            while (num3 > 0 && (num & num2) == 0u)
            {
                num3--;
                num2 >>= 1;
            }
            return num3 + (this.dataLength - 1 << 5);
        }
        internal int bitCountRaw()
        {
            uint arg_0F_0 = this.data[this.dataLength - 1];
            int num = 32;
            return num + (this.dataLength - 1 << 5);
        }
        internal byte[] getBytes()
        {
            int num = this.bitCount();
            byte[] array2;
            if (num == 0)
            {
                byte[] array = new byte[1];
                array2 = array;
            }
            else
            {
                int num2 = num >> 3;
                if ((num & 7) != 0)
                {
                    num2++;
                }
                array2 = new byte[num2];
                int num3 = num2 & 3;
                if (num3 == 0)
                {
                    num3 = 4;
                }
                int num4 = 0;
                for (int i = this.dataLength - 1; i >= 0; i--)
                {
                    uint num5 = this.data[i];
                    for (int j = num3 - 1; j >= 0; j--)
                    {
                        array2[num4 + j] = (byte)(num5 & 255u);
                        num5 >>= 8;
                    }
                    num4 += num3;
                    num3 = 4;
                }
            }
            return array2;
        }
        public byte[] getBytesRaw()
        {
            int num = this.bitCountRaw();
            byte[] array2;
            if (num == 0)
            {
                byte[] array = new byte[1];
                array2 = array;
            }
            else
            {
                int num2 = num >> 3;
                if ((num & 7) != 0)
                {
                    num2++;
                }
                array2 = new byte[num2];
                int num3 = num2 & 3;
                if (num3 == 0)
                {
                    num3 = 4;
                }
                int num4 = 0;
                for (int i = this.dataLength - 1; i >= 0; i--)
                {
                    uint num5 = this.data[i];
                    for (int j = num3 - 1; j >= 0; j--)
                    {
                        array2[num4 + j] = (byte)(num5 & 255u);
                        num5 >>= 8;
                    }
                    num4 += num3;
                    num3 = 4;
                }
            }
            return array2;
        }
        internal void setBit(uint bitNum)
        {
            uint num = bitNum >> 5;
            byte b = (byte)(bitNum & 31u);
            uint num2 = 1u << (int)b;
            this.data[(int)((uint)((UIntPtr)num))] |= num2;
            if ((ulong)num >= (ulong)((long)this.dataLength))
            {
                this.dataLength = (int)(num + 1u);
            }
        }
        internal void unsetBit(uint bitNum)
        {
            uint num = bitNum >> 5;
            if ((ulong)num < (ulong)((long)this.dataLength))
            {
                byte b = (byte)(bitNum & 31u);
                uint num2 = 1u << (int)b;
                uint num3 = 4294967295u ^ num2;
                this.data[(int)((uint)((UIntPtr)num))] &= num3;
                if (this.dataLength > 1 && this.data[this.dataLength - 1] == 0u)
                {
                    this.dataLength--;
                }
            }
        }
        internal BigInt sqrt()
        {
            uint num = (uint)this.bitCount();
            if ((num & 1u) != 0u)
            {
                num = (num >> 1) + 1u;
            }
            else
            {
                num >>= 1;
            }
            uint num2 = num >> 5;
            byte b = (byte)(num & 31u);
            BigInt bigInt = new BigInt();
            uint num3;
            if (b == 0)
            {
                num3 = 2147483648u;
            }
            else
            {
                num3 = 1u << (int)b;
                num2 += 1u;
            }
            bigInt.dataLength = (int)num2;
            for (int i = (int)(num2 - 1u); i >= 0; i--)
            {
                while (num3 != 0u)
                {
                    bigInt.data[i] ^= num3;
                    if (bigInt * bigInt > this)
                    {
                        bigInt.data[i] ^= num3;
                    }
                    num3 >>= 1;
                }
                num3 = 2147483648u;
            }
            return bigInt;
        }
        internal static BigInt[] LucasSequence(BigInt P, BigInt Q, BigInt k, BigInt n)
        {
            if (k.dataLength == 1 && k.data[0] == 0u)
            {
                return new BigInt[]
				{
					0,
					2 % n,
					1 % n
				};
            }
            BigInt bigInt = new BigInt();
            int num = n.dataLength << 1;
            bigInt.data[num] = 1u;
            bigInt.dataLength = num + 1;
            bigInt /= n;
            int num2 = 0;
            for (int i = 0; i < k.dataLength; i++)
            {
                uint num3 = 1u;
                for (int j = 0; j < 32; j++)
                {
                    if ((k.data[i] & num3) != 0u)
                    {
                        i = k.dataLength;
                        break;
                    }
                    num3 <<= 1;
                    num2++;
                }
            }
            BigInt k2 = k >> num2;
            return BigInt.LucasSequenceHelper(P, Q, k2, n, bigInt, num2);
        }
        private static BigInt[] LucasSequenceHelper(BigInt P, BigInt Q, BigInt k, BigInt n, BigInt constant, int s)
        {
            BigInt[] array = new BigInt[3];
            if ((k.data[0] & 1u) == 0u)
            {
                throw new ArgumentException("k shoud be odd.");
            }
            int num = k.bitCount();
            uint num2 = 1u << (num & 31) - 1;
            BigInt bigInt = 2 % n;
            BigInt bigInt2 = 1 % n;
            BigInt bigInt3 = P % n;
            BigInt bigInt4 = bigInt2;
            bool flag = true;
            for (int i = k.dataLength - 1; i >= 0; i--)
            {
                while (num2 != 0u && (i != 0 || num2 != 1u))
                {
                    if ((k.data[i] & num2) != 0u)
                    {
                        bigInt4 = bigInt4 * bigInt3 % n;
                        bigInt = (bigInt * bigInt3 - P * bigInt2) % n;
                        bigInt3 = n.BarrettReduction(bigInt3 * bigInt3, n, constant);
                        bigInt3 = (bigInt3 - (bigInt2 * Q << 1)) % n;
                        if (flag)
                        {
                            flag = false;
                        }
                        else
                        {
                            bigInt2 = n.BarrettReduction(bigInt2 * bigInt2, n, constant);
                        }
                        bigInt2 = bigInt2 * Q % n;
                    }
                    else
                    {
                        bigInt4 = (bigInt4 * bigInt - bigInt2) % n;
                        bigInt3 = (bigInt * bigInt3 - P * bigInt2) % n;
                        bigInt = n.BarrettReduction(bigInt * bigInt, n, constant);
                        bigInt = (bigInt - (bigInt2 << 1)) % n;
                        if (flag)
                        {
                            bigInt2 = Q % n;
                            flag = false;
                        }
                        else
                        {
                            bigInt2 = n.BarrettReduction(bigInt2 * bigInt2, n, constant);
                        }
                    }
                    num2 >>= 1;
                }
                num2 = 2147483648u;
            }
            bigInt4 = (bigInt4 * bigInt - bigInt2) % n;
            bigInt = (bigInt * bigInt3 - P * bigInt2) % n;
            if (flag)
            {
                flag = false;
            }
            else
            {
                bigInt2 = n.BarrettReduction(bigInt2 * bigInt2, n, constant);
            }
            bigInt2 = bigInt2 * Q % n;
            for (int j = 0; j < s; j++)
            {
                bigInt4 = bigInt4 * bigInt % n;
                bigInt = (bigInt * bigInt - (bigInt2 << 1)) % n;
                if (flag)
                {
                    bigInt2 = Q % n;
                    flag = false;
                }
                else
                {
                    bigInt2 = n.BarrettReduction(bigInt2 * bigInt2, n, constant);
                }
            }
            array[0] = bigInt4;
            array[1] = bigInt;
            array[2] = bigInt2;
            return array;
        }
    }

}