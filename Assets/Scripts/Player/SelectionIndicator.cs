using UnityEngine;

namespace Albia.Player
{
    /// <summary>
    /// Visual indicator around selected creatures.
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Visual")]
        public LineRenderer ringRenderer;
        public Renderer highlightRenderer;
        
        [Header("Ring")]
        public int ringSegments = 32;
        public float baseRadius = 0.6f;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.1f;
        public float heightOffset = 0.05f;
        
        [Header("Animation")]
        public Color selectedColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        public bool animatePulse = true;
        public float fadeInDuration = 0.2f;
        
        private Transform targetTransform;
        private float pulseTime;
        private float currentAlpha;
        private Vector3[] ringPoints;
        private Color currentColor;
        private float currentRadius;
        private bool isVisible;

        void Awake()
        {
            CreateRing();
            currentColor = selectedColor;
            ringPoints = new Vector3[ringSegments + 1];
        }

        void Update()
        {
            if (!isVisible || targetTransform == null) return;
            
            UpdatePosition();
            Animate();
            DrawRing();
        }

        void CreateRing()
        {
            if (ringRenderer == null)
            {
                GameObject ring = new GameObject("SelectionRing");
                ring.transform.SetParent(transform, false);
                ringRenderer = ring.AddComponent<LineRenderer>();
                
                ringRenderer.useWorldSpace = false;
                ringRenderer.loop = true;
                ringRenderer.positionCount = ringSegments + 1;
                ringRenderer.startWidth = 0.02f;
                ringRenderer.endWidth = 0.02f;
                
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = selectedColor;
                ringRenderer.material = mat;
            }
            
            if (highlightRenderer == null)
            {
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlight.name = "Highlight";
                highlight.transform.SetParent(transform, false);
                highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
                Destroy(highlight.GetComponent<Collider>());
                
                highlightRenderer = highlight.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.3f);
                highlightRenderer.material = mat;
            }
        }

        void DrawRing()
        {
            if (ringRenderer == null) return;
            
            float angleStep = 360f / ringSegments * Mathf.Deg2Rad;
            Vector3[] points = new Vector3[ringSegments + 1];
            
            for (int i = 0; i <= ringSegments; i++)
            {
                float angle = i * angleStep;
                points[i] = new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    0,
                    Mathf.Sin(angle) * currentRadius
                );
            }
            
            ringRenderer.SetPositions(points);
            ringRenderer.material.color = currentColor;
            
            if (highlightRenderer != null)
            {
                highlightRenderer.material.color = new Color(
                    currentColor.r, currentColor.g, currentColor.b, 0.3f * currentAlpha
                );
                
                float scale = currentRadius * 2f;
                highlightRenderer.transform.localScale = new Vector3(scale, scale, 1);
            }
        }

        void Animate()
        {
            if (animatePulse)
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                float pulseFactor = Mathf.Sin(pulseTime) * 0.5f + 0.5f;
                currentRadius = baseRadius + pulseFactor * pulseAmount;
            }
            
            currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, Time.deltaTime / fadeInDuration);
            
            currentColor = selectedColor;
            currentColor.a *= currentAlpha;
        }

        public void SetTarget(Transform target)
        {
            targetTransform = target;
            
            if (target != null && target.GetComponent<Collider>() != null)
            {
                var bounds = target.GetComponent<Collider>().bounds;
                float targetRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                baseRadius = targetRadius * 1.2f;
            }
        }

        public void SetColor(Color color)
        {
            selectedColor = color;
            currentColor = color;
            
            if (ringRenderer != null)
                ringRenderer.material.color = color;
        }

        public void SetOffset(float offset)
        {
            heightOffset = offset;
        }

        public void Show()
        {
            isVisible = true;
            currentAlpha = 0f;
            pulseTime = 0f;
            
            if (ringRenderer != null) ringRenderer.enabled = true;
            if (highlightRenderer != null) highlightRenderer.enabled = true;
        }

        public void Hide()
        {
            isVisible = false;
            if (ringRenderer != null) ringRenderer.enabled = false;
            if (highlightRenderer != null) highlightRenderer.enabled = false;
        }

        public void UpdatePosition()
        {
            if (targetTransform == null) return;
            
            float targetY = targetTransform.position.y;
            
            if (targetTransform.GetComponent<Collider>() != null)
                targetY = targetTransform.GetComponent<Collider>().bounds.min.y;
            
            transform.position = new Vector3(
                targetTransform.position.x,
                targetY + heightOffset,
                targetTransform.position.z
            );
            
            // Face camera
            if (Camera.main != null)
            {
                highlightRenderer.transform.LookAt(Camera.main.transform);
                highlightRenderer.transform.rotation = Quaternion.Euler(90, highlightRenderer.transform.rotation.eulerAngles.y, 0);
            }
        }
    }
}
