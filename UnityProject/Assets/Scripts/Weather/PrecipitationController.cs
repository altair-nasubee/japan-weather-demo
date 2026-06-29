using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>
    /// rainIntensity とコンディションに応じて雨/雪 ParticleSystem の放出量を制御する。
    /// VFX Graph はミニチュア俯瞰デモでの調整が難しいため ParticleSystem を採用。
    /// </summary>
    public class PrecipitationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ParticleSystem rainPS;
        [SerializeField] private ParticleSystem snowPS;
        [SerializeField] private float maxRainRate = 3000f;
        [SerializeField] private float maxSnowRate = 800f;
        [SerializeField] private float followSpeed = 4000f;

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
            if (rainPS != null) { var em = rainPS.emission; em.rateOverTime = curRain; }
            if (snowPS != null) { var em = snowPS.emission; em.rateOverTime = curSnow; }
        }
    }
}
