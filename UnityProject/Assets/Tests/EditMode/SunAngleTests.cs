using NUnit.Framework;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class SunAngleTests
    {
        [TestCase(0f, -90f)]
        [TestCase(6f, 0f)]
        [TestCase(12f, 90f)]
        [TestCase(18f, 0f)]
        public void ElevationDeg_KeyHours(float hour, float expected)
        {
            Assert.AreEqual(expected, SunAngle.ElevationDeg(hour), 1e-3f);
        }

        [Test]
        public void ElevationDeg_NightIsNegative()
        {
            Assert.Less(SunAngle.ElevationDeg(3f), 0f);
            Assert.Less(SunAngle.ElevationDeg(21f), 0f);
        }
    }
}
