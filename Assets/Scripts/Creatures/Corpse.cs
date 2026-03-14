using UnityEngine;
using AlbiaReborn.World.Voxel;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Corpse left after organism dies.
    /// Adds organic matter to tile, decomposes over time.
    /// </summary>
    public class Corpse : MonoBehaviour
    {
        [Header("Decomposition")]
        public float TotalBiomass = 50f;
        public float DecompositionRate = 1f; // per second
        public float CurrentBiomass { get; private set; }
        
        private Vector3Int _tilePosition;
        private float _decayTimer = 0f;
        private bool _decomposing = true;

        void Start()
        {
            CurrentBiomass = TotalBiomass;
            _tilePosition = Vector3Int.FloorToInt(transform.position);
            
            // Visual state
            GetComponent<Renderer>()?.material.SetColor("_Color", Color.gray);
        }

        void Update()
        {
            if (!_decomposing) return;

            float decay = DecompositionRate * Time.deltaTime;
            CurrentBiomass -= decay;

            // Shrink visual
            float scale = CurrentBiomass / TotalBiomass;
            transform.localScale = Vector3.one * scale * 0.5f;

            if (CurrentBiomass <= 0)
            {
                CompleteDecomposition();
            }
        }

        void CompleteDecomposition()
        {
            _decomposing = false;
            
            // TODO: Add nutrients to soil system
            // SoilNutrients.AddToTile(_tilePosition, TotalBiomass * 0.5f);
            
            // Spawn fungi (if system exists)
            // if (Random.value < 0.3f) SpawnFungi();

            Destroy(gameObject);
        }

        /// <summary>
        /// Scavenge some biomass (for Grendels, predators).
        /// </summary>
        public float Scavenge(float amount)
        {
            float taken = Mathf.Min(amount, CurrentBiomass);
            CurrentBiomass -= taken;
            
            if (CurrentBiomass <= 0)
            {
                Destroy(gameObject);
            }
            
            return taken;
        }
    }
}
