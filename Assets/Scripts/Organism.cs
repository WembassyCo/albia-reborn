using UnityEngine;

namespace Albia.Creatures
{
    /// <summary>
    /// Base class for all living organisms in Albia.
    /// Provides common functionality for life cycle, health, and energy management.
    /// </summary>
    public abstract class Organism : MonoBehaviour
    {
        [Header("Organism Base Properties")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float maxEnergy = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float currentEnergy;
        [SerializeField] protected float age = 0f;
        [SerializeField] protected bool isAlive = true;
        
        [Header("Visual")]
        [SerializeField] protected MeshRenderer meshRenderer;
        
        // Properties
        public float Health => currentHealth;
        public float Energy => currentEnergy;
        public float Age => age;
        public bool IsAlive => isAlive;
        public float MaxHealth => maxHealth;
        public float MaxEnergy => maxEnergy;
        
        // Events
        public System.Action OnDeath;
        public System.Action<float> OnHealthChanged;
        public System.Action<float> OnEnergyChanged;
        
        protected virtual void Awake()
        {
            Initialize();
        }
        
        protected virtual void Initialize()
        {
            currentHealth = maxHealth;
            currentEnergy = maxEnergy;
            age = 0f;
            isAlive = true;
            
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            }
        }
        
        protected virtual void Update()
        {
            if (!isAlive) return;
            
            UpdateAge(Time.deltaTime);
            UpdateBiology(Time.deltaTime);
        }
        
        /// <summary>
        /// Updates the age of the organism
        /// </summary>
        protected virtual void UpdateAge(float deltaTime)
        {
            age += deltaTime;
        }
        
        /// <summary>
        /// Override to implement biological processes (metabolism, etc.)
        /// </summary>
        protected abstract void UpdateBiology(float deltaTime);
        
        /// <summary>
        /// Modifies health by the given amount
        /// </summary>
        public virtual void ModifyHealth(float amount)
        {
            if (!isAlive) return;
            
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
            
            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Modifies energy by the given amount
        /// </summary>
        public virtual void ModifyEnergy(float amount)
        {
            if (!isAlive) return;
            
            currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
        }
        
        /// <summary>
        /// Kills the organism
        /// </summary>
        public virtual void Die()
        {
            if (!isAlive) return;
            
            isAlive = false;
            currentHealth = 0f;
            OnDeath?.Invoke();
        }
        
        /// <summary>
        /// Revives the organism (for testing/debugging)
        /// </summary>
        public virtual void Revive()
        {
            isAlive = true;
            currentHealth = maxHealth;
            currentEnergy = maxEnergy;
        }
        
        /// <summary>
        /// Gets the current life stage (0-1 where 1 is max age)
        /// </summary>
        public virtual float GetLifeStage()
        {
            return Mathf.Clamp01(age / 100f); // Default max age of 100
        }
        
        /// <summary>
        /// Destroys the organism game object
        /// </summary>
        protected virtual void DestroyOrganism()
        {
            Destroy(gameObject);
        }
    }
}
