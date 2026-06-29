using UnityEngine;
using TMPro;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.UI
{
    /// <summary>右上の情報パネル。都市名・天気・気温を表示する。</summary>
    public class InfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text conditionLabel;
        [SerializeField] private TMP_Text temperatureLabel;

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
            if (conditionLabel != null) conditionLabel.text = ToJapanese(s.condition);
            if (temperatureLabel != null) temperatureLabel.text = $"{s.temperatureCelsius:0.0}℃";
        }

        private static string ToJapanese(WeatherCondition c) => c switch
        {
            WeatherCondition.Clear => "晴れ ☀",
            WeatherCondition.Cloudy => "曇り ☁",
            WeatherCondition.Rain => "雨 ☂",
            WeatherCondition.Storm => "雷雨 ⚡",
            WeatherCondition.Snow => "雪 ❄",
            _ => "—"
        };
    }
}
