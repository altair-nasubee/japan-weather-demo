using NUnit.Framework;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class WeatherParserTests
    {
        const string Json = @"{
          ""list"": [
            { ""dt"": 1704067200, ""main"": { ""temp"": 5.5 },
              ""weather"": [ { ""id"": 800 } ], ""clouds"": { ""all"": 10 },
              ""wind"": { ""speed"": 3.2, ""deg"": 90 } },
            { ""dt"": 1704078000, ""main"": { ""temp"": 7.0 },
              ""weather"": [ { ""id"": 500 } ], ""clouds"": { ""all"": 80 },
              ""wind"": { ""speed"": 5.0, ""deg"": 180 }, ""rain"": { ""3h"": 5.0 } }
          ],
          ""city"": { ""name"": ""Tokyo"", ""timezone"": 32400 }
        }";

        [Test]
        public void Parse_ReturnsAllSnapshots()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(2, snaps.Length);
        }

        [Test]
        public void Parse_MapsConditionAndCloud()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(WeatherCondition.Clear, snaps[0].condition);
            Assert.AreEqual(0.1f, snaps[0].cloudCoverage, 1e-4f);
            Assert.AreEqual(WeatherCondition.Rain, snaps[1].condition);
            Assert.AreEqual(0.8f, snaps[1].cloudCoverage, 1e-4f);
        }

        [Test]
        public void Parse_NormalizesRain_AndDefaultsZero()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(0f, snaps[0].rainIntensity, 1e-4f);     // rain 無し → 0
            Assert.AreEqual(0.5f, snaps[1].rainIntensity, 1e-4f);   // 5mm/3h ÷ 10 = 0.5
        }

        [Test]
        public void Parse_ConvertsToJst()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(9, snaps[0].dateTime.Hour);  // 00:00Z → 09:00 JST
        }

        [Test]
        public void Parse_CopiesWindAndTemp()
        {
            var snaps = WeatherParser.Parse(Json);
            Assert.AreEqual(3.2f, snaps[0].windSpeed, 1e-4f);
            Assert.AreEqual(90f, snaps[0].windDirectionDeg, 1e-4f);
            Assert.AreEqual(5.5f, snaps[0].temperatureCelsius, 1e-4f);
        }
    }
}
