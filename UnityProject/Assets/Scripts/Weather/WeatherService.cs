using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using JapanWeatherDemo.Data;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap から予報を取得する。キー無し/失敗時はダミーで成立させる。</summary>
    public class WeatherService : MonoBehaviour
    {
        const string Endpoint = "https://api.openweathermap.org/data/2.5/forecast";
        const int Cnt = 40;

        private string apiKey = "";
        private readonly Dictionary<string, WeatherSnapshot[]> cache = new();

        public bool HasApiKey => !string.IsNullOrEmpty(apiKey);

        private void Awake() => apiKey = ResolveKey();

        private string ResolveKey()
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
            string json = File.Exists(configPath) ? File.ReadAllText(configPath) : null;
            return ApiKeyResolver.Resolve(json, Environment.GetEnvironmentVariable);
        }

        public void FetchForecast(CityData city, Action<WeatherSnapshot[]> onResult, Action<string> onError)
        {
            if (cache.TryGetValue(city.name, out var cached)) { onResult?.Invoke(cached); return; }

            if (!HasApiKey)
            {
                onError?.Invoke("API キー未設定: ダミーデータで表示します");
                var dummy = DummyWeather.Generate(city.name, DateTime.Now);
                cache[city.name] = dummy;
                onResult?.Invoke(dummy);
                return;
            }
            StartCoroutine(FetchRoutine(city, onResult, onError));
        }

        private IEnumerator FetchRoutine(CityData city, Action<WeatherSnapshot[]> onResult, Action<string> onError)
        {
            string url = $"{Endpoint}?lat={city.lat}&lon={city.lon}&appid={apiKey}&units=metric&cnt={Cnt}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"取得失敗: {req.error}。ダミーデータで表示します");
                var dummy = DummyWeather.Generate(city.name, DateTime.Now);
                onResult?.Invoke(dummy);
                yield break;
            }

            WeatherSnapshot[] snaps;
            try { snaps = WeatherParser.Parse(req.downloadHandler.text); }
            catch (Exception e)
            {
                onError?.Invoke($"パース失敗: {e.Message}");
                yield break; // 直前の表示を維持
            }

            cache[city.name] = snaps;
            onResult?.Invoke(snaps);
        }
    }
}
