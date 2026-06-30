using NUnit.Framework;
using JapanWeatherDemo.Map;

namespace JapanWeatherDemo.Tests
{
    public class BillboardScaleTests
    {
        [Test]
        public void Scale_IsProportionalToDistance()
        {
            // 画面上一定サイズ ⇔ ワールドスケールは距離に比例
            float s1 = BillboardScale.ScaleForConstantScreenSize(10f, 0.01f);
            float s2 = BillboardScale.ScaleForConstantScreenSize(20f, 0.01f);
            Assert.AreEqual(0.1f, s1, 1e-5f);
            Assert.AreEqual(0.2f, s2, 1e-5f);
            Assert.AreEqual(2f, s2 / s1, 1e-5f); // 距離2倍→スケール2倍
        }

        [Test]
        public void Scale_NeverNegative()
        {
            float s = BillboardScale.ScaleForConstantScreenSize(-5f, 0.01f);
            Assert.AreEqual(0f, s, 1e-5f);
        }
    }
}
