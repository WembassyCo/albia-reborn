using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Albia.Creatures;
using AlbiaReborn.Climate;

namespace Albia.UI
{
    /// <summary>
    /// UI panel showing world statistics: population counts, time/season, and selected creature info.
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [Header("World Stats")]
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private TextMeshProUGUI dayText;
        
        [Header("Creature Selection Info")]
        [SerializeField] private TextMeshProUGUI selectedNameText;
        [SerializeField] private TextMeshProUGUI selectedStatusText;
        [SerializeField] private GameObject selectedInfoPanel;
        
        [Header("Biome Display")]
        [SerializeField] private TextMeshProUGUI biomeText;
        [SerializeField] private TextMeshProUGUI temperatureText;
        [SerializeField] private TextMeshProUGUI moistureText;
        
        [Header("World References")]
        [SerializeField] private Transform creaturesContainer;
        [SerializeField] private WeatherCell weatherCell;
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private bool trackTime = true;
        
        [Header("Display Settings")]
        [SerializeField] private string dateFormat = "Day {0} - {1:00}:{2:00}";
        [SerializeField] private string[] monthNames = new[] { 
            "Spring", "Spring", "Summer", "Summer", 
            "Fall", "Fall", "Winter", "Winter" 
        };
        
        // Runtime
        private float lastUpdateTime;
        private Norn selectedCreature;
        private List<Norn> trackedCreatures = new List<Norn>();
        
        // World simulation time
        private float worldTime;
        private float dayLength = 120f; // 2 minutes = 1 day
        private int currentDay = 1;
        
        private void Awake()
        {
            if (selectedInfoPanel != null)
                selectedInfoPanel.SetActive(false);
                
            // Try to find weather cell if not assigned
            if (weatherCell == null)
                weatherCell = FindObjectOfType<WeatherCell>();
        }
        
        private void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval) return;
            
            lastUpdateTime = Time.time;
            RefreshStats();
            
            if (trackTime)
                UpdateWorldTime();
        }
        
        /// <summary>
        /// Updates the world simulation time
        /// </summary>
        private void UpdateWorldTime()
        {
            worldTime += Time.deltaTime;
            
            // Calculate day progression
            float dayProgress = worldTime / dayLength;
            currentDay = Mathf.FloorToInt(dayProgress) + 1;
            float timeOfDay = (dayProgress % 1f) * 24f; // 0-24 hours
            
            // Calculate month/season (8 months = 1 year)
            int month = Mathf.FloorToInt((dayProgress / 30f) % 8f);
            month = Mathf.Clamp(month, 0, 7);
            
            // Update displays
            if (timeText != null)
            {
                int hours = Mathf.FloorToInt(timeOfDay);
                int minutes = Mathf.FloorToInt((timeOfDay % 1f) * 60f);
                timeText.text = string.Format(dateFormat, currentDay, hours, minutes);
            }
            
            if (seasonText != null)
                seasonText.text = GetSeasonForMonth(month);
            
            if (dayText != null)
                dayText.text = $"Year {(currentDay / 240) + 1}";
        }
        
        private string GetSeasonForMonth(int month)
        {
            if (month < 0 || month >= monthNames.Length)
                return "Unknown";
            return monthNames[month];
        }
        
        /// <summary>
        /// Refreshes all displayed statistics
        /// </summary>
        public void RefreshStats()
        {
            UpdatePopulationCount();
            UpdateSelectedInfo();
            UpdateBiomeInfo();
        }
        
        /// <summary>
        /// Updates the population display
        /// </summary>
        private void UpdatePopulationCount()
        {
            RefreshTrackedCreatures();
            
            if (populationText == null) return;
            
            int alive = 0;
            int dead = 0;
            
            foreach (Norn norn in trackedCreatures)
            {
                if (norn != null && norn.IsAlive)
                    alive++;
                else
                    dead++;
            }
            
            populationText.text = $"Norns: {alive} Alive | {dead} Deceased";
        }
        
        /// <summary>
        /// Updates the selected creature info panel
        /// </summary>
        private void UpdateSelectedInfo()
        {
            if (selectedCreature == null || selectedInfoPanel == null)
            {
                if (selectedInfoPanel != null)
                    selectedInfoPanel.SetActive(false);
                return;
            }
            
            selectedInfoPanel.SetActive(true);
            
            if (selectedNameText != null)
                selectedNameText.text = selectedCreature.name ?? $"Norn #{selectedCreature.GetInstanceID():X}";
            
            if (selectedStatusText != null && selectedCreature.IsAlive)
            {
                NornState state = selectedCreature.State;
                string activity = GetCurrentActivity(selectedCreature);
                selectedStatusText.text = $"{activity}\nHealth: {state.Health:P0} | Energy: {state.Energy:P0}";
            }
            else if (selectedStatusText != null)
            {
                selectedStatusText.text = "Deceased";
            }
        }
        
        private string GetCurrentActivity(Norn norn)
        {
            // Access action system through reflection or property
            var actionSystem = norn.GetComponent<Albia.Creatures.Neural.ActionSystem>();
            if (actionSystem != null)
                return actionSystem.GetActionDescription();
            
            // Fallback states
            NornState state = norn.State;
            if (state.Health < 0.3f) return "Critical";
            if (state.Hunger > 0.7f) return "Hungry";
            if (state.Energy < 0.2f) return "Tired";
            return "Idle";
        }
        
        /// <summary>
        /// Updates biome and weather information
        /// </summary>
        private void UpdateBiomeInfo()
        {
            if (weatherCell != null)
            {
                if (biomeText != null)
                    biomeText.text = BiomeHelper.GetBiomeName(weatherCell.Biome);
                
                if (temperatureText != null)
                    temperatureText.text = $"{weatherCell.Temperature:F1}°C";
                
                if (moistureText != null)
                    moistureText.text = $"Moisture: {weatherCell.Moisture:P0}";
            }
            else
            {
                // Use default display
                if (biomeText != null) biomeText.text = "Temperate Forest";
                if (temperatureText != null) temperatureText.text = "15.0°C";
                if (moistureText != null) moistureText.text = "Moisture: 50%";
            }
        }
        
        /// <summary>
        /// Refreshes the list of tracked creatures
        /// </summary>
        public void RefreshTrackedCreatures()
        {
            trackedCreatures.Clear();
            
            if (creaturesContainer != null)
            {
                foreach (Transform child in creaturesContainer)
                {
                    Norn norn = child.GetComponent<Norn>();
                    if (norn != null)
                        trackedCreatures.Add(norn);
                }
            }
            else
            {
                // Fall back to FindObjectsOfType
                Norn[] norns = FindObjectsOfType<Norn>();
                trackedCreatures.AddRange(norns);
            }
        }
        
        /// <summary>
        /// Sets the currently selected creature for display
        /// </summary>
        public void SetSelectedCreature(Norn creature)
        {
            selectedCreature = creature;
            UpdateSelectedInfo();
        }
        
        /// <summary>
        /// Gets the currently selected creature
        /// </summary>
        public Norn GetSelectedCreature() => selectedCreature;
        
        /// <summary>
        /// Clears the current creature selection
        /// </summary>
        public void ClearSelection()
        {
            selectedCreature = null;
            if (selectedInfoPanel != null)
                selectedInfoPanel.SetActive(false);
        }
        
        /// <summary>
        /// Adds a creature to be tracked (for population count)
        /// </summary>
        public void TrackCreature(Norn creature)
        {
            if (creature != null && !trackedCreatures.Contains(creature))
                trackedCreatures.Add(creature);
        }
        
        /// <summary>
        /// Removes a creature from tracking
        /// </summary>
        public void UntrackCreature(Norn creature)
        {
            trackedCreatures.Remove(creature);
            
            if (selectedCreature == creature)
                ClearSelection();
        }
        
        /// <summary>
        /// Gets the total population count
        /// </summary>
        public int GetPopulationCount() => trackedCreatures.Count;
        
        /// <summary>
        /// Gets the count of living creatures
        /// </summary>
        public int GetLivingCount()
        {
            int count = 0;
            foreach (Norn norn in trackedCreatures)
                if (norn != null && norn.IsAlive)
                    count++;
            return count;
        }
        
        /// <summary>
        /// Sets the world time directly
        /// </summary>
        public void SetWorldTime(float time, int day)
        {
            worldTime = time;
            currentDay = day;
        }
        
        /// <summary>
        /// Gets the current world time
        /// </summary>
        public float GetWorldTime() => worldTime;
        
        /// <summary>
        /// Gets the current day
        /// </summary>
        public int GetCurrentDay() => currentDay;
    }
}