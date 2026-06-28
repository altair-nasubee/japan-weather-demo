using System;

namespace JapanWeatherDemo.Data
{
    [Serializable]
    public struct CityData
    {
        public string name;
        public float lat;
        public float lon;
        public string prefecture;
    }
}