using UnityEngine;
using Albia.Core;

namespace Albia.Ecology
{
    /// <summary>
    /// Basic food source that Norns can consume.
    /// MVP: Simple energy source
    /// Full: Quality, type, spoilage, decomposer integration
    /// </summary>
    public class FoodSource : MonoBehaviour
    {
        [SerializeField] private float nutritionValue = 25f;
        [SerializeField] private float respawnTime = 30f;
        [SerializeField] private GameObject visualMesh;

        public float NutritionValue => nutritionValue;
        public bool IsConsumed { get; private set; } = false;

        void Start()
        {
            // Ensure proper layer for detection
            gameObject.layer = LayerMask.NameToLayer("Food");
            tag = "Food";
        }

        /// <summary>
        /// Try to consume this food source
        /// </summary>
        public bool TryConsume(Organism consumer)
        {
            if (IsConsumed) return false;

            IsConsumed = true;
            consumer.ConsumeEnergy(nutritionValue);
            
            // Disable visual
            gameObject.SetActive(false);
            
            // Respawn after delay
            Invoke(nameof(Respawn), respawnTime);
            
            return true;
        }

        private void Respawn()
        {
            IsConsumed = false;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Set respawn behavior
        /// </summary>
        public void SetRespawnTime(float time) => respawnTime = time;
    }
}