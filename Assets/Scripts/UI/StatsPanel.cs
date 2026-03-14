using UnityEngine;
using TMPro;
using Albia.Core;

namespace Albia.UI
{
    /// <summary>
    /// World stats display (population, time, etc.)
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI seasonText; // SCALES TO: full seasons
        
        private float updateTimer = 0f;
        private const float UPDATE_INTERVAL = 1f;

        void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                updateTimer = 0f;
                UpdateStats();
            }
        }

        void UpdateStats()
        {
            // Population
            int norns = GameObject.FindObjectsOfType<Norn>().Length;
            // SCALES TO: PopulationRegistry.GetAllNorns().Count
            populationText.text = $"Norns: {norns}";

            // Time
            if (TimeManager.Instance != null)
            {
                timeText.text = $"Day {TimeManager.Instance.GameDay}, {TimeManager.Instance.DayTime:F1}h";
            }

            // Season (MVP: none, Full: calculated from TimeManager)
            seasonText.text = "Season: --"; // SCALES TO: seasonal display
        }
    }
}