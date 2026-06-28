using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 地図の緯度経度範囲とワールド XZ 範囲を 1 箇所に集約する定数アセット。
    /// テクスチャのトリミング範囲とマーカー配置でこのアセットを共有する。
    /// </summary>
    [CreateAssetMenu(fileName = "MapBounds", menuName = "JapanWeatherDemo/Map Bounds")]
    public class MapBoundsSO : ScriptableObject
    {
        [Header("緯度経度範囲（WGS84）")]
        public float latMin = 24.0f;
        public float latMax = 46.5f;
        public float lonMin = 122.0f;
        public float lonMax = 146.5f;

        [Header("地図 Plane のワールド XZ 範囲")]
        public float planeMinX = -10f;
        public float planeMaxX = 10f;
        public float planeMinZ = -10f;
        public float planeMaxZ = 10f;
    }
}
