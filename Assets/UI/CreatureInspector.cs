using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Albia.Creatures;
using Albia.Creatures.Neural;

namespace Albia.UI
{
    /// <summary>
    /// UI panel for inspecting a selected creature's detailed stats.
    /// Displays Name, Age, Energy, Stage, and Chemical state values with visualization bars.
    /// </summary>
    public class CreatureInspector : MonoBehaviour
    {
        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private Image creatureIcon;
        
        [Header("Core Stats")]
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI ageValueText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI energyValueText;
        
        [Header("Status Bars")]
        [SerializeField] private Image healthBar;
        [SerializeField] private Image hungerBar;
        [SerializeField] private Image energyBar;
        [SerializeField] private Image comfortBar;
        
        [Header("Chemical States")]
        [SerializeField] private Transform chemicalContainer;
        [SerializeField] private GameObject chemicalBarPrefab;
        
        [Header("Bar Colors")]
        [SerializeField] private Color healthColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color hungerColor = new Color(0.9f, 0.4f, 0.1f);
        [SerializeField] private Color energyColor = new Color(0.2f, 0.5f, 0.9f);
        [SerializeField] private Color comfortColor = new Color(0.8f, 0.7f, 0.2f);
        [SerializeField] private Color fearColor = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color painColor = new Color(0.6f, 0.1f, 0.6f);
        [SerializeField] private Color curiosityColor = new Color(0.9f, 0.6f, 0.1f);
        [SerializeField] private Color aggressionColor = new Color(0.8f, 0.2f, 0.2f);
        
        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        
        // Runtime
        private Norn targetCreature;
        private float lastUpdateTime;
        private ChemicalBar[] chemicalBars;
        
        // Lifecycle stages
        private string GetLifeStage(float age)
        {
            if (age < 0.5f) return "Baby";
            if (age < 2f) return "Child";
            if (age < 5f) return "Youth";
            if (age < 15f) return "Adult";
            return "Elder";
        }
        
        private void Awake()
        {
            SetupChemicalBars();
            SetBarColors();
        }
        
