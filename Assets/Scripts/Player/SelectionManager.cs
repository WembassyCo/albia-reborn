using UnityEngine;
using Albia.Core;
using Albia.Creatures;

namespace Albia.Player
{
    /// <summary>
    /// Manages creature selection and visual indicator
    /// MVP: Click to select, visual ring
    /// Full: Multi-select, selection box, grouping
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private GameObject selectionRingPrefab;
        [SerializeField] private Material selectionMaterial;
        [SerializeField] private float ringHeight = 0.1f;
        [SerializeField] private float ringSize = 1.2f;
        
        [Header("Input")]
        [SerializeField] private KeyCode deselectKey = KeyCode.Escape;
        
        private GameObject currentRing;
        private Organism selectedOrganism;
        
        public static SelectionManager Instance { get; private set; }
        public Organism SelectedOrganism => selectedOrganism;
        
        public event System.Action<Organism> OnOrganismSelected;
        public event System.Action OnSelectionCleared;
        
        void Awake()
        {
            Instance = this;
        }
        
        void Start()
        {
            // Create selection ring
            CreateSelectionRing();
        }
        
        void Update()
        {
            // Handle deselect
            if (Input.GetKeyDown(deselectKey))
            {
                ClearSelection();
            }
            
            // Update ring position
            UpdateSelectionVisual();
        }
        
        void CreateSelectionRing()
        {
            if (selectionRingPrefab != null)
            {
                currentRing = Instantiate(selectionRingPrefab);
            }
            else
            {
                // Create primitive ring
                currentRing = new GameObject("SelectionRing");
                
                // Create a simple ring using LineRenderer
                LineRenderer lr = currentRing.AddComponent<LineRenderer>();
                lr.positionCount = 32;
                lr.useWorldSpace = true;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.loop = true;
                
                // Circle positions
                for (int i = 0; i < 32; i++)
                {
                    float angle = (i / 32f) * Mathf.PI * 2;
                    Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ringSize;
                    lr.SetPosition(i, pos);
                }
                
                if (selectionMaterial != null)
                {
                    lr.material = selectionMaterial;
                }
            }
            
            currentRing.SetActive(false);
        }
        
        /// <summary>
        /// Select an organism
        /// </summary>
        public void SelectOrganism(Organism organism)
        {
            if (organism == null || !organism.IsAlive)
            {
                ClearSelection();
                return;
            }
            
            selectedOrganism = organism;
            currentRing.SetActive(true);
            
            OnOrganismSelected?.Invoke(organism);
        }
        
        /// <summary>
        /// Clear current selection
        /// </summary>
        public void ClearSelection()
        {
            selectedOrganism = null;
            if (currentRing != null)
            {
                currentRing.SetActive(false);
            }
            
            OnSelectionCleared?.Invoke();
        }
        
        void UpdateSelectionVisual()
        {
            if (selectedOrganism == null || currentRing == null) return;
            
            // Follow selected organism
            Vector3 pos = selectedOrganism.transform.position;
            pos.y += ringHeight;
            currentRing.transform.position = pos;
        }
        
        /// <summary>
        /// Check if organism is selected
        /// </summary>
        public bool IsSelected(Organism organism)
        {
            return selectedOrganism == organism;
        }
    }
}