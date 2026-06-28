using NUnit.Framework;
using System;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class SnapshotInterpolatorTests
    {
        static WeatherSnapshot A() => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 0, 0, 0),
            condition = WeatherCondition.Clear,
            cloudCoverage = 0f, windSpeed = 0f, rainIntensity = 0f, temperatureCelsius = 10f
        };
        static WeatherSnapshot B() => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 3, 0, 0),
            condition = WeatherCondition.Rain,
            cloudCoverage = 1f, windSpeed = 10f, rainIntensity = 1f, temperatureCelsius = 20f
        };

        [Test]
        public void Lerp_Midpoint_AveragesNumbers()
        {
            var m = SnapshotInterpolator.Lerp(A(), B(), 0.5f);
            Assert.AreEqual(0.5f, m.cloudCoverage, 1e-4f);
            Assert.AreEqual(5f, m.windSpeed, 1e-4f);
            Assert.AreEqual(15f, m.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void Lerp_ConditionSwitchesAtHalf()
        {
            Assert.AreEqual(WeatherCondition.Clear, SnapshotInterpolator.Lerp(A(), B(), 0.49f).condition);
            Assert.AreEqual(WeatherCondition.Rain, SnapshotInterpolator.Lerp(A(), B(), 0.5f).condition);
        }

        [Test]
        public void Lerp_InterpolatesDateTime()
        {
            var m = SnapshotInterpolator.Lerp(A(), B(), 0.5f);
            Assert.AreEqual(new DateTime(2024, 1, 1, 1, 30, 0), m.dateTime);
        }
    }
}
