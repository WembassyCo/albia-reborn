using UnityEngine;
using System.Collections.Generic;
using Albia.Core;
using Albia.Lifecycle;

namespace Albia.Creatures
{
    /// <summary>
    /// Social interactions between creatures
    /// MVP: Basic proximity detection
    /// Full: Relationships, pack behavior, communication
    /// </summary>
    public class SocialBehavior : MonoBehaviour
    {
        [SerializeField] private float socialRange = 5f;
        [SerializeField] private float socialInterval = 2f;
        
        private Organism self;
        private float timer = 0f;
        private List<Organism> nearbyCreatures = new List<Organism>();
        
        void Start()
        {
            self = GetComponent<Organism>();
        }
        
        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= socialInterval)
            {
                timer = 0f;
                UpdateSocialState();
            }
        }
        
        void UpdateSocialState()
        {
            nearbyCreatures.Clear();
            
            // Find nearby creatures
            var all = PopulationRegistry.Instance?.GetAllNorns();
            if (all == null) return;
            
            foreach (var creature in all)
            {
                if (creature != self && Vector3.Distance(transform.position, creature.transform.position) < socialRange)
                {
                    nearbyCreatures.Add(creature);
                }
            }
            
            // Update loneliness chemical
            var chemicals = self?.Chemicals;
            if (chemicals != null)
            {
                float loneliness = nearbyCreatures.Count == 0 ? 10f : 0f;
                chemicals.SetLevel(ChemicalType.Loneliness, loneliness);
            }
            
            // SCALES TO: Relationships, offspring tracking, pack formation
        }
        
        public List<Organism> GetNearbyCreatures() => new List<Organism>(nearbyCreatures);
        public bool HasNearbyCreatures => nearbyCreatures.Count > 0;
    }
}