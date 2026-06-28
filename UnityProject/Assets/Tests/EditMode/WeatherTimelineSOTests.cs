using NUnit.Framework;
using System;
using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Tests
{
    public class WeatherTimelineSOTests
    {
        static WeatherSnapshot Snap(float temp) => new WeatherSnapshot
        {
            dateTime = new DateTime(2024, 1, 1, 9, 0, 0),
            condition = WeatherCondition.Clear,
            temperatureCelsius = temp
        };

        [Test]
        public void SetData_FiresEvent_WithFirstSnapshot()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetData("東京", new[] { Snap(1f), Snap(2f) });

            Assert.AreEqual(0, so.currentIndex);
            Assert.IsTrue(got.HasValue);
            Assert.AreEqual(1f, got.Value.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void SetIndex_ClampsAndFires()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(1f), Snap(2f), Snap(3f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetIndex(99);

            Assert.AreEqual(2, so.currentIndex);            // クランプ
            Assert.AreEqual(3f, got.Value.temperatureCelsius, 1e-4f);
        }

        [Test]
        public void Count_ReflectsSnapshots()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(1f), Snap(2f) });
            Assert.AreEqual(2, so.Count);
        }

        [Test]
        public void SetContinuousIndex_InterpolatesBetweenSnapshots()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(10f), Snap(20f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetContinuousIndex(0.5f);

            Assert.AreEqual(15f, got.Value.temperatureCelsius, 1e-3f);
        }

        [Test]
        public void SetContinuousIndex_ClampsToRange()
        {
            var so = ScriptableObject.CreateInstance<WeatherTimelineSO>();
            so.SetData("東京", new[] { Snap(10f), Snap(20f) });
            WeatherSnapshot? got = null;
            so.OnSnapshotChanged += s => got = s;

            so.SetContinuousIndex(5f);

            Assert.AreEqual(20f, got.Value.temperatureCelsius, 1e-3f);
        }

    }
}
