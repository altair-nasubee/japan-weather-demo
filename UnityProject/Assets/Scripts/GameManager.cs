using System;
using UnityEngine;
using JapanWeatherDemo.Data;
using JapanWeatherDemo.Map;
using JapanWeatherDemo.Weather;
using JapanWeatherDemo.UI;

namespace JapanWeatherDemo
{
    /// <summary>起動時配線のハブ。都市選択 → 取得 → タイムライン更新を仲介する。</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private MapManager mapManager;
        [SerializeField] private WeatherService weatherService;
        [SerializeField] private string initialCityName = "東京";
        [SerializeField] private TimelineUIController timelineUI;

        public WeatherTimelineSO Timeline { get; private set; }
        public event Action<string> StatusMessage;
        public event Action<bool> LoadingChanged;

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
            LoadingChanged?.Invoke(true);
            weatherService.FetchForecast(
                city,
                snaps =>
                {
                    Timeline.SetData(city.name, snaps);
                    if (timelineUI != null) timelineUI.ConfigureForCurrentData();
                    LoadingChanged?.Invoke(false);
                },
                error =>
                {
                    StatusMessage?.Invoke(error);
                    LoadingChanged?.Invoke(false);
                });
        }
    }
}
