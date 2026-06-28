using System.Collections.Generic;
using Newtonsoft.Json;

namespace JapanWeatherDemo.Weather
{
    /// <summary>OpenWeatherMap /forecast レスポンスのデシリアライズ用 DTO。</summary>
    public class OwmForecastResponse
    {
        [JsonProperty("list")] public List<OwmListItem> List { get; set; }
        [JsonProperty("city")] public OwmCity City { get; set; }
    }

    public class OwmCity
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("timezone")] public int Timezone { get; set; }
    }

    public class OwmListItem
    {
        [JsonProperty("dt")] public long Dt { get; set; }
        [JsonProperty("main")] public OwmMain Main { get; set; }
        [JsonProperty("weather")] public List<OwmWeather> Weather { get; set; }
        [JsonProperty("clouds")] public OwmClouds Clouds { get; set; }
        [JsonProperty("wind")] public OwmWind Wind { get; set; }
        [JsonProperty("rain")] public OwmRain Rain { get; set; }
    }

    public class OwmMain { [JsonProperty("temp")] public float Temp { get; set; } }
    public class OwmWeather { [JsonProperty("id")] public int Id { get; set; } }
    public class OwmClouds { [JsonProperty("all")] public float All { get; set; } }
    public class OwmWind { [JsonProperty("speed")] public float Speed { get; set; } [JsonProperty("deg")] public float Deg { get; set; } }
    public class OwmRain { [JsonProperty("3h")] public float ThreeHour { get; set; } }
}
