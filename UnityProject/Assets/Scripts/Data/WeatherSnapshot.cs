using System;

namespace JapanWeatherDemo.Data
{
    [Serializable]
    public struct WeatherSnapshot
    {
        public DateTime dateTime;          // JST（UTC+9 に変換済み）
        public WeatherCondition condition; // Clear / Cloudy / Rain / Storm / Snow
        public float cloudCoverage;        // 0〜1
        public float windSpeed;            // m/s
        public float windDirectionDeg;     // 0〜360
        public float rainIntensity;        // 0〜1
        public float temperatureCelsius;
    }
}