using UnityEngine;

namespace Albia.Player
{
    /// <summary>
    /// Smooth follow camera for selected creature
    /// MVP: Basic follow
    /// Full: Smart framing, occlusion detection
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0, 10, -10);
        [SerializeField] private float lookAtOffset = 1f;
        
        private bool isFollowing = false;
        
        void Update()
        {
            if (!isFollowing || target == null) return;
            
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            
            Vector3 lookAt = target.position + Vector3.up * lookAtOffset;
            transform.LookAt(lookAt);
        }
        
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            isFollowing = target != null;
        }
        
        public void StopFollowing() => isFollowing = false;
    }
}