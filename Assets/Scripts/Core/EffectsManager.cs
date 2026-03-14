using UnityEngine;
using System.Collections.Generic;

namespace Albia.Core
{
    /// <summary>
    /// Visual effects manager for particle systems
    /// MVP: Simple spawn effects
    /// Full: Complex systems, pooling
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }
        
        [Header("Effects")]
        [SerializeField] private GameObject eatEffectPrefab;
        [SerializeField] private GameObject dieEffectPrefab;
        [SerializeField] private GameObject birthEffectPrefab;
        
        void Awake() => Instance = this;
        
        public void PlayEat(Vector3 position)
        {
            SpawnEffect(eatEffectPrefab, position);
        }
        
        public void PlayDeath(Vector3 position)
        {
            SpawnEffect(dieEffectPrefab, position);
        }
        
        public void PlayBirth(Vector3 position)
        {
            SpawnEffect(birthEffectPrefab, position);
        }
        
        void SpawnEffect(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            Instantiate(prefab, position, Quaternion.identity);
        }
    }
}