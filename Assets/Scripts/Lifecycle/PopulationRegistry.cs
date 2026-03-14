using UnityEngine;
using Albia.Core;
using Albia.Creatures;
using System;
using System.Collections.Generic;

namespace Albia.Lifecycle
{
    public class PopulationRegistry : MonoBehaviour
    {
        public static PopulationRegistry Instance { get; private set; }
        [SerializeField] private List<Organism> allNorns = new List<Organism>();
        
        public event Action<Organism> OnOrganismRegistered;
        public event Action<Organism> OnOrganismDeregistered;
        public event Action<Organism> OnOrganismDeath;
        public event Action OnPopulationChanged;

        void Awake() { Instance = this; }

        public void RegisterOrganism(Organism organism)
        {
            if (!allNorns.Contains(organism))
            {
                allNorns.Add(organism);
                organism.OnDeath += OnOrganismDied;
                OnOrganismRegistered?.Invoke(organism);
                OnPopulationChanged?.Invoke();
            }
        }

        public void DeregisterOrganism(Organism organism)
        {
            if (allNorns.Contains(organism))
            {
                allNorns.Remove(organism);
                organism.OnDeath -= OnOrganismDied;
                OnOrganismDeregistered?.Invoke(organism);
                OnPopulationChanged?.Invoke();
            }
        }

        private void OnOrganismDied(Organism organism)
        {
            OnOrganismDeath?.Invoke(organism);
            OnPopulationChanged?.Invoke();
        }

        public List<Organism> GetAllNorns()
        {
            allNorns.RemoveAll(org => org == null || !org.IsAlive);
            return new List<Organism>(allNorns);
        }

        public int GetPopulationCount() => GetAllNorns().Count;

        public List<Organism> GetNornsInArea(Vector3 center, float radius)
        {
            var result = new List<Organism>();
            foreach (var norn in GetAllNorns())
            {
                if (Vector3.Distance(center, norn.transform.position) <= radius)
                    result.Add(norn);
            }
            return result;
        }
    }
}
