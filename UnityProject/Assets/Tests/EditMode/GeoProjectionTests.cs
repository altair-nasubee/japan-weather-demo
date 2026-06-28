using NUnit.Framework;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class GeoProjectionTests
    {
        // 範囲: lat[20,40] lon[120,140] → plane X[-10,10] Z[-10,10]
        const float LatMin = 20f, LatMax = 40f, LonMin = 120f, LonMax = 140f;
        const float PX0 = -10f, PX1 = 10f, PZ0 = -10f, PZ1 = 10f;

        static Vector2 Project(float lat, float lon) =>
            GeoProjection.LatLonToXZ(lat, lon, LatMin, LatMax, LonMin, LonMax, PX0, PX1, PZ0, PZ1);

        [Test]
        public void SouthWestCorner_MapsTo_MinXMinZ()
        {
            Vector2 p = Project(LatMin, LonMin);
            Assert.AreEqual(-10f, p.x, 1e-4f);
            Assert.AreEqual(-10f, p.y, 1e-4f); // p.y は Z
        }

        [Test]
        public void NorthEastCorner_MapsTo_MaxXMaxZ()
        {
            Vector2 p = Project(LatMax, LonMax);
            Assert.AreEqual(10f, p.x, 1e-4f);
            Assert.AreEqual(10f, p.y, 1e-4f);
        }

        [Test]
        public void Center_MapsTo_Origin()
        {
            Vector2 p = Project(30f, 130f);
            Assert.AreEqual(0f, p.x, 1e-4f);
            Assert.AreEqual(0f, p.y, 1e-4f);
        }

        [Test]
        public void HigherLatitude_GivesLargerZ()
        {
            Assert.Less(Project(25f, 130f).y, Project(35f, 130f).y);
        }
    }
}
