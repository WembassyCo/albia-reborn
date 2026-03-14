using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Albia.Core;
using Albia.Physics;
using Albia.Creatures;
using System.Collections.Generic;

namespace Albia.Player
{
    /// <summary>
    /// Enhanced selection and interaction handler with reliable raycasting
    /// and right-click commanding functionality.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }
        
        [Header("Camera Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask selectableLayers;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float maxSelectionDistance = 100f;
        [SerializeField] private float commandDistance = 100f;
        
        [Header("Selection Raycast")]
        [SerializeField] private bool useMultipleRaycasts = true;
        [SerializeField] private float raycastRadius = 0.2f;
        [SerializeField] private float raycastDistance = 0.3f;
        [SerializeField] private int raycastCount = 5;
        
        [Header("Selection Indicator")]
        [SerializeField] private SelectionIndicator selectionIndicatorPrefab;
        [SerializeField] private Transform selectionIndicatorContainer;
        [SerializeField] private bool showSelectionIndicator = true;
        [SerializeField] private Color selectionColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        [SerializeField] private float indicatorOffset = 0.1f;
        
        [Header("Command Visuals")]
        [SerializeField] private LineRenderer commandLineRenderer;
        [SerializeField] private float commandLineDuration = 1f;
        [SerializeField] private GameObject commandMarkerPrefab;
        [SerializeField] private float commandMarkerDuration = 2f;
        
        [Header("UI Settings")]
        [SerializeField] private bool ignoreUI = true;
        [SerializeField] private bool showDebugInfo = false;
        
        public Organism SelectedOrganism { get; private set; }
        
        private SelectionIndicator activeIndicator;
        private GameObject activeCommandMarker;
        private Coroutine commandLineCoroutine;
        
        public System.Action<Organism> OnOrganismSelected;
        public System.Action OnSelectionCleared;
        public System.Action<Vector3> OnCommandPosition;
        public System.Action<Transform> OnCommandTarget;
        
        private Vector2 mouseScreenPosition;
        private bool leftClicked;
        private bool rightClicked;
        private bool isPointerOverUI;

        void Awake()
        {
            Instance = this;
            InitializeComponents();
            
            if (selectableLayers == 0)
                selectableLayers = LayerMask.GetMask("Creatures", "Organism", "Default");
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Terrain", "Ground", "World");
        }

        void Update()
        {
            GatherInput();
            UpdateSelectionIndicator();
            
            if (leftClicked && !isPointerOverUI)
                HandleSelection();
            
            if (rightClicked && !isPointerOverUI)
                HandleCommand();
        }

        void InitializeComponents()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
            
            if (commandLineRenderer == null)
            {
                GameObject lineObj = new GameObject("CommandLine");
                lineObj.transform.SetParent(transform);
                commandLineRenderer = lineObj.AddComponent<LineRenderer>();
                commandLineRenderer.positionCount = 2;
                commandLineRenderer.startWidth = 0.05f;
                commandLineRenderer.endWidth = 0.02f;
                commandLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                commandLineRenderer.startColor = Color.green;
                commandLineRenderer.endColor = Color.green;
                commandLineRenderer.enabled = false;
            }
            
            if (selectionIndicatorContainer == null)
            {
                GameObject container = new GameObject("SelectionIndicators");
                container.transform.SetParent(transform);
                selectionIndicatorContainer = container.transform;
            }
        }

        void GatherInput()
        {
            mouseScreenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            leftClicked = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            rightClicked = UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
            isPointerOverUI = ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void HandleSelection()
        {
            Ray ray = playerCamera.ScreenPointToRay(mouseScreenPosition);
            Organism hitOrganism = useMultipleRaycasts ? RaycastWithRadius(ray) : SimpleRaycast(ray);
            
            if (hitOrganism != null && hitOrganism.IsAlive)
                SelectOrganism(hitOrganism);
            else
                ClearSelection();
        }
        
        Organism SimpleRaycast(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxSelectionDistance, selectableLayers))
                return GetOrganismFromCollider(hit.collider);
            return null;
        }
        
        Organism RaycastWithRadius(Ray ray)
        {
            if (Physics.SphereCast(ray, raycastRadius, out RaycastHit hit, maxSelectionDistance, selectableLayers))
            {
                Organism organism = GetOrganismFromCollider(hit.collider);
                if (organism != null) return organism;
            }
            
            Organism bestOrganism = null;
            float bestDistance = float.MaxValue;
            
            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit centerHit, maxSelectionDistance, selectableLayers))
            {
                Organism org = GetOrganismFromCollider(centerHit.collider);
                if (org != null && centerHit.distance < bestDistance)
                {
                    bestOrganism = org;
                    bestDistance = centerHit.distance;
                }
            }
            
            float angleStep = 360f / raycastCount;
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * raycastRadius;
                Vector3 newDir = (ray.direction + playerCamera.transform.TransformDirection(offset) * 0.1f).normalized;
                
                if (Physics.Raycast(ray.origin, newDir, out RaycastHit offHit, maxSelectionDistance, selectableLayers))
                {
                    Organism org = GetOrganismFromCollider(offHit.collider);
                    if (org != null && offHit.distance < bestDistance)
                    {
                        bestOrganism = org;
                        bestDistance = offHit.distance;
                    }
                }
            }
            
            if (bestOrganism == null)
            {
                Vector3 sphereCenter = ray.origin + ray.direction * 10f;
                Collider[] colliders = Physics.OverlapSphere(sphereCenter, raycastRadius * 2f, selectableLayers);
                
                foreach (var col in colliders)
                {
                    Organism org = GetOrganismFromCollider(col);
                    if (org != null)
                    {
                        float dist = Vector3.Distance(sphereCenter, org.transform.position);
                        if (dist < bestDistance)
                        {
                            bestOrganism = org;
                            bestDistance = dist;
                        }
                    }
                }
            }
            
            return bestOrganism;
        }
        
        Organism GetOrganismFromCollider(Collider collider)
        {
            if (collider == null) return null;
            
            Organism organism = collider.GetComponent<Organism>();
            if (organism != null) return organism;
            
            organism = collider.GetComponentInParent<Organism>();
            if (organism != null) return organism;
            
            Norn norn = collider.GetComponent<Norn>() ?? collider.GetComponentInParent<Norn>();
            if (norn != null && norn is Organism)
                return (Organism)(object)norn;
            
            if (collider.CompareTag("Creature") || collider.CompareTag("Organism") || collider.CompareTag("Norn"))
                return collider.GetComponent<Organism>() ?? collider.GetComponentInParent<Organism>();
            
            return null;
        }
        
        void SelectOrganism(Organism organism)
        {
            if (organism == SelectedOrganism) return;
            ClearSelection(false);
            
            SelectedOrganism = organism;
            
            if (showSelectionIndicator && selectionIndicatorPrefab != null)
                CreateSelectionIndicator(organism);
            
            OnOrganismSelected?.Invoke(organism);
        }
        
        void ClearSelection(bool triggerEvent = true)
        {
            if (SelectedOrganism == null) return;
            
            SelectedOrganism = null;
            
            if (activeIndicator != null)
            {
                Destroy(activeIndicator.gameObject);
                activeIndicator = null;
            }
            
            if (triggerEvent)
                OnSelectionCleared?.Invoke();
        }
        
        void CreateSelectionIndicator(Organism organism)
        {
            if (selectionIndicatorPrefab == null) return;
            
            activeIndicator = Instantiate(selectionIndicatorPrefab, selectionIndicatorContainer);
            activeIndicator.SetTarget(organism.transform);
            activeIndicator.SetColor(selectionColor);
            activeIndicator.SetOffset(indicatorOffset);
            activeIndicator.Show();
        }
        
        void UpdateSelectionIndicator()
        {
            if (activeIndicator == null) return;
            
            if (SelectedOrganism != null)
                activeIndicator.UpdatePosition();
            else
                ClearSelection();
        }

        void HandleCommand()
        {
            if (SelectedOrganism == null) return;
            
            Ray ray = playerCamera.ScreenPointToRay(mouseScreenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit targetHit, maxSelectionDistance, selectableLayers))
            {
                Organism targetOrganism = GetOrganismFromCollider(targetHit.collider);
                if (targetOrganism != null && targetOrganism != SelectedOrganism)
                {
                    CommandToTarget(targetOrganism.transform);
                    return;
                }
            }
            
            if (Physics.Raycast(ray, out RaycastHit groundHit, commandDistance, groundLayers))
                CommandToPosition(groundHit.point);
        }
        
        void CommandToPosition(Vector3 position)
        {
            if (!IsValidNavMeshPosition(position, out Vector3 validPosition))
                return;
            
            SendMoveCommand(validPosition);
            ShowCommandFeedback(SelectedOrganism.transform.position, validPosition);
            OnCommandPosition?.Invoke(validPosition);
        }
        
        void CommandToTarget(Transform target)
        {
            SendFollowCommand(target);
            ShowCommandFeedback(SelectedOrganism.transform.position, target.position);
            OnCommandTarget?.Invoke(target);
        }
        
        void SendMoveCommand(Vector3 position)
        {
            NornMotor motor = SelectedOrganism.GetComponent<NornMotor>();
            if (motor != null)
            {
                motor.MoveTo(position);
                return;
            }
            
            NavMeshAgent agent = SelectedOrganism.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled)
                agent.SetDestination(position);
        }
        
        void SendFollowCommand(Transform target)
        {
            NornMotor motor = SelectedOrganism.GetComponent<NornMotor>();
            if (motor != null)
                motor.MoveToTarget(target);
        }
        
        bool IsValidNavMeshPosition(Vector3 position, out Vector3 validPosition)
        {
            validPosition = position;
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas);
        }

        void ShowCommandFeedback(Vector3 fromPosition, Vector3 toPosition)
        {
            if (commandLineRenderer != null)
            {
                if (commandLineCoroutine != null)
                    StopCoroutine(commandLineCoroutine);
                commandLineCoroutine = StartCoroutine(ShowCommandLine(fromPosition, toPosition));
            }
            
            if (commandMarkerPrefab != null)
                ShowCommandMarker(toPosition);
        }
        
        System.Collections.IEnumerator ShowCommandLine(Vector3 start, Vector3 end)
        {
            commandLineRenderer.enabled = true;
            
            float elapsed = 0f;
            while (elapsed < commandLineDuration)
            {
                if (SelectedOrganism != null)
                    start = SelectedOrganism.transform.position;
                
                commandLineRenderer.SetPosition(0, start + Vector3.up * 0.5f);
                commandLineRenderer.SetPosition(1, end + Vector3.up * 0.1f);
                
                float alpha = 1f - (elapsed / commandLineDuration);
                Color c = Color.green;
                c.a = alpha;
                commandLineRenderer.startColor = c;
                commandLineRenderer.endColor = c;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            commandLineRenderer.enabled = false;
        }
        
        void ShowCommandMarker(Vector3 position)
        {
            if (activeCommandMarker != null)
                Destroy(activeCommandMarker);
            
            activeCommandMarker = Instantiate(commandMarkerPrefab, position, Quaternion.identity);
            Destroy(activeCommandMarker, commandMarkerDuration);
        }

        public void Select(Organism organism)
        {
            if (organism != null && organism.IsAlive)
                SelectOrganism(organism);
        }
        
        public void Deselect()
        {
            ClearSelection();
        }
        
        public void CommandTo(Vector3 position)
        {
            if (SelectedOrganism != null)
                CommandToPosition(position);
        }
        
        public bool IsSelected(Organism organism)
        {
            return SelectedOrganism == organism;
        }
        
        public void SetSelectionColor(Color color)
        {
            selectionColor = color;
            if (activeIndicator != null)
                activeIndicator.SetColor(color);
        }
    }
}
