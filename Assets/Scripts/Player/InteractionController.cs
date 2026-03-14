using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Albia.Core;
using Albia.Creatures;

namespace Albia.Player
{
    /// <summary>
    /// Selection controller with reliable raycast and right-click commands.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }
        
        [Header("Camera")]
        public Camera playerCamera;
        public LayerMask selectableLayers;
        public LayerMask groundLayers;
        public float maxSelectionDistance = 100f;
        
        [Header("Selection")]
        public bool useMultipleRaycasts = true;
        public float raycastRadius = 0.2f;
        public int raycastCount = 5;
        public SelectionIndicator selectionIndicatorPrefab;
        public Color selectionColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        
        public Organism SelectedOrganism { get; private set; }
        private SelectionIndicator activeIndicator;
        
        public System.Action<Organism> OnOrganismSelected;
        public System.Action OnSelectionCleared;
        public System.Action<Vector3> OnCommandPosition;

        void Awake()
        {
            Instance = this;
            if (playerCamera == null) playerCamera = Camera.main;
            if (selectableLayers == 0)
                selectableLayers = LayerMask.GetMask("Creatures", "Default");
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Terrain", "Ground");
        }

        void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                TrySelect(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            
            if (UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame && SelectedOrganism != null)
                TryCommand(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            
            UpdateIndicator();
        }

        void TrySelect(Vector2 mousePos)
        {
            Ray ray = playerCamera.ScreenPointToRay(mousePos);
            
            Organism org = useMultipleRaycasts 
                ? RaycastWithRadius(ray) 
                : SimpleRaycast(ray);
            
            if (org != null && org.IsAlive)
                Select(org);
            else
                Deselect();
        }

        Organism SimpleRaycast(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, selectableLayers))
                return GetOrganism(hit.collider);
            return null;
        }

        Organism RaycastWithRadius(Ray ray)
        {
            // Try sphere cast first
            if (Physics.SphereCast(ray, raycastRadius, out RaycastHit sphereHit, maxSelectionDistance, selectableLayers))
            {
                Organism org = GetOrganism(sphereHit.collider);
                if (org != null) return org;
            }
            
            // Try overlapping rays
            Organism best = null;
            float bestDist = float.MaxValue;
            float angleStep = 360f / raycastCount;
            
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.1f;
                Vector3 dir = (ray.direction + playerCamera.transform.TransformDirection(offset)).normalized;
                
                if (Physics.Raycast(ray.origin, dir, out RaycastHit h, maxSelectionDistance, selectableLayers))
                {
                    Organism org = GetOrganism(h.collider);
                    if (org != null && h.distance < bestDist)
                    {
                        best = org;
                        bestDist = h.distance;
                    }
                }
            }
            
            return best;
        }

        Organism GetOrganism(Collider col)
        {
            if (col == null) return null;
            
            Organism org = col.GetComponent<Organism>();
            if (org != null) return org;
            
            return col.GetComponentInParent<Organism>();
        }

        void Select(Organism organism)
        {
            if (SelectedOrganism == organism) return;
            
            // Clear previous
            Deselect();
            
            SelectedOrganism = organism;
            
            if (selectionIndicatorPrefab != null)
            {
                activeIndicator = Instantiate(selectionIndicatorPrefab);
                activeIndicator.SetTarget(organism.transform);
                activeIndicator.SetColor(selectionColor);
                activeIndicator.Show();
            }
            
            OnOrganismSelected?.Invoke(organism);
        }

        void Deselect()
        {
            if (SelectedOrganism == null) return;
            
            SelectedOrganism = null;
            
            if (activeIndicator != null)
            {
                Destroy(activeIndicator.gameObject);
                activeIndicator = null;
            }
            
            OnSelectionCleared?.Invoke();
        }

        void UpdateIndicator()
        {
            if (activeIndicator != null)
            {
                if (SelectedOrganism != null)
                    activeIndicator.UpdatePosition();
                else
                    Deselect();
            }
        }

        void TryCommand(Vector2 mousePos)
        {
            Ray ray = playerCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, groundLayers))
            {
                CommandTo(hit.point);
            }
        }

        void CommandTo(Vector3 pos)
        {
            if (SelectedOrganism == null) return;
            
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                pos = hit.position;
            
            var motor = SelectedOrganism.GetComponent<NornMotor>();
            if (motor != null)
                motor.MoveTo(pos);
            else
            {
                var agent = SelectedOrganism.GetComponent<NavMeshAgent>();
                if (agent != null)
                    agent.SetDestination(pos);
            }
            
            OnCommandPosition?.Invoke(pos);
        }
    }
}
