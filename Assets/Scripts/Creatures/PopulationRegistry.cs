using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlbiaReborn.Creatures
{
    /// <summary>
    /// Global registry for all living organisms.
    /// Singleton pattern.
    /// </summary>
    public class PopulationRegistry : MonoBehaviour
    {
        private Dictionary<Guid, Organism> _organisms = new();
        
        public static PopulationRegistry Instance { get; private set; }
        
        public int PopulationCount => _organisms.Count;
        public IEnumerable<Organism> AllOrganisms => _organisms.Values;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(Organism organism)
        {
            if (!_organisms.ContainsKey(organism.OrganismId))
            {
                _organisms[organism.OrganismId] = organism;
            }
        }

        public void Unregister(Organism organism)
        {
            _organisms.Remove(organism.OrganismId);
        }

        public Organism GetOrganism(Guid id)
        {
            _organisms.TryGetValue(id, out Organism organism);
            return organism;
        }

        public List<Organism> GetOrganismsInRange(Vector3 position, float range)
        {
            var results = new List<Organism>();
            float rangeSq = range * range;

            foreach (var organism in _organisms.Values)
            {
                if (organism == null || !organism.gameObject.activeSelf)
                    continue;

                float distSq = (organism.transform.position - position).sqrMagnitude;
                if (distSq <= rangeSq)
                {
                    results.Add(organism);
                }
            }

            return results;
        }

        public List<Organism> GetOrganismsOfSpecies(string speciesName)
        {
            var results = new List<Organism>();
            
            foreach (var organism in _organisms.Values)
            {
                if (organism.Species != null && organism.Species.SpeciesName == speciesName)
                {
                    results.Add(organism);
                }
            }

            return results;
        }
    }
}