        private void SetupChemicalBars()
        {
            if (chemicalContainer == null || chemicalBarPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in chemicalContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create bars for each chemical state
            string[] chemicalNames = new[] { "Hunger", "Fear", "Pain", "Sleepiness", 
                                             "Reward", "Sex Drive", "Boredom", "Curiosity",
                                             "Comfort", "Aggression", "Trust" };
            Color[] colors = new[] { hungerColor, fearColor, painColor, comfortColor,
                                     curiosityColor, aggressionColor, comfortColor,
                                     curiosityColor, comfortColor, aggressionColor, comfortColor };
            
            chemicalBars = new ChemicalBar[chemicalNames.Length];
            
            for (int i = 0; i < chemicalNames.Length; i++)
            {
                GameObject barObj = Instantiate(chemicalBarPrefab, chemicalContainer);
                chemicalBars[i] = new ChemicalBar(barObj, chemicalNames[i], colors[i]);
            }
        }
        
        private void SetBarColors()
        {
            if (healthBar != null) healthBar.color = healthColor;
            if (hungerBar != null) hungerBar.color = hungerColor;
            if (energyBar != null) energyBar.color = energyColor;
            if (comfortBar != null) comfortBar.color = comfortColor;
        }
        
        private void Update()
        {
            if (targetCreature == null) return;
            if (Time.time - lastUpdateTime < updateInterval) return;
            
            lastUpdateTime = Time.time;
            RefreshDisplay();
        }
        
        /// <summary>
        /// Sets the creature to inspect and shows the panel
        /// </summary>
        public void Inspect(Norn creature)
        {
            targetCreature = creature;
            gameObject.SetActive(true);
            RefreshDisplay();
        }
        
        /// <summary>
        /// Clears the current inspection and hides the panel
        /// </summary>
        public void ClearInspection()
        {
            targetCreature = null;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Refreshes all displayed values
        /// </summary>
        public void RefreshDisplay()
        {
            if (targetCreature == null) return;
            
            NornState state = targetCreature.State;
            
            // Header
            if (nameText != null)
                nameText.text = targetCreature.name ?? $"Norn #{targetCreature.GetInstanceID():X}";
            
            if (stageText != null)
                stageText.text = GetLifeStage(state.Age);
            
            // Core stats
            if (ageText != null) ageText.text = "Age";
            if (ageValueText != null) ageValueText.text = $"{state.Age:F1} years";
            
            if (energyText != null) energyText.text = "Energy";
            if (energyValueText != null) energyValueText.text = $"{state.Energy:P0}";
            
            // Status bars
            SetBarFill(healthBar, state.Health);
            SetBarFill(hungerBar, state.Hunger);
            SetBarFill(energyBar, state.Energy);
            
            // Chemical states from sensory system
            if (targetCreature.GetComponent<SensorySystem>() != null)
            {
                ChemicalState chemicals = targetCreature.GetComponent<SensorySystem>().Chemicals;
                SetBarFill(comfortBar, chemicals.Comfort);
                
                // Update chemical bars
                if (chemicalBars != null)
                {
                    chemicalBars[0].SetValue(chemicals.Hunger);     // Hunger
                    chemicalBars[1].SetValue(chemicals.Fear);       // Fear
                    chemicalBars[2].SetValue(chemicals.Pain);        // Pain
                    chemicalBars[3].SetValue(chemicals.Sleepiness); // Sleepiness
                    chemicalBars[4].SetValue(chemicals.Reward);      // Reward
                    chemicalBars[5].SetValue(chemicals.SexDrive);    // Sex Drive
                    chemicalBars[6].SetValue(chemicals.Boredom);     // Boredom
                    chemicalBars[7].SetValue(chemicals.Curiosity);   // Curiosity
                    chemicalBars[8].SetValue(chemicals.Comfort);     // Comfort
                    chemicalBars[9].SetValue(chemicals.Aggression);  // Aggression
                    chemicalBars[10].SetValue(chemicals.Trust);      // Trust
                }
            }
            
            // Update colors based on critical values
            UpdateCriticalColors(state);
        }
        
        private void SetBarFill(Image bar, float value)
        {
            if (bar != null)
                bar.fillAmount = Mathf.Clamp01(value);
        }
        
        private void UpdateCriticalColors(NornState state)
        {
            // Flash health bar when low
            if (healthBar != null && state.Health < 0.3f)
            {
                healthBar.color = Color.Lerp(Color.red, healthColor, 
                    Mathf.PingPong(Time.time * 2f, 1f));
            }
            else if (healthBar != null)
            {
                healthBar.color = healthColor;
            }
            
            // Hunger bar gets more red when high
            if (hungerBar != null)
            {
                hungerBar.color = Color.Lerp(energyColor, hungerColor, state.Hunger);
            }
        }
        
        /// <summary>
        /// Gets the currently inspected creature
        /// </summary>
        public Norn GetInspectedCreature() => targetCreature;
        
        /// <summary>
        /// Helper class for individual chemical bars
        /// </summary>
        private class ChemicalBar
        {
            public readonly Image FillImage;
            public readonly TextMeshProUGUI LabelText;
            public readonly TextMeshProUGUI ValueText;
            
            public ChemicalBar(GameObject barObject, string label, Color color)
            {
                // Find child components
                Transform fillTransform = barObject.transform.Find("Fill");
                if (fillTransform != null)
                    FillImage = fillTransform.GetComponent<Image>();
                
                Transform labelTransform = barObject.transform.Find("Label");
                if (labelTransform != null)
                    LabelText = labelTransform.GetComponent<TextMeshProUGUI>();
                
                Transform valueTransform = barObject.transform.Find("Value");
                if (valueTransform != null)
                    ValueText = valueTransform.GetComponent<TextMeshProUGUI>();
                
                if (FillImage != null)
                    FillImage.color = color;
                
                if (LabelText != null)
                    LabelText.text = label;
            }
            
            public void SetValue(float value)
            {
                if (FillImage != null)
                    FillImage.fillAmount = Mathf.Clamp01(value);
                
                if (ValueText != null)
                    ValueText.text = $"{Mathf.Clamp01(value):P0}";
            }
        }
    }
}