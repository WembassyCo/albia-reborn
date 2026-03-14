using UnityEngine;
using AlbiaReborn.Creatures;
using AlbiaReborn.Core;

namespace AlbiaReborn.UI
{
    /// <summary>
    /// Central UI manager for all game overlays.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public CreatureInspector CreaturePanel;
        public GameObject PopulationCounter;
        public GameObject TimeDisplay;
        public GameObject WorldInfoPanel;

        [Header("Overlays")]
        public bool ShowBiomeOverlay = false;
        public bool ShowPopulationOverlay = false;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Toggle overlays with keys
            if (Input.GetKeyDown(KeyCode.B))
                ShowBiomeOverlay = !ShowBiomeOverlay;
            
            if (Input.GetKeyDown(KeyCode.P))
                ShowPopulationOverlay = !ShowPopulationOverlay;
            
            if (Input.GetKeyDown(KeyCode.I))
                ToggleInspector();
        }

        void ToggleInspector()
        {
            if (CreaturePanel != null)
            {
                CreaturePanel.gameObject.SetActive(!CreaturePanel.gameObject.activeSelf);
            }
        }

        /// <summary>
        /// Open creature inspection.
        /// </summary>
        public void InspectCreature(Organism organism)
        {
            if (CreaturePanel != null)
            {
                CreaturePanel.Inspect(organism);
            }
        }

        /// <summary>
        /// Close all panels.
        /// </summary>
        public void CloseAllPanels()
        {
            CreaturePanel?.Close();
        }

        /// <summary>
        /// Update time display.
        /// </summary>
        public void UpdateTimeDisplay(TimeManager time)
        {
            if (time == null || TimeDisplay == null) return;
            
            var text = TimeDisplay.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = $"Year {time.CurrentYear}, Day {time.CurrentDay}\n" +
                           $"Time: {time.DayAngle * 24f:F0}:00\n" +
                           $"Season: {time.CurrentSeason}";
            }
        }

        /// <summary>
        /// Update population counter.
        /// </summary>
        public void UpdatePopulationCount(int count)
        {
            if (PopulationCounter == null) return;
            
            var text = PopulationCounter.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = $"Population: {count}";
            }
        }
    }
}
