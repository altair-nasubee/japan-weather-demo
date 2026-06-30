using UnityEngine;

namespace JapanWeatherDemo.Map
{
    /// <summary>ビルボードを画面上一定サイズに保つためのワールドスケール計算（純粋関数）。</summary>
    public static class BillboardScale
    {
        /// <summary>
        /// 透視投影では見た目の大きさは worldSize/distance に比例する。
        /// 画面上一定サイズにするには worldSize を distance に比例させればよい。
        /// </summary>
        /// <param name="distance">カメラからマーカーまでの距離。</param>
        /// <param name="scalePerUnit">距離 1 あたりのワールドスケール（大きさの基準）。</param>
        public static float ScaleForConstantScreenSize(float distance, float scalePerUnit)
            => Mathf.Max(0f, distance) * scalePerUnit;
    }
}
