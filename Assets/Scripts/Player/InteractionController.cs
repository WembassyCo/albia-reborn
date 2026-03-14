using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Albia.Core;
using Albia.Physics;
using Albia.Creatures;

namespace Albia.Player
{
    /// <summary>
    /// Enhanced selection and interaction with reliable raycasting and right-click commands.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }
        
        [Header("Camera")]
        public Camera playerCamera;
        public LayerMask selectableLayers;
        public LayerMask groundLayers;
        public float maxSelectionDistance = 100f;
        
        [Header("Raycast")]
        public bool useMultipleRaycasts = true;
        public float raycastRadius = 0.2f;
        public int raycastCount = 5;
        
        [Header("Selection")]
        public SelectionIndicator selectionIndicatorPrefab;
        public bool showSelectionIndicator = true;
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
            HandleInput();
            UpdateSelectionIndicator();
        }

        void HandleInput()
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            bool leftClick = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            bool rightClick = UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            
            if (leftClick && !overUI)
                TrySelect(mousePos);
            
            if (rightClick && !overUI && SelectedOrganism != null)
                TryCommand(mousePos);
        }

        void TrySelect(Vector2 mousePos)
        {
            Ray ray = playerCamera.ScreenPointToRay(mousePos);
            Organism organism = useMultipleRaycasts ? RaycastWithRadius(ray) : SimpleRaycast(ray);
            
            if (organism != null && organism.IsAlive)
                Select(organism);
            else
                Deselect();
        }

        Organism SimpleRaycast(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, selectableLayers))
                return GetOrganismFromCollider(hit.collider);
            return null;
        }

        Organism RaycastWithRadius(Ray ray)
        {
            // Sphere cast
            if (Physics.SphereCast(ray, raycastRadius, out RaycastHit hit, maxSelectionDistance, selectableLayers))
            {
                Organism org = GetOrganismFromCollider(hit.collider);
                if (org != null) return org;
            }
            
            // Multiple rays
            Organism best = null;
            float bestDist = float.MaxValue;
            
            if (Physics.Raycast(ray, out RaycastHit centerHit, maxSelectionDistance, selectableLayers))
            {
                best = GetOrganismFromCollider(centerHit.collider);
                bestDist = centerHit.distance;
            }
            
            float angleStep = 360f / raycastCount;
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 0.05f;
                Vector3 newDir = (ray.direction + playerCamera.transform.TransformDirection(offset)).normalized;
                
                if (Physics.Raycast(ray.origin, newDir, out RaycastHit h, maxSelectionDistance, selectableLayers))
                {
                    Organism org = GetOrganismFromCollider(h.collider);
                    if (org != null && h.distance < bestDist)
                    {
                        best = org;
                        bestDist = h.distance;
                    }
                }
            }
            
            return best;
        }

        Organism GetOrganismFromCollider(Collider col)
        {
            if (col == null) return null;
            
            Organism org = col.GetComponent<Organism>() ?? col.GetComponentInParent<Organism>();
            if (org != null) return org;
            
            Norn norn = col.GetComponent<Norn>() ?? col.GetComponentInParent<Norn>();
            return norn as Organism;
        }

        void Select(Organism organism)
        {
            if (SelectedOrganism == organism) return;
            
            // Clear previous
            if (activeIndicator != null)
            {
                Destroy(activeIndicator.gameObject);
                activeIndicator = null;
            }
            
            SelectedOrganism = organism;
            
            // Create new indicator
            if (showSelectionIndicator && selectionIndicatorPrefab != null)
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

        void UpdateSelectionIndicator()
        {
            if (activeIndicator != null && SelectedOrganism != null)
                activeIndicator.UpdatePosition();
            else if (activeIndicator != null)
                Deselect();
        }

        void TryCommand(Vector2 mousePos)
        {
            Ray ray = playerCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, groundLayers))
            {
                CommandTo(hit.point);
            }
        }

        void CommandTo(Vector3 position)
        {
            if (SelectedOrganism == null) return;
            
            // Validate position
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                position = hit.position;
            
            // Send command
            var motor = SelectedOrganism.GetComponent<NornMotor>();
            if (motor != null)
                motor.MoveTo(position);
            else
            {
                var agent = SelectedOrganism.GetComponent<NavMeshAgent>();
                if (agent != null)
                    agent.SetDestination(position);
            }
            
            OnCommandPosition?.Invoke(position);
        }

        public bool IsSelected(Organism organism)
        {
            return SelectedOrganism == organism;
        }
    }
}
