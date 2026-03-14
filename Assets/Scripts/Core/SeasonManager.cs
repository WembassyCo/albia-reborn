using UnityEngine;
using System;

namespace Albia.Core
{
    /// <summary>
    /// Manages seasonal cycles affecting world and creatures
    /// MVP: Basic season tracking
    /// Full: Biome-specific seasons, weather, migration triggers
    /// </summary>
    public class SeasonManager : MonoBehaviour
    {
        public static SeasonManager Instance { get; private set; }
        
        public enum Season { Spring, Summer, Autumn, Winter }
        
        public Season CurrentSeason { get; private set; } = Season.Spring;
        public int CurrentYear { get; private set; } = 1;
        
        [Header("Season Settings")]
        [SerializeField] private float daysPerSeason = 30f;
        
        // Events
        public event Action<Season> OnSeasonChanged;
        public event Action<int> OnYearChanged;
        
        void Awake() => Instance = this;
        
        void Start()
        {
            TimeManager.Instance?.OnDayPassed.AddListener(OnDayPassed);
        }
        
        void OnDayPassed(int day)
        {
            int seasonIndex = (int)((day - 1) / daysPerSeason) % 4;
            Season newSeason = (Season)seasonIndex;
            
            if (newSeason != CurrentSeason)
            {
                CurrentSeason = newSeason;
                OnSeasonChanged?.Invoke(CurrentSeason);
                
                if (CurrentSeason == Season.Spring && day > daysPerSeason)
                {
                    CurrentYear++;
                    OnYearChanged?.Invoke(CurrentYear);
                }
            }
        }
        
        public float GetGrowthMultiplier()
        {
            return CurrentSeason switch
            {
                Season.Spring => 1.5f,
                Season.Summer => 1.2f,
                Season.Autumn => 0.8f,
                Season.Winter => 0.3f,
                _ => 1f
            };
        }
        
        public float GetMetabolismMultiplier()
        {
            return CurrentSeason == Season.Winter ? 1.3f : 1f;
        }
    }
}