using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Simple visual indicator for food (scales to proper mesh + biome variants)
    /// </summary>
    public class FoodSource : MonoBehaviour
    {
        [SerializeField] private float nutritionValue = 25f;
        [SerializeField] private float respawnTime = 60f;

        public float NutritionValue => nutritionValue;

        // Pulse animation
        private float bobOffset;

        private void Start()
        {
            bobOffset = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            // Simple bob animation
            float y = Mathf.Sin(Time.time * 2f + bobOffset) * 0.1f;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                0.5f + y,
                transform.localPosition.z
            );
        }

        public void Consume(Organism consumer)
        {
            consumer.ConsumeEnergy(nutritionValue);
            gameObject.SetActive(false);
            
            // SCALES TO: Food quality, chemical effects, regrowth
            Invoke(nameof(Reactivate), respawnTime);
        }

        private void Reactivate()
        {
            gameObject.SetActive(true);
        }
    }
}