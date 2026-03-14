using UnityEngine;
using System;

namespace AlbiaReborn.Core
{
    /// <summary>
    /// Master time simulation.
    /// Tracks orbital position, day angle, moon phase.
    /// Single source of truth for all time-dependent systems.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Header("Time Scale")]
        public float TimeScale = 60f; // 1 real second = 1 game minute
        public float MinTimeScale = 0.5f;
        public float MaxTimeScale = 100f;

        [Header("World Parameters")]
        public float AxialTilt = 23.5f; // degrees
        public float OrbitalPeriod = 365f; // game days per year
        public float LunarPeriod = 30f; // game days per lunar cycle

        [Header("Current State")]
        public float OrbitalPosition = 0f; // 0-1 (fraction of year)
        public float DayAngle = 0f; // 0-1 (0=midnight, 0.5=noon)
        public float MoonPhase = 0f; // 0-1
        public float TotalYears = 0f;

        [Header("Derived")]
        public int CurrentDay = 0;
        public int CurrentYear = 0;
        public Season CurrentSeason = Season.Spring;

        public float SeasonalIntensity => Mathf.Cos(OrbitalPosition * Mathf.PI * 2f);

        public event Action OnDayChanged;
        public event Action OnSeasonChanged;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime * TimeScale;
            
            // Update orbital position (1 year = OrbitalPeriod days)
            float daysPassed = deltaTime / (60f * 60f * 24f); // Convert to game-days
            float yearFraction = daysPassed / OrbitalPeriod;
            
            OrbitalPosition += yearFraction;
            if (OrbitalPosition >= 1f)
            {
                OrbitalPosition -= 1f;
                TotalYears++;
                CurrentYear++;
            }

            // Day angle (24 hour cycle)
            DayAngle += daysPassed; // Days passed = fractional days
            int daysAdvanced = 0;
            while (DayAngle >= 1f)
            {
                DayAngle -= 1f;
                daysAdvanced++;
            }
            
            if (daysAdvanced > 0)
            {
                CurrentDay += daysAdvanced;
                OnDayChanged?.Invoke();
            }

            // Moon phase
            MoonPhase += daysPassed / LunarPeriod;
            if (MoonPhase >= 1f) MoonPhase -= 1f;

            // Update season
            UpdateSeason();
        }

        void UpdateSeason()
        {
            // 0.0-0.25 = Spring, 0.25-0.5 = Summer, 0.5-0.75 = Autumn, 0.75-1.0 = Winter
            Season newSeason = OrbitalPosition switch
            {
                < 0.25f => Season.Spring,
                < 0.5f => Season.Summer,
                < 0.75f => Season.Autumn,
                _ => Season.Winter
            };

            if (newSeason != CurrentSeason)
            {
                CurrentSeason = newSeason;
                OnSeasonChanged?.Invoke();
                Debug.Log($"Season changed to {CurrentSeason}");
            }
        }

        /// <summary>
        /// Get day length based on orbital position (for seasonal day length variation).
        /// </summary>
        public float GetDayLength()
        {
            // Varies with orbital position due to axial tilt
            return 12f + SeasonalIntensity * (24f - 12f); // 12-24 hours
        }

        /// <summary>
        /// Set time scale (player-adjustable).
        /// </summary>
        public void SetTimeScale(float scale)
        {
            TimeScale = Mathf.Clamp(scale, MinTimeScale, MaxTimeScale);
        }
    }

    public enum Season
    {
        Spring, Summer, Autumn, Winter
    }
}
