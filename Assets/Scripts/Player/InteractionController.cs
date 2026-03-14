using UnityEngine;
using UnityEngine.UI;
using Albia.Core;
using System.Collections.Generic;

namespace Albia.UI
{
    /// <summary>
    /// Simple selection and interaction handler.
    /// Attached to Main Camera.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }
        
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask selectableLayers;
        [SerializeField] private float maxSelectionDistance = 50f;

        public Organism SelectedOrganism { get; private set; }
        
        // Events
        public System.Action<Organism> OnOrganismSelected;
        public System.Action OnSelectionCleared;

        void Awake()
        {
            Instance = this;
            if (playerCamera == null) playerCamera = Camera.main;
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = playerCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                
                if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, selectableLayers))
                {
                    Organism organism = hit.collider.GetComponent<Organism>();
                    if (organism != null && organism.IsAlive)
                    {
                        SelectOrganism(organism);
                    }
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        private void SelectOrganism(Organism organism)
        {
            SelectedOrganism = organism;
            OnOrganismSelected?.Invoke(organism);
        }

        private void ClearSelection()
        {
            SelectedOrganism = null;
            OnSelectionCleared?.Invoke();
        }
    }
}