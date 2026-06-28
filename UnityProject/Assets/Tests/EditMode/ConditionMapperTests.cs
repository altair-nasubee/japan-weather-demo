using NUnit.Framework;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class ConditionMapperTests
    {
        [TestCase(200, WeatherCondition.Storm)]   // 2xx 雷雨
        [TestCase(232, WeatherCondition.Storm)]
        [TestCase(300, WeatherCondition.Rain)]    // 3xx 霧雨
        [TestCase(500, WeatherCondition.Rain)]    // 5xx 雨
        [TestCase(531, WeatherCondition.Rain)]
        [TestCase(600, WeatherCondition.Snow)]    // 6xx 雪
        [TestCase(622, WeatherCondition.Snow)]
        [TestCase(701, WeatherCondition.Cloudy)]  // 7xx 大気現象
        [TestCase(781, WeatherCondition.Cloudy)]
        [TestCase(800, WeatherCondition.Clear)]   // 快晴
        [TestCase(801, WeatherCondition.Cloudy)]  // 雲量あり
        [TestCase(804, WeatherCondition.Cloudy)]
        public void FromOwmId_MapsToExpected(int id, WeatherCondition expected)
        {
            Assert.AreEqual(expected, ConditionMapper.FromOwmId(id));
        }
    }
}