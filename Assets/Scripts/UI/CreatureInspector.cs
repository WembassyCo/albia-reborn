using UnityEngine;
using UnityEngine.UI;
using AlbiaReborn.Creatures;
using AlbiaReborn.Creatures.Biochemistry;

namespace AlbiaReborn.UI
{
    /// <summary>
    /// Inspection panel for creatures.
    /// Shows: name, age, chemicals, genome.
    /// </summary>
    public class CreatureInspector : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject Panel;
        public Text NameText;
        public Text AgeText;
        public Text StageText;
        public Text EnergyText;
        public Transform ChemicalBarsParent;
        public GameObject ChemicalBarPrefab;

        private Organism _target;
        private ChemicalBar[] _chemicalBars;

        void Start()
        {
            Panel.SetActive(false);
            InitializeChemicalBars();
        }

        void InitializeChemicalBars()
        {
            int chemicalCount = System.Enum.GetValues(typeof(ChemicalType)).Length;
            _chemicalBars = new ChemicalBar[chemicalCount];

            int idx = 0;
            foreach (ChemicalType type in System.Enum.GetValues(typeof(ChemicalType)))
            {
                GameObject barObj = Instantiate(ChemicalBarPrefab, ChemicalBarsParent);
                _chemicalBars[idx] = barObj.GetComponent<ChemicalBar>();
                _chemicalBars[idx].SetLabel(type.ToString());
                idx++;
            }
        }

        void Update()
        {
            if (_target != null)
            {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Open inspector for specific creature.
        /// </summary>
        public void Inspect(Organism organism)
        {
            _target = organism;
            Panel.SetActive(true);
            UpdateDisplay();
        }

        public void Close()
        {
            _target = null;
            Panel.SetActive(false);
        }

        void UpdateDisplay()
        {
            if (_target == null) return;

            NameText.text = _target.OrganismName ?? "Unnamed";
            AgeText.text = $"Age: {_target.Age:F1}s";
            StageText.text = $"Stage: {_target.Stage}";
            EnergyText.text = $"Energy: {_target.Energy:F0}/{_target.MaxEnergy:F0}";

            // Update chemical bars
            if (_target.Chemicals != null)
            {
                int idx = 0;
                foreach (ChemicalType type in System.Enum.GetValues(typeof(ChemicalType)))
                {
                    float value = _target.Chemicals.GetChemical(type);
                    _chemicalBars[idx].SetValue(value);
                    idx++;
                }
            }
        }
    }

    /// <summary>
    /// Individual chemical bar UI element.
    /// </summary>
    public class ChemicalBar : MonoBehaviour
    {
        public Text Label;
        public Image Fill;

        public void SetLabel(string text)
        {
            if (Label != null) Label.text = text;
        }

        public void SetValue(float value)
        {
            if (Fill != null) Fill.fillAmount = value;
            
            // Color based on value
            if (Fill != null)
            {
                Fill.color = Color.Lerp(Color.red, Color.green, value);
            }
        }
    }
}
