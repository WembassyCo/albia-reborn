using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Albia.Core;

namespace Albia.UI
{
    /// <summary>
    /// MVP creature inspector panel.
    /// Shows real-time stats for selected organism.
    /// </summary>
    public class CreatureInspectorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot; // The inspector panel container
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private Slider energySlider;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private TextMeshProUGUI chemicalText; // MVP: Debug format

        private Organism selectedOrganism;

        void Start()
        {
            InteractionController.Instance.OnOrganismSelected += OnOrganismSelected;
            InteractionController.Instance.OnSelectionCleared += OnSelectionCleared;
            
            panelRoot.SetActive(false);
        }

        void OnDestroy()
        {
            if (InteractionController.Instance != null)
            {
                InteractionController.Instance.OnOrganismSelected -= OnOrganismSelected;
                InteractionController.Instance.OnSelectionCleared -= OnSelectionCleared;
            }
        }

        void Update()
        {
            if (selectedOrganism != null && selectedOrganism.IsAlive)
            {
                UpdateDisplay();
            }
        }

        private void OnOrganismSelected(Organism organism)
        {
            selectedOrganism = organism;
            panelRoot.SetActive(true);
            UpdateDisplay();
            
            // Hook into organism events - scales to full property updates
            organism.OnEnergyChanged += OnEnergyChanged;
        }

        private void OnSelectionCleared()
        {
            if (selectedOrganism != null)
            {
                selectedOrganism.OnEnergyChanged -= OnEnergyChanged;
            }
            selectedOrganism = null;
            panelRoot.SetActive(false);
        }

        private void OnEnergyChanged(float newEnergy)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (selectedOrganism == null) return;

            nameText.text = selectedOrganism.name;
            ageText.text = $"Age: {selectedOrganism.Age:F1} years";
            stageText.text = $"Stage: {selectedOrganism.Stage}";
            
            energySlider.value = selectedOrganism.Energy / selectedOrganism.MaxEnergy;
            energyText.text = $"{selectedOrganism.Energy:F0} / {selectedOrganism.MaxEnergy:F0}";

            // SCALES TO: Full chemical readout
            // Check if organism has ChemicalState (via interface or derived type)
            if (selectedOrganism is Norn norn && norn.Chemicals != null)
            {
                var c = norn.Chemicals;
                chemicalText.text = $"H: {c.Hunger:F2} F: {c.Fear:F2} R: {c.Reward:F2}\n" +
                                   $"L: {c.Loneliness:F2} B: {c.Boredom:F2} S: {c.Satisfaction:F2}";
            }
            else
            {
                chemicalText.text = "Chemistry: N/A";
            }
        }
    }
}