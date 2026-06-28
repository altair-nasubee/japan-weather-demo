using NUnit.Framework;
using System;
using System.Linq;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class DummyWeatherTests
    {
        [Test]
        public void Generate_Returns40Snapshots_3HoursApart()
        {
            var start = new DateTime(2024, 6, 28, 9, 0, 0);
            var snaps = DummyWeather.Generate("東京", start);
            Assert.AreEqual(40, snaps.Length);
            Assert.AreEqual(start, snaps[0].dateTime);
            Assert.AreEqual(start.AddHours(3), snaps[1].dateTime);
            Assert.AreEqual(start.AddHours(3 * 39), snaps[39].dateTime);
        }

        [Test]
        public void Generate_ContainsVariedConditions()
        {
            var snaps = DummyWeather.Generate("東京", new DateTime(2024, 6, 28, 9, 0, 0));
            int distinct = snaps.Select(s => s.condition).Distinct().Count();
            Assert.GreaterOrEqual(distinct, 3);
        }
    }
}
