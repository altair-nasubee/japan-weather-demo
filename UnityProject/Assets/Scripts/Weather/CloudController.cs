using UnityEngine;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>
    /// 地図上空に置いた半透明の雲レイヤー（Plane）の不透明度と色を、
    /// cloudCoverage とコンディションに滑らかに追従させる。
    /// HDRP の Volumetric Clouds は地球スケールの大気でミニチュア俯瞰では映らないため、
    /// このデモではメッシュレイヤー方式を採用する。
    /// </summary>
    public class CloudController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Renderer cloudLayer; // 地図上空の雲レイヤー（半透明 Plane）
        [SerializeField] private float followSpeed = 1.5f;
        [SerializeField] private float maxAlpha = 0.85f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private Material mat;
        private float targetCoverage, currentCoverage;
        private Color targetTint = Color.white, curTint = Color.white;

        private void OnEnable()
        {
            if (cloudLayer != null) mat = cloudLayer.material; // インスタンス化
            if (gameManager != null) gameManager.Timeline.OnSnapshotChanged += OnSnapshot;
        }

        private void OnDisable()
        {
            if (gameManager != null && gameManager.Timeline != null)
                gameManager.Timeline.OnSnapshotChanged -= OnSnapshot;
        }

        private void OnSnapshot(WeatherSnapshot s)
        {
            targetCoverage = s.cloudCoverage;
            // 雨・雷・雪は暗い雲、晴れ・曇りは明るい雲色
            switch (s.condition)
            {
                case WeatherCondition.Storm: targetTint = new Color(0.30f, 0.30f, 0.36f); break;
                case WeatherCondition.Rain: targetTint = new Color(0.55f, 0.55f, 0.60f); break;
                case WeatherCondition.Snow: targetTint = new Color(0.92f, 0.92f, 0.96f); break;
                default: targetTint = Color.white; break;
            }
        }

        private void Update()
        {
            if (mat == null) return;
            currentCoverage = Mathf.MoveTowards(currentCoverage, targetCoverage, followSpeed * Time.deltaTime);
            curTint = Color.Lerp(curTint, targetTint, followSpeed * Time.deltaTime);
            Color c = curTint;
            c.a = currentCoverage * maxAlpha;
            mat.SetColor(BaseColorId, c);
        }
    }
}
