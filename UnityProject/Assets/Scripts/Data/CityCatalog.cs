using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace JapanWeatherDemo.Data
{
    /// <summary>都市マスタ（Cities.json）の読み込み。</summary>
    public static class CityCatalog
    {
        public static List<CityData> LoadFromJson(string json)
        {
            var list = JsonConvert.DeserializeObject<List<CityData>>(json);
            return list ?? new List<CityData>();
        }

        public static List<CityData> LoadFromStreamingAssets(string fileName = "Cities.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[CityCatalog] not found: {path}");
                return new List<CityData>();
            }
            return LoadFromJson(File.ReadAllText(path));
        }
    }
}