using NUnit.Framework;
using UnityEngine;
using JapanWeatherDemo.CameraControl;

namespace JapanWeatherDemo.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void Position_IsAboveAndBehindTarget()
        {
            var target = new Vector3(3f, 0.1f, -2f);
            var (pos, _) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Assert.AreEqual(3f, pos.x, 1e-4f);
            Assert.AreEqual(0.1f + 14f, pos.y, 1e-4f);
            Assert.AreEqual(-2f - 10f, pos.z, 1e-4f);
        }

        [Test]
        public void Rotation_LooksAtTarget()
        {
            var target = new Vector3(3f, 0.1f, -2f);
            var (pos, rot) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 expected = (target - pos).normalized;
            Assert.AreEqual(expected.x, fwd.x, 1e-4f);
            Assert.AreEqual(expected.y, fwd.y, 1e-4f);
            Assert.AreEqual(expected.z, fwd.z, 1e-4f);
        }

        [Test]
        public void Pitch_MatchesHeightToDistanceRatio()
        {
            var target = Vector3.zero;
            var (_, rot) = CameraFraming.ComputeFocusPose(target, 14f, 10f);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 flat = new Vector3(fwd.x, 0f, fwd.z);
            float pitch = Vector3.Angle(fwd, flat);
            float expected = Mathf.Atan2(14f, 10f) * Mathf.Rad2Deg; // ≈ 54.46°
            Assert.AreEqual(expected, pitch, 1e-2f);
        }
    }
}
