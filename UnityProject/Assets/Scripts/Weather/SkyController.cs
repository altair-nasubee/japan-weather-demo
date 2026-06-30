using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
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
        private HDAdditionalLightData sunHD;

        private void OnEnable()
        {
            if (sun != null) sunHD = sun.GetComponent<HDAdditionalLightData>();
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

            // 昼夜の明暗差は「識別できる程度」に抑える。曇りでも昼/夜らしさは保つ。
            float cloud = Mathf.Clamp01(s.cloudCoverage);

            if (targetElevation > 0f)
            {
                // 昼：日中とわかる明るさを保ちつつ、まぶしくしすぎない。曇りでも暗くしすぎない。
                targetIntensity = Mathf.Lerp(0.8f, 0.6f, cloud);
                // 朝夕はオレンジ寄り、日中は白
                targetColor = targetElevation < 20f ? new Color(1f, 0.7f, 0.5f) : Color.white;
            }
            else
            {
                // 夜：低い斜光を補うため lux は高めだが、青み＋低照射角で夜と分かる。暗くなりすぎない。
                targetIntensity = Mathf.Lerp(0.95f, 0.8f, cloud);
                targetColor = new Color(0.6f, 0.68f, 0.9f); // 夜と分かる程度の青み
            }
        }

        private void Update()
        {
            if (sun == null) return;
            curElevation = Mathf.MoveTowards(curElevation, targetElevation, followSpeed * 30f * Time.deltaTime);
            curIntensity = Mathf.MoveTowards(curIntensity, targetIntensity, followSpeed * Time.deltaTime);
            curColor = Color.Lerp(curColor, targetColor, followSpeed * Time.deltaTime);

            // 太陽が低い/地平線下でもマップ上面に光が当たるよう、照射角度に下限を設ける
            float lightPitch = Mathf.Max(curElevation, 30f);
            sun.transform.rotation = Quaternion.Euler(lightPitch, sunYaw, 0f);
            // HDRP は HDAdditionalLightData.intensity（Lux）が実効値。Light.intensity 直接では暗くなる。
            if (sunHD != null) sunHD.intensity = curIntensity * 100000f;
            else sun.intensity = curIntensity * 3.14f;
            sun.color = curColor;
        }
    }
}
