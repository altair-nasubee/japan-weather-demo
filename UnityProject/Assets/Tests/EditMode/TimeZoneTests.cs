using NUnit.Framework;
using System;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class TimeZoneTests
    {
        [Test]
        public void UnixUtcToJst_AddsNineHours()
        {
            // 2024-01-01T00:00:00Z = 1704067200。JST では 09:00。
            DateTime jst = TimeZoneUtil.UnixUtcToJst(1704067200L);
            Assert.AreEqual(2024, jst.Year);
            Assert.AreEqual(1, jst.Month);
            Assert.AreEqual(1, jst.Day);
            Assert.AreEqual(9, jst.Hour);
            Assert.AreEqual(0, jst.Minute);
        }

        [Test]
        public void UnixUtcToJst_CrossesDateBoundary()
        {
            // 2024-01-01T20:00:00Z = 1704139200。JST では 翌 05:00。
            DateTime jst = TimeZoneUtil.UnixUtcToJst(1704139200L);
            Assert.AreEqual(2, jst.Day);
            Assert.AreEqual(5, jst.Hour);
        }
    }
}
