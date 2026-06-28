using System;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>オフライン・無キー時のデモ用固定スナップショット列を生成する。</summary>
    public static class DummyWeather
    {
        // 40 点ぶんのコンディションを周期的に変化させ、見せ場を作る。
        static readonly WeatherCondition[] Pattern =
        {
            WeatherCondition.Clear, WeatherCondition.Clear, WeatherCondition.Cloudy,
            WeatherCondition.Rain, WeatherCondition.Storm, WeatherCondition.Rain,
            WeatherCondition.Cloudy, WeatherCondition.Snow
        };

        public static WeatherSnapshot[] Generate(string cityName, DateTime startJst)
        {
            var snaps = new WeatherSnapshot[40];
            for (int i = 0; i < 40; i++)
            {
                var cond = Pattern[i % Pattern.Length];
                snaps[i] = new WeatherSnapshot
                {
                    dateTime = startJst.AddHours(3 * i),
                    condition = cond,
                    cloudCoverage = CloudFor(cond),
                    windSpeed = 2f + (i % 5),
                    windDirectionDeg = (i * 30) % 360,
                    rainIntensity = cond == WeatherCondition.Rain ? 0.5f : (cond == WeatherCondition.Storm ? 0.9f : 0f),
                    temperatureCelsius = 15f + 8f * (float)Math.Sin(i * Math.PI / 8.0)
                };
            }
            return snaps;
        }

        static float CloudFor(WeatherCondition c) => c switch
        {
            WeatherCondition.Clear => 0.1f,
            WeatherCondition.Cloudy => 0.8f,
            WeatherCondition.Rain => 0.7f,
            WeatherCondition.Storm => 1.0f,
            WeatherCondition.Snow => 0.5f,
            _ => 0.5f
        };
    }
}
