using UnityEngine;

namespace JapanWeatherDemo.Weather
{
    /// <summary>時刻（JST 小数時）から太陽の高度角を返す簡易モデル。</summary>
    public static class SunAngle
    {
        // elevation = -90 * cos(2π * hour/24)。0 時=-90、6 時=0、12 時=+90、18 時=0。
        public static float ElevationDeg(float hour)
        {
            return -90f * Mathf.Cos(2f * Mathf.PI * hour / 24f);
        }
    }
}
