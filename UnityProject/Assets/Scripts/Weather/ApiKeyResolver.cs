using System;
using Newtonsoft.Json.Linq;

namespace JapanWeatherDemo.Weather
{
    /// <summary>config.json → 環境変数 OWM_API_KEY の順に API キーを解決する。</summary>
    public static class ApiKeyResolver
    {
        public const string EnvVarName = "OWM_API_KEY";

        public static string Resolve(string configJsonOrNull, Func<string, string> envGetter)
        {
            if (!string.IsNullOrEmpty(configJsonOrNull))
            {
                try
                {
                    var key = JObject.Parse(configJsonOrNull)["apiKey"]?.ToString();
                    if (!string.IsNullOrEmpty(key)) return key;
                }
                catch { /* 不正 JSON は無視して次へ */ }
            }
            string env = envGetter?.Invoke(EnvVarName);
            return string.IsNullOrEmpty(env) ? "" : env;
        }
    }
}
