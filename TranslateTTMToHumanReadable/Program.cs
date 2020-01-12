using System;

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
                    for (int i = 0; i < (s.Length / 4 * 4); i += 4)
                    {
                        var ttm = TimeTempMessageLE.FromBase64(s[i..(i + 4)]);
                        Console.WriteLine($"Time:{ttm.Time} Temp:{ttm.Temp} Dow:{ttm.DayOfWeek}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
