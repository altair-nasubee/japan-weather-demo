using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>時刻で太陽角度を、コンディションで光の強さ・色を制御する。</summary>
    public class SkyController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Light sun;
        [SerializeField] private float followSpeed = 2f;
        [SerializeField] private float sunYaw = 170f; // 南中方向（南向き）

        private float targetElevation, curElevation;
        private float targetIntensity, curIntensity;
        private Color targetColor = Color.white, curColor = Color.white;

        private void OnEnable()
        {
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            float hour = s.dateTime.Hour + s.dateTime.Minute / 60f;
            targetElevation = SunAngle.ElevationDeg(hour);

            // コンディションで光量と色温度の目標を決める
            float cloudDim = Mathf.Lerp(1f, 0.35f, s.cloudCoverage);
            targetIntensity = Mathf.Max(0f, targetElevation > 0 ? cloudDim : 0.05f);

            // 朝夕はオレンジ寄り、昼は白、夜は青み
            if (targetElevation <= 0f) targetColor = new Color(0.4f, 0.5f, 0.8f);      // 夜
            else if (targetElevation < 20f) targetColor = new Color(1f, 0.6f, 0.35f);  // 朝夕焼け
            else targetColor = Color.white;                                            // 昼
        }

        private void Update()
        {
            if (sun == null) return;
            curElevation = Mathf.MoveTowards(curElevation, targetElevation, followSpeed * 30f * Time.deltaTime);
            curIntensity = Mathf.MoveTowards(curIntensity, targetIntensity, followSpeed * Time.deltaTime);
            curColor = Color.Lerp(curColor, targetColor, followSpeed * Time.deltaTime);

            sun.transform.rotation = Quaternion.Euler(curElevation, sunYaw, 0f);
            sun.intensity = curIntensity * 3.14f; // HDRP の Lux スケールに合わせ調整
            sun.color = curColor;
        }
    }
}
