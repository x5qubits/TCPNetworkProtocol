using System;
using System.Diagnostics;

namespace JHSNetProtocol
{
    public class JHSTime
    {
        private static Stopwatch sw = Stopwatch.StartNew();

        public static long TimeStamp => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        public static float Time => Convert.ToSingle(JHSTime.sw.Elapsed.TotalSeconds);
    }
}
