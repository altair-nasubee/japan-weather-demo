using NUnit.Framework;
using System.Collections.Generic;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class CityCatalogTests
    {
        [Test]
        public void LoadFromJson_ParsesArray()
        {
            string json = "[{\"name\":\"東京\",\"lat\":35.6895,\"lon\":139.6917,\"prefecture\":\"東京都\"}," +
                          "{\"name\":\"札幌\",\"lat\":43.0642,\"lon\":141.3468,\"prefecture\":\"北海道\"}]";
            List<CityData> cities = CityCatalog.LoadFromJson(json);
            Assert.AreEqual(2, cities.Count);
            Assert.AreEqual("東京", cities[0].name);
            Assert.AreEqual(35.6895f, cities[0].lat, 1e-4f);
            Assert.AreEqual("北海道", cities[1].prefecture);
        }

        [Test]
        public void LoadFromJson_EmptyArray_ReturnsEmptyList()
        {
            Assert.AreEqual(0, CityCatalog.LoadFromJson("[]").Count);
        }
    }
}