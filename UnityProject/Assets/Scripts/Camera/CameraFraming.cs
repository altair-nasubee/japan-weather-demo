using UnityEngine;

namespace JapanWeatherDemo.CameraControl
{
    /// <summary>都市フォーカス時のカメラ姿勢を算出する純粋関数。</summary>
    public static class CameraFraming
    {
        /// <summary>
        /// target（注視点）に対し height だけ上空・backDistance だけ -Z に引いた
        /// 斜め見下ろしのカメラ姿勢を返す。rotation は target を向く。
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ComputeFocusPose(
            Vector3 target, float height, float backDistance)
        {
            Vector3 position = target + new Vector3(0f, height, -backDistance);
            Quaternion rotation = Quaternion.LookRotation(target - position);
            return (position, rotation);
        }
    }
}
