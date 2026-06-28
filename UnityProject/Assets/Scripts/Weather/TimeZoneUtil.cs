using System;

namespace JapanWeatherDemo.Weather
{
    /// <summary>UNIX UTC 秒を JST(UTC+9) の DateTime に変換する。</summary>
    public static class TimeZoneUtil
    {
        const int JstOffsetHours = 9;

        public static DateTime UnixUtcToJst(long unixSeconds)
        {
            DateTime utc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            return DateTime.SpecifyKind(utc.AddHours(JstOffsetHours), DateTimeKind.Unspecified);
        }
    }
}
