using UnityEngine;
using System;

namespace Albia.Core
{
    /// <summary>
    /// Master time simulation.
    /// MVP: Basic time scaling
    /// Full: Orbital mechanics, seasons, day/night, tides
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Scale")]
        [SerializeField] private float timeScale = 60f; // 1 real sec = 1 game minute
        [SerializeField] private float minTimeScale = 0f;
        [SerializeField] private float maxTimeScale = 300f;

        [Header("Time Tracking")]
        [SerializeField] private int gameDay = 1;
        [SerializeField] private float dayTime = 0f; // 0-24 hours
        
        // Public data
        public float TotalGameTime { get; private set; } = 0f;
        public int GameDay => gameDay;
        public float DayTime => dayTime; // 0-24
        public float CurrentTimeScale => timeScale;
        
        // Events (for UI, simulation systems)
        public event Action HourPassed;
        public event Action DayPassed;
        
        // Scales to:
        public float SeasonalIntensity { get; private set; } = 0f; // -1 winter to +1 summer
        public float DayLength { get; private set; } = 12f; // hours
        public float LightLevel { get; private set; } = 1f; // 0-1

        private float lastHour = -1f;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            // Advance game time
            float delta = Time.deltaTime * timeScale;
            dayTime += delta / 60f; // Convert to game minutes then hours
            TotalGameTime += delta;

            // Check for day rollover
            if (dayTime >= 24f)
            {
                dayTime -= 24f;
                gameDay++;
                DayPassed?.Invoke();
                
                // Scales to: Season tracking
                UpdateSeasons();
            }

            // Check for hour tick
            int currentHour = Mathf.FloorToInt(dayTime);
            if (currentHour != lastHour)
            {
                lastHour = currentHour;
                HourPassed?.Invoke();
            }

            // Update derived values
            UpdateLightLevel();
        }

        private void UpdateLightLevel()
        {
            // Simple day/night cycle
            // Noon at 12, midnight at 0/24
            float dayProgress = dayTime / 24f;
            LightLevel = Mathf.Sin(dayProgress * Mathf.PI * 2f - Mathf.PI / 2f) * 0.5f + 0.5f;
            
            // Scales to: 
            // - Seasonal light adjustment
            // - Moon phase at night
            // - Weather effects
        }

        private void UpdateSeasons()
        {
            // SCALES TO: 
            // SeasonalIntensity derived from orbital position
            // Uses axial tilt and day of year
            // Triggers SeasonChanged event
        }

        /// <summary>
        /// Set simulation speed (for player controls)
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Clamp(scale, minTimeScale, maxTimeScale);
        }

        /// <summary>
        /// Pause/resume toggle
        /// </summary>
        public void TogglePause()
        {
            Time.timeScale = Time.timeScale > 0 ? 0f : 1f;
        }

        /// <summary>
        /// Scales to: Season queries
        /// </summary>
        public float GetTemperatureOffset()
        {
            return SeasonalIntensity * 10f; // +/- 10 degrees
        }
    }
}