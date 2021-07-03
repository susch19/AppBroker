using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using DayOfWeek = AppBokerASP.Devices.Heater.DayOfWeek;

namespace AppBokerASP.Devices.Heater
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TimeTempMessageLE
    {

        fixed byte data[3];

        //0000 0000
        public TimeTempMessageLE(DayOfWeek dayOfWeek, TimeSpan time, float temp) : this()
        {
            DayOfWeek = dayOfWeek;
            Time = time;
            Temp = temp;
        }

        public TimeTempMessageLE(byte[] bytes)
        {
            for (int i = 0; i < 3; i++)
                data[i] = bytes[i];
        }

        public DayOfWeek DayOfWeek
        {
            get => (DayOfWeek)(data[0] & 0x7); set => data[0] = (byte)(((int)value & 0x7) | (data[0] & 0xF8));
        }

        public TimeSpan Time
        {
            get => TimeSpan.FromMinutes((ushort)(((data[0] & 0xF8) >> 3) | ((data[1] & 0x3F) << 5)));
            set
            {
                var s = (ushort)value.TotalMinutes;
                //if (s > 1440)
                //    throw new NotSupportedException("More than 1440 Minutes or 24h is not supported");
                //0000 0111 1112 2222
                // >> 6
                //0000 0000 0001 1111 ---

                //data[1] = (byte)(((s >> 2) & 0xF8) | (data[0] & 0x7));
                //data[0] = (byte)((s & 0x3F) | (data[1] & 0xC0));
                data[0] = (byte)(((s << 3) & 0xF8) | (data[0] & 0x7));
                data[1] = (byte)(((s >> 5) & 0x3F) | (data[1] & 0xC0));
            }
        }
        public float Temp
        {
            get => UShortToFloat((ushort)(((data[1] >> 6) & 0x3) | (data[2] << 2)));
            set
            {
                var u = FloatToUShort(value);
                //if (u > 766)
                //    throw new NotSupportedException("This Value is to damn high");

                data[1] = (byte)(((u & 0x3) << 6) | (data[1] & 0x3F));
                data[2] = (byte)((u >> 2) & 0xFF);
            }
        }
        ushort FloatToUShort(float temp) => (ushort)((temp + 0.05f) * 10);

        float UShortToFloat(ushort temp) => temp / 10f;

        public string GetBits()
        {
            string s = "";
            for (int i = 0; i < 3; i++)
                s += Convert.ToString(data[i], 2).PadLeft(8, '0') + " ";
            return s;
        }

        public string GetBitsLE()
        {
            string s = "";
            for (int i = 0; i < 3; i++)
                s += Convert.ToString(data[i], 2).PadLeft(8, '0') + " ";
            return s;
        }

        public byte[] ToBinary()
        {
            return new byte[] { data[0], data[1], data[2], };
        }

        public void FromBinary(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 3)
                throw new ArgumentException(nameof(bytes));

            for (int i = 0; i < 3; i++)
            {
                data[i] = bytes[i];
            }
        }

        public static TimeTempMessageLE LoadFromBinary(ReadOnlySpan<byte> bytes)
        {
            var ttm = new TimeTempMessageLE();
            ttm.FromBinary(bytes);
            return ttm;
        }

        public override string ToString()
        {
            fixed (byte* ptr = data)
            {
                var bla = new ReadOnlySpan<byte>(ptr, 3);
                return Convert.ToBase64String(bla, Base64FormattingOptions.None);
            }
        }

        public static TimeTempMessageLE FromBase64(string s)
        {
            var bytes = Convert.FromBase64String(s);
            return new TimeTempMessageLE(bytes);
        }

        public static void Test()
        {
            for (int e = 0; e <= (byte)DayOfWeek.Sun; e++)
            {
                for (int i = 0; i < TimeSpan.FromDays(1).TotalMinutes; i++)
                {
                    for (int o = 0; o < 766; o++)
                    {
                        try
                        {

                            var ttm = new TimeTempMessageLE((DayOfWeek)e, TimeSpan.FromMinutes(i), o / 10f);
                            if (!(ttm.Time.TotalMinutes == i && ttm.DayOfWeek == (DayOfWeek)e && Math.Abs(ttm.Temp - o / 10f) < 0.001f))
                                Console.WriteLine($"Wrong input output I:{i} = M:{ttm.Time.TotalMinutes}  O:{o / 10f} = T:{ttm.Temp}  E:{e} = {(byte)ttm.DayOfWeek}");

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }


        }
    }
}
