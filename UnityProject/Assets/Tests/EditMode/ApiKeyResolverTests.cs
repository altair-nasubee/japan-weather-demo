using NUnit.Framework;
using JapanWeatherDemo.Weather;

namespace JapanWeatherDemo.Tests
{
    public class ApiKeyResolverTests
    {
        [Test]
        public void Resolve_PrefersConfigJson()
        {
            string json = "{\"apiKey\":\"FROM_CONFIG\"}";
            string key = ApiKeyResolver.Resolve(json, name => "FROM_ENV");
            Assert.AreEqual("FROM_CONFIG", key);
        }

        [Test]
        public void Resolve_FallsBackToEnv_WhenConfigMissing()
        {
            string key = ApiKeyResolver.Resolve(null, name => name == "OWM_API_KEY" ? "FROM_ENV" : "");
            Assert.AreEqual("FROM_ENV", key);
        }

        [Test]
        public void Resolve_FallsBackToEnv_WhenConfigKeyEmpty()
        {
            string key = ApiKeyResolver.Resolve("{\"apiKey\":\"\"}", name => "FROM_ENV");
            Assert.AreEqual("FROM_ENV", key);
        }

        [Test]
        public void Resolve_ReturnsEmpty_WhenNothingSet()
        {
            string key = ApiKeyResolver.Resolve(null, name => "");
            Assert.AreEqual("", key);
        }
    }
}
