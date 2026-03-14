using UnityEngine;
using Albia.Core;

namespace Albia.Player
{
    /// <summary>
    /// Click handler for selecting and interacting with creatures/objects.
    /// MVP: Simple selection
    /// Full: Inspection panel, teaching interactions, tool usage
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask selectableLayers;
        [SerializeField] private float maxSelectionDistance = 50f;

        public Organism SelectedOrganism { get; private set; }
        public GameObject SelectedObject { get; private set; }

        // Events
        public System.Action<Organism> OnOrganismSelected;
        public System.Action OnSelectionCleared;

        private void Update()
        {
            HandleSelection();
        }

        private void HandleSelection()
        {
            // Left click to select
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = playerCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                
                if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, selectableLayers))
                {
                    // Check for organism
                    Organism organism = hit.collider.GetComponent<Organism>();
                    if (organism != null)
                    {
                        SelectOrganism(organism);
                        return;
                    }

                    // Check for interactable object
                    SelectedObject = hit.collider.gameObject;
                    Debug.Log($"[Interaction] Selected object: {SelectedObject.name}");
                }
                else
                {
                    // Clear selection on empty click
                    ClearSelection();
                }
            }
        }

        private void SelectOrganism(Organism organism)
        {
            SelectedOrganism = organism;
            Debug.Log($"[Interaction] Selected Norn: {organism.name} (Energy: {organism.Energy:F1})");
            
            OnOrganismSelected?.Invoke(organism);

            // SCALES TO:
            // - Open inspection panel
            // - Show genome viewer
            // - Display chemical state
            // - Enable teaching interactions
        }

        private void ClearSelection()
        {
            SelectedOrganism = null;
            SelectedObject = null;
            OnSelectionCleared?.Invoke();
        }

        /// <summary>
        /// SCALES TO: Interact with selected creature
        /// </summary>
        public void InteractWithSelected()
        {
            if (SelectedOrganism == null) return;
            
            // MVP: No direct interaction
            // Full: Attempt play interaction, stimulate curiosity chemical
            Debug.Log($"[Interaction] Interacting with {SelectedOrganism.name}");
        }

        /// <summary>
        /// SCALES TO: Teach word to selected Norn
        /// </summary>
        public void TeachWord(string word)
        {
            if (SelectedOrganism == null) return;
            
            // MVP: Log only
            // Full: Language system processes word association
            Debug.Log($"[Teaching] Attempting to teach '{word}' to {SelectedOrganism.name}");
        }
    }
}