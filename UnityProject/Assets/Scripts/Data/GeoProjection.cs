using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 緯度経度を地図 Plane 上の XZ 座標へ線形（等緯度経度）変換する純関数。
    /// 戻り値 Vector2 の x が World X、y が World Z を表す。
    /// </summary>
    public static class GeoProjection
    {
        public static Vector2 LatLonToXZ(
            float lat, float lon,
            float latMin, float latMax, float lonMin, float lonMax,
            float planeMinX, float planeMaxX, float planeMinZ, float planeMaxZ)
        {
            float x = Mathf.Lerp(planeMinX, planeMaxX, Mathf.InverseLerp(lonMin, lonMax, lon));
            float z = Mathf.Lerp(planeMinZ, planeMaxZ, Mathf.InverseLerp(latMin, latMax, lat));
            return new Vector2(x, z);
        }

        public static Vector2 LatLonToXZ(float lat, float lon, MapBoundsSO b)
        {
            return LatLonToXZ(lat, lon,
                b.latMin, b.latMax, b.lonMin, b.lonMax,
                b.planeMinX, b.planeMaxX, b.planeMinZ, b.planeMaxZ);
        }
    }
}
