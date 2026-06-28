using System;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>隣接スナップショット間を線形補間する純関数。</summary>
    public static class SnapshotInterpolator
    {
        public static WeatherSnapshot Lerp(WeatherSnapshot a, WeatherSnapshot b, float t)
        {
            t = Mathf.Clamp01(t);
            long ticks = (long)(a.dateTime.Ticks + (b.dateTime.Ticks - a.dateTime.Ticks) * (double)t);
            return new WeatherSnapshot
            {
                dateTime = new DateTime(ticks),
                condition = t < 0.5f ? a.condition : b.condition,
                cloudCoverage = Mathf.Lerp(a.cloudCoverage, b.cloudCoverage, t),
                windSpeed = Mathf.Lerp(a.windSpeed, b.windSpeed, t),
                windDirectionDeg = Mathf.Lerp(a.windDirectionDeg, b.windDirectionDeg, t),
                rainIntensity = Mathf.Lerp(a.rainIntensity, b.rainIntensity, t),
                temperatureCelsius = Mathf.Lerp(a.temperatureCelsius, b.temperatureCelsius, t)
            };
        }
    }
}
