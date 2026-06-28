using JapanWeatherDemo.Weather;
using System;
using System;
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>
    /// 天気タイムラインのランタイムデータコンテナ。保存アセットではなく
    /// CreateInstance で 1 個だけ生成し、都市切替で snapshots を差し替えて再利用する。
    /// </summary>
    public class WeatherTimelineSO : ScriptableObject
    {
        public string cityName;
        public WeatherSnapshot[] snapshots = new WeatherSnapshot[0];
        public int currentIndex;

        public event Action<WeatherSnapshot> OnSnapshotChanged;

        public int Count => snapshots != null ? snapshots.Length : 0;

        public void SetData(string cityName, WeatherSnapshot[] snapshots)
        {
            this.cityName = cityName;
            this.snapshots = snapshots ?? new WeatherSnapshot[0];
            currentIndex = 0;
            if (Count > 0) OnSnapshotChanged?.Invoke(this.snapshots[0]);
        }

        public void SetIndex(int index)
        {
            if (Count == 0) return;
            currentIndex = Mathf.Clamp(index, 0, snapshots.Length - 1);
            OnSnapshotChanged?.Invoke(snapshots[currentIndex]);
        }

        /// <summary>連续位置(0〜 Count-1)から補間したスナップショットを発火する。</summary>
        public void SetContinuousIndex(float pos)
        {
            if (Count == 0) return;
            pos = Mathf.Clamp(pos, 0f, snapshots.Length - 1);
            int i = Mathf.FloorToInt(pos);
            int next = Mathf.Min(i + 1, snapshots.Length - 1);
            float frac = pos - i;
            currentIndex = Mathf.RoundToInt(pos);
            OnSnapshotChanged?.Invoke(SnapshotInterpolator.Lerp(snapshots[i], snapshots[next], frac));
        }

    }
}
