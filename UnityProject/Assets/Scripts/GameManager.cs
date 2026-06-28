using System;
using UnityEngine;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Map;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo
{
    /// <summary>起動時配線のハブ。都市選択 → 取得 → タイムライン更新を仲介する。</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private WeatherService weatherService;
        [SerializeField] private string initialCityName = "東京";

        public WeatherTimelineSO Timeline { get; private set; }
        public event Action<string> StatusMessage;

        private void Awake()
        {
            Timeline = ScriptableObject.CreateInstance<WeatherTimelineSO>();
        }

        private void OnEnable()
        {
            if (mapManager != null) mapManager.CitySelected += SelectCity;
        }

        private void OnDisable()
        {
            if (mapManager != null) mapManager.CitySelected -= SelectCity;
        }

        private void Start()
        {
            // 初期都市（東京）を選択。マーカー側にも選択ハイライトを反映させる。
            mapManager.SelectByName(initialCityName);
        }

        private void SelectCity(CityData city)
        {
            weatherService.FetchForecast(
                city,
                snaps =>
                {
                    Timeline.SetData(city.name, snaps);
                    Debug.Log($"[GameManager] {city.name}: {snaps.Length} snapshots loaded");
                },
                error => StatusMessage?.Invoke(error));
        }
    }
}
