using System;
using UnityEngine;
using Albia.Creatures;
using Albia.Core;

namespace Albia.World.History
{
    /// <summary>
    /// Tracks notable events in the simulation.
    /// </summary>
    public class WorldEventLogger : MonoBehaviour
    {
        public static WorldEventLogger Instance { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            SubscribeToEvents();
        }

        void SubscribeToEvents()
        {
            PopulationRegistry.Instance.OnOrganismSpawned += OnCreatureBorn;
            // Subscribe to death events from all organisms
        }

        void OnCreatureBorn(Organism organism)
        {
            LogEvent(WorldEventType.Birth, $"{organism.OrganismName} ({organism.Species?.SpeciesName}) born", organism.transform.position);
        }

        public void LogEvent(WorldEventType type, string description, Vector3 position)
        {
            var time = TimeManager.Instance;
            string timestamp = time != null 
                ? $"Year {time.CurrentYear}, Day {time.CurrentDay}"
                : $"Time {Time.time:F0}s";
            
            Debug.Log($"[Event] [{timestamp}] {type}: {description}");
            
            // TODO: Store in ledger
        }

        public void LogFirstWords(Organism norn, string word)
        {
            LogEvent(WorldEventType.Cultural, $"{norn.OrganismName} learned first word: \"{word}\"", norn.transform.position);
        }

        public void LogFirstStructure()
        {
            LogEvent(WorldEventType.Cultural, "First structure built", Vector3.zero);
        }

        public void LogExtinction(string speciesName)
        {
            LogEvent(WorldEventType.Ecological, $"{speciesName} has gone extinct", Vector3.zero);
        }
    }

    public enum WorldEventType
    {
        Birth,
        Death,
        Cultural,
        Ecological,
        Climate,
        Discovery
    }
}
