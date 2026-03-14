using UnityEngine;
using System;

namespace Albia.Core
{
    /// <summary>
    /// Weather effects on world
    /// MVP: Rain toggle
    /// Full: Temperature, wind, storms
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }
        
        public enum WeatherType { Clear, Rain, Storm }
        
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
        
        [Header("Settings")]
        [SerializeField] private float rainChance = 0.1f;
        [SerializeField] private float rainDuration = 60f;
        
        private float weatherTimer = 0f;
        
        public event Action<WeatherType> OnWeatherChanged;
        
        void Awake() => Instance = this;
        
        void Update()
        {
            weatherTimer += Time.deltaTime;
            
            if (CurrentWeather == WeatherType.Clear && weatherTimer > 10f)
            {
                if (UnityEngine.Random.value < rainChance * Time.deltaTime)
                {
                    SetWeather(WeatherType.Rain);
                }
            }
            else if (CurrentWeather != WeatherType.Clear && weatherTimer > rainDuration)
            {
                SetWeather(WeatherType.Clear);
            }
        }
        
        void SetWeather(WeatherType type)
        {
            CurrentWeather = type;
            weatherTimer = 0f;
            OnWeatherChanged?.Invoke(type);
            
            // SCALES TO: Particle effects, lighting changes
        }
        
        public bool IsRaining => CurrentWeather == WeatherType.Rain || CurrentWeather == WeatherType.Storm;
    }
}