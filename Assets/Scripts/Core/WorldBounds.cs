using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Defines and enforces world boundaries
    /// MVP: Simple bounds
    /// Full: Wrapping, multiple zones
    /// </summary>
    public class WorldBounds : MonoBehaviour
    {
        public static WorldBounds Instance { get; private set; }
        
        [SerializeField] private float width = 100f;
        [SerializeField] private float depth = 100f;
        [SerializeField] private float minHeight = -10f;
        [SerializeField] private float maxHeight = 50f;
        
        void Awake() => Instance = this;
        
        public Vector3 ClampPosition(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, -width/2, width/2);
            pos.z = Mathf.Clamp(pos.z, -depth/2, depth/2);
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            return pos;
        }
        
        public bool IsInside(Vector3 pos)
        {
            return pos.x >= -width/2 && pos.x <= width/2 &&
                   pos.z >= -depth/2 && pos.z <= depth/2;
        }
        
        public Vector3 GetRandomPosition()
        {
            return new Vector3(
                Random.Range(-width/2, width/2),
                1f,
                Random.Range(-depth/2, depth/2)
            );
        }
        
        public Vector3 GetEdgePosition()
        {
            // Random position on world edge
            if (Random.value > 0.5f)
            {
                return new Vector3(
                    Random.Range(-width/2, width/2),
                    1f,
                    Random.value > 0.5f ? -depth/2 : depth/2
                );
            }
            else
            {
                return new Vector3(
                    Random.value > 0.5f ? -width/2 : width/2,
                    1f,
                    Random.Range(-depth/2, depth/2)
                );
            }
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 center = Vector3.zero;
            Vector3 size = new Vector3(width, maxHeight - minHeight, depth);
            Gizmos.DrawWireCube(center + Vector3.up * (maxHeight + minHeight)/2, size);
        }
    }
}