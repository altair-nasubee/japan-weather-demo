using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>Volumetric Clouds の密度を cloudCoverage に滑らかに追従させる。</summary>
    public class CloudController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Volume volume;
        [SerializeField] private float followSpeed = 1.5f;
        [SerializeField] private float maxDensity = 1f;

        private VolumetricClouds clouds;
        private float targetCoverage;
        private float currentCoverage;

        private void OnEnable()
        {
            if (volume != null && volume.profile.TryGet(out clouds)) { }
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s) => targetCoverage = s.cloudCoverage;

        private void Update()
        {
            if (clouds == null) return;
            currentCoverage = Mathf.MoveTowards(currentCoverage, targetCoverage, followSpeed * Time.deltaTime);
            clouds.densityMultiplier.value = currentCoverage * maxDensity;
            clouds.enable.value = currentCoverage > 0.02f;
        }
    }
}
