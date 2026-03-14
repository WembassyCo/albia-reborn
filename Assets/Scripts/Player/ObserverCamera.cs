using UnityEngine;
using UnityEngine.InputSystem;
using Albia.Core;

namespace Albia.Player
{
    /// <summary>
    /// Camera controller for observing the world.
    /// MVP: Free roam camera
    /// Full: Creature follow, inspection, world tools
    /// </summary>
    public class ObserverCamera : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveSpeed = 30f;
        [SerializeField] private float scrollSpeed = 50f;
        [SerializeField] private float minHeight = 2f;
        [SerializeField] private float maxHeight = 100f;

        [Header("Rotation")]
        [SerializeField] private float rotateSpeed = 3f;
        
        [Header("Bounds")]
        [SerializeField] private float worldSize = 100f;

        private Vector2 moveInput;
        private float heightInput;
        private bool isRotating;
        private Vector3 velocity = Vector3.zero;

        // SCALES TO:
        // - Follow target (track specific Norn)
        // - Inspection state (zoom to creature)
        // - World Tools (terrain editing)
        // - Time scale controls

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleHeight();
            ApplyBounds();
        }

        private void HandleMovement()
        {
            // WASD movement
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            bool fast = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            float speed = fast ? fastMoveSpeed : moveSpeed;

            if (Keyboard.current.wKey.isPressed)
                transform.position += forward * speed * Time.deltaTime;
            if (Keyboard.current.sKey.isPressed)
                transform.position -= forward * speed * Time.deltaTime;
            if (Keyboard.current.aKey.isPressed)
                transform.position -= right * speed * Time.deltaTime;
            if (Keyboard.current.dKey.isPressed)
                transform.position += right * speed * Time.deltaTime;
        }

        private void HandleRotation()
        {
            // Right mouse drag to rotate
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                transform.Rotate(Vector3.up, delta.x * rotateSpeed * 0.01f, Space.World);
                transform.Rotate(Vector3.right, -delta.y * rotateSpeed * 0.01f, Space.Self);
            }
        }

        private void HandleHeight()
        {
            // Scroll wheel for height
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                Vector3 pos = transform.position;
                pos.y += scroll * scrollSpeed * Time.deltaTime;
                transform.position = pos;
            }
        }

        private void ApplyBounds()
        {
            // Clamp height
            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            
            // Clamp horizontal position
            pos.x = Mathf.Clamp(pos.x, -worldSize, worldSize);
            pos.z = Mathf.Clamp(pos.z, -worldSize, worldSize);
            
            transform.position = pos;
        }

        /// <summary>
        /// SCALES TO: Follow specific creature
        /// </summary>
        public void StartFollowing(Transform target)
        {
            // TODO: Implement follow camera
        }

        /// <summary>
        /// SCALES TO: Stop following
        /// </summary>
        public void StopFollowing()
        {
            // TODO: Return to free roam
        }

        /// <summary>
        /// SCALES TO: Set time dilation
        /// </summary>
        public void SetTimeScale(float scale)
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetTimeScale(scale);
        }
    }
}