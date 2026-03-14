using UnityEngine;

namespace Albia.Player
{
    /// <summary>
    /// Visual indicator that appears around selected creatures.
    /// Draws a ring or highlight around the target transform.
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private LineRenderer ringRenderer;
        [SerializeField] private Renderer highlightRenderer;
        
        [Header("Ring Settings")]
        [SerializeField] private int ringSegments = 32;
        [SerializeField] private float baseRadius = 0.6f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.1f;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private float heightOffset = 0.05f;
        
        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 0.3f, 0.6f);
        
        [Header("Animation")]
        [SerializeField] private bool animatePulse = true;
        [SerializeField] private bool animateRotation = true;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.1f;
        
        // Target tracking
        private Transform targetTransform;
        private Collider targetCollider;
        private Vector3 lastTargetPosition;
        private bool isVisible;
        
        // Animation state
        private float pulseTime;
        private float currentAlpha = 0f;
        private float targetAlpha = 1f;
        private float baseIntensity;
        
        // Cached values
        private Vector3[] ringPoints;
        private Color currentColor;
        private float currentRadius;

        void Awake()
        {
            InitializeComponents();
            CreateRingMesh();
        }

        void Update()
        {
            if (!isVisible) return;
            
            UpdatePosition();
            AnimateIndicator();
            UpdateVisuals();
        }

        void OnEnable()
        {
            if (ringRenderer != null)
            {
                ringRenderer.enabled = true;
            }
        }

        void OnDisable()
        {
            if (ringRenderer != null)
            {
                ringRenderer.enabled = false;
            }
        }

        #region Initialization
        
        private void InitializeComponents()
        {
            // Create line renderer if not set
            if (ringRenderer == null)
            {
                GameObject ringObj = new GameObject("SelectionRing");
                ringObj.transform.SetParent(transform, false);
                ringRenderer = ringObj.AddComponent<LineRenderer>();
                
                // Configure line renderer
                ringRenderer.useWorldSpace = false;
                ringRenderer.loop = true;
                ringRenderer.positionCount = ringSegments + 1;
                ringRenderer.startWidth = 0.02f;
                ringRenderer.endWidth = 0.02f;
                
                // Create material
                Material ringMaterial = new Material(Shader.Find("Sprites/Default"));
                ringMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ringMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ringMaterial.SetInt("_ZWrite", 0);
                ringMaterial.DisableKeyword("_ALPHATEST_ON");
                ringMaterial.EnableKeyword("_ALPHABLEND_ON");
                ringMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ringMaterial.renderQueue = 3000;
                ringMaterial.color = selectedColor;
                ringRenderer.material = ringMaterial;
            }
            
            // Create highlight renderer if not set (simple quad)
            if (highlightRenderer == null)
            {
                GameObject highlightObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlightObj.name = "SelectionHighlight";
                highlightObj.transform.SetParent(transform, false);
                highlightObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                Destroy(highlightObj.GetComponent<Collider>()); // Remove collider
                
                highlightRenderer = highlightObj.GetComponent<Renderer>();
                
                // Create semitransparent material
                Material highlightMat = new Material(Shader.Find("Sprites/Default"));
                highlightMat.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f);
                highlightRenderer.material = highlightMat;
            }
            
            // Initialize arrays
            ringPoints = new Vector3[ringSegments + 1];
            currentColor = selectedColor;
            baseIntensity = selectedColor.a;
        }
        
        private void CreateRingMesh()
        {
            if (ringRenderer == null) return;
            
            // Calculate ring points
            float angleStep = 360f / ringSegments * Mathf.Deg2Rad;
            
            for (int i = 0; i <= ringSegments; i++)
            {
                float angle = i * angleStep;
                ringPoints[i] = new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0,
                    Mathf.Sin(angle) * currentRadius
                );
            }
            
            ringRenderer.SetPositions(ringPoints);
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Set the target transform to follow
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            
            // Get collider for bounds calculation
            if (target != null)
            {
                targetCollider = target.GetComponent<Collider>();
                if (targetCollider == null)
                {
                    targetCollider = target.GetComponentInChildren<Collider>();
                }
                
                // Update radius based on target bounds
                UpdateRadiusFromTarget();
            }
        }
        
        /// <summary>
        /// Set the vertical offset from the target
        /// </summary>
        public void SetOffset(float offset)
        {
            heightOffset = offset;
        }
        
        /// <summary>
        /// Set the indicator color
        /// </summary>
        public void SetColor(Color color)
        {
            selectedColor = color;
            currentColor = color;
            baseIntensity = color.a;
            
            if (ringRenderer != null && ringRenderer.material != null)
            {
                ringRenderer.material.color = color;
            }
            
            if (highlightRenderer != null && highlightRenderer.material != null)
            {
                highlightRenderer.material.color = new Color(color.r, color.g, color.b, 0.3f);
            }
        }
        
        /// <summary>
        /// Show the indicator
        /// </summary>
        public void Show()
        {
            isVisible = true;
            targetAlpha = 1f;
            
            if (ringRenderer != null)
            {
                ringRenderer.enabled = true;
            }
            
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = true;
            }
            
            // Reset pulse
            pulseTime = 0f;
        }
        
        /// <summary>
        /// Hide the indicator
        /// </summary>
        public void Hide()
        {
            targetAlpha = 0f;
        }
        
        /// <summary>
        /// Update position (called every frame)
        /// </summary>
        public void UpdatePosition()
        {
            if (targetTransform == null) return;
            
            // Calculate position at the base of the target
            float targetY = targetTransform.position.y;
            
            // If we have a collider, use its base
            if (targetCollider != null)
            {
                targetY = targetCollider.bounds.min.y;
            }
            
            // Position indicator
            transform.position = new Vector3(
                targetTransform.position.x,
                targetY + heightOffset,
                targetTransform.position.z
            );
            
            // Face camera
            if (Camera.main != null)
            {
                highlightRenderer.transform.LookAt(Camera.main.transform);
                highlightRenderer.transform.rotation = Quaternion.Euler(
                    90,
                    highlightRenderer.transform.rotation.eulerAngles.y,
                    0
                );
            }
            
            lastTargetPosition = targetTransform.position;
        }
        
        #endregion

        #region Internal Methods
        
        private void UpdateRadiusFromTarget()
        {
            if (targetCollider != null)
            {
                // Get the larger of width/depth
                Bounds bounds = targetCollider.bounds;
                float targetRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                currentRadius = targetRadius * 1.2f; // 20% larger than target
            }
            else
            {
                currentRadius = baseRadius;
            }
            
            CreateRingMesh();
        }
        
        private void AnimateIndicator()
        {
            // Pulse animation
            if (animatePulse)
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                float pulseFactor = Mathf.Sin(pulseTime) * 0.5f + 0.5f; // 0 to 1
                currentRadius = baseRadius + pulseFactor * pulseAmount;
            }
            
            // Rotation animation
            if (animateRotation && ringRenderer != null)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Fade animation
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, 
                (targetAlpha > currentAlpha ? 1f / fadeInDuration : 1f / fadeOutDuration) * Time.deltaTime);
            
            // Disable when fully faded out
            if (currentAlpha <= 0.01f && targetAlpha <= 0.01f)
            {
                isVisible = false;
                if (ringRenderer != null) ringRenderer.enabled = false;
                if (highlightRenderer != null) highlightRenderer.enabled = false;
            }
        }
        
        private void UpdateVisuals()
        {
            if (ringRenderer == null) return;
            
            // Update ring positions with new radius
            float angleStep = 360f / ringSegments * Mathf.Deg2Rad;
            for (int i = 0; i <= ringSegments; i++)
            {
                float angle = i * angleStep;
                ringPoints[i] = new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0,
                    Mathf.Sin(angle) * currentRadius
                );
            }
            ringRenderer.SetPositions(ringPoints);
            
            // Update colors with alpha
            Color ringColor = currentColor;
            ringColor.a = currentAlpha * baseIntensity;
            ringRenderer.material.color = ringColor;
            
            if (highlightRenderer != null)
            {
                Color highlightColor = currentColor;
                highlightColor.a = currentAlpha * 0.3f;
                highlightRenderer.material.color = highlightColor;
                
                // Scale highlight based on radius
                float scale = currentRadius * 2f;
                highlightRenderer.transform.localScale = new Vector3(scale, scale, 1);
            }
        }
        
        #endregion

        #region Gizmos
        
        void OnDrawGizmos()
        {
            if (!isVisible || targetTransform == null) return;
            
            Gizmos.color = currentColor;
            Gizmos.DrawWireSphere(transform.position, currentRadius);
        }
        
        #endregion
    }
}
