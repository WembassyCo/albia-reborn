using UnityEngine;

namespace Albia.Player
{
    /// <summary>
    /// Visual ring indicator around selected creatures.
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("Visual")]
        public LineRenderer ringRenderer;
        public Renderer highlightRenderer;
        
        [Header("Settings")]
        public int ringSegments = 32;
        public float baseRadius = 0.6f;
        public float pulseAmount = 0.1f;
        public float heightOffset = 0.05f;
        public Color selectedColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        
        private Transform targetTransform;
        private Color currentColor;
        private float pulseTime;
        private float currentRadius;
        private bool isVisible;

        void Awake()
        {
            CreateRing();
            currentColor = selectedColor;
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
                GameObject ring = new GameObject("Ring");
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
                    Mathf.Sin(angle) * currentRadius);
            }
            
            ringRenderer.SetPositions(points);
            ringRenderer.material.color = currentColor;
        }

        void Animate()
        {
            pulseTime += Time.deltaTime * 2f;
            float pulse = Mathf.Sin(pulseTime) * 0.5f + 0.5f;
            currentRadius = baseRadius + pulse * pulseAmount;
        }

        public void SetTarget(Transform target)
        {
            targetTransform = target;
            
            if (target != null && target.GetComponent<Collider>() != null)
            {
                var bounds = target.GetComponent<Collider>().bounds;
                float r = Mathf.Max(bounds.extents.x, bounds.extents.z);
                baseRadius = r * 1.2f;
            }
        }

        public void SetColor(Color color)
        {
            selectedColor = color;
            currentColor = color;
            if (ringRenderer != null)
                ringRenderer.material.color = color;
        }

        public void Show()
        {
            isVisible = true;
            pulseTime = 0f;
            if (ringRenderer != null) ringRenderer.enabled = true;
        }

        public void Hide()
        {
            isVisible = false;
            if (ringRenderer != null) ringRenderer.enabled = false;
        }

        public void UpdatePosition()
        {
            if (targetTransform == null) return;
            
            float y = targetTransform.position.y;
            if (targetTransform.GetComponent<Collider>() != null)
                y = targetTransform.GetComponent<Collider>().bounds.min.y;
            
            transform.position = new Vector3(
                targetTransform.position.x,
                y + heightOffset,
                targetTransform.position.z);
        }
    }
}
