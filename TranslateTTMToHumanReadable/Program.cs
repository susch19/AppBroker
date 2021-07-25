using System;
using System.Linq;
using System.Text;

namespace TranslateTTMToHumanReadable
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var s = Console.ReadLine();
                try
                {
                    s = s.Replace("-", "");
                    for (int i = 0; i < (s.Length / 6 * 6); i += 6)
                    {


                        var ttm = TimeTempMessageLE.LoadFromBinary(StringToByteArray(s[i..(i + 6)]));
                        Console.WriteLine($"Time:{ttm.Time} Temp:{ttm.Temp} Dow:{ttm.DayOfWeek}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            static byte[] StringToByteArray(string hex)
            {
                return Enumerable.Range(0, hex.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                 .ToArray();
            }
        }
    }
}
