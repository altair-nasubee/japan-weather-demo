using UnityEngine;
using UnityEngine.VFX;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>rainIntensity とコンディションに応じて雨/雪 VFX の放出量を制御する。</summary>
    public class PrecipitationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private VisualEffect rainVfx;
        [SerializeField] private VisualEffect snowVfx;
        [SerializeField] private float maxRainRate = 4000f;
        [SerializeField] private float maxSnowRate = 1500f;
        [SerializeField] private float followSpeed = 6000f;

        private static readonly int SpawnRateId = Shader.PropertyToID("SpawnRate");
        private float targetRain, targetSnow, curRain, curSnow;

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
            bool isSnow = s.condition == WeatherCondition.Snow;
            bool isRainy = s.condition == WeatherCondition.Rain || s.condition == WeatherCondition.Storm;
            targetRain = isRainy ? Mathf.Clamp01(s.rainIntensity) * maxRainRate : 0f;
            targetSnow = isSnow ? maxSnowRate : 0f;
        }

        private void Update()
        {
            curRain = Mathf.MoveTowards(curRain, targetRain, followSpeed * Time.deltaTime);
            curSnow = Mathf.MoveTowards(curSnow, targetSnow, followSpeed * Time.deltaTime);
            if (rainVfx != null) rainVfx.SetFloat(SpawnRateId, curRain);
            if (snowVfx != null) snowVfx.SetFloat(SpawnRateId, curSnow);
        }
    }
}
