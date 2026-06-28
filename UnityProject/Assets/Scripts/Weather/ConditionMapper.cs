using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap weather[0].id を WeatherCondition 5 区分へ分類する。</summary>
    public static class ConditionMapper
    {
        public static WeatherCondition FromOwmId(int id)
        {
            if (id >= 200 && id < 300) return WeatherCondition.Storm;
            if (id >= 300 && id < 600) return WeatherCondition.Rain;   // 3xx 霧雨 + 5xx 雨
            if (id >= 600 && id < 700) return WeatherCondition.Snow;
            if (id >= 700 && id < 800) return WeatherCondition.Cloudy;  // 大気現象
            if (id == 800) return WeatherCondition.Clear;
            if (id > 800 && id < 900) return WeatherCondition.Cloudy;   // 80x 雲量あり
            return WeatherCondition.Cloudy;                             // 未知は曇り扱い
        }
    }
}