using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OWM forecast JSON を WeatherSnapshot[] に変換する純関数。</summary>
    public static class WeatherParser
    {
        public const float RainMaxMmPer3h = 10f;

        public static WeatherSnapshot[] Parse(string json)
        {
            var res = JsonConvert.DeserializeObject<OwmForecastResponse>(json);
            if (res?.List == null) return new WeatherSnapshot[0];

            var snaps = new List<WeatherSnapshot>(res.List.Count);
            foreach (var item in res.List)
            {
                int id = (item.Weather != null && item.Weather.Count > 0) ? item.Weather[0].Id : 800;
                float rain3h = item.Rain != null ? item.Rain.ThreeHour : 0f;

                snaps.Add(new WeatherSnapshot
                {
                    dateTime = TimeZoneUtil.UnixUtcToJst(item.Dt),
                    condition = ConditionMapper.FromOwmId(id),
                    cloudCoverage = Mathf.Clamp01((item.Clouds?.All ?? 0f) / 100f),
                    windSpeed = item.Wind?.Speed ?? 0f,
                    windDirectionDeg = item.Wind?.Deg ?? 0f,
                    rainIntensity = Mathf.Clamp01(rain3h / RainMaxMmPer3h),
                    temperatureCelsius = item.Main?.Temp ?? 0f
                });
            }
            return snaps.ToArray();
        }
    }
}
