using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Albia.Creatures;
using Albia.Creatures.Genetics;
using Albia.World.Generation;

namespace Albia.World.History
{
    /// <summary>
    /// Runs abstract historical simulation before player enters world.
    /// 500 years in ~15 seconds.
    /// </summary>
    public class WorldHistorySimulator
    {
        private int _yearsToSimulate = 500;
        private float _simulationStartTime;
        
        // Populations (abstract - counts, not individual creatures)
        private Dictionary<string, PopulationHistory> _populations;
        private List<HistoricalEvent> _events;
        private System.Random _random;

        // World map regions
        private int _regionSize = 32; // 32x32 tiles per region
        private int _numRegionsX;
        private int _numRegionsZ;

        public WorldHistorySimulator(int seed, int worldWidth, int worldHeight)
        {
            _random = new System.Random(seed);
            _populations = new Dictionary<string, PopulationHistory>();
            _events = new List<HistoricalEvent>();
            
            _numRegionsX = worldWidth / _regionSize;
            _numRegionsZ = worldHeight / _regionSize;
        }

        /// <summary>
        /// Run the simulation.
        /// Returns the history ledger.
        /// </summary>
        public HistoryLedger RunSimulation()
        {
            _simulationStartTime = Time.realtimeSinceStartup;
            
            Debug.Log("Starting world history simulation...");
            
            // Initialize populations
            InitializePopulations();
            
            // Run abstracted years
            for (int year = 0; year < _yearsToSimulate; year++)
            {
                SimulateYear(year);
            }
            
            _simulationStartTime = Time.realtimeSinceStartup - _simulationStartTime;
            Debug.Log($"History simulation complete in {_simulationStartTime:F2} seconds");
            
            return new HistoryLedger(_events, _populations);
        }

        void InitializePopulations()
        {
            // Initial populations per region
            for (int rx = 0; rx < _numRegionsX; rx++)
            {
                for (int rz = 0; rz < _numRegionsZ; rz++)
                {
                    // Norns: 10-30 per region
                    int nornPop = _random.Next(10, 30);
                    AddPopulation("Norn", rx, rz, nornPop);
                    
                    // Grendels: 1-5 per region
                    int grendelPop = _random.Next(1, 5);
                    AddPopulation("Grendel", rx, rz, grendelPop);
                }
            }
        }

        void AddPopulation(string species, int regionX, int regionZ, int count)
        {
            string key = $"{species}_{regionX}_{regionZ}";
            _populations[key] = new PopulationHistory
            {
                Species = species,
                RegionX = regionX,
                RegionZ = regionZ,
                InitialPopulation = count,
                CurrentPopulation = count
            };
        }

        void SimulateYear(int year)
        {
            // For each region, update populations
            foreach (var pop in _populations.Values)
            {
                // Birth rate based on population size (logistic growth)
                float birthRate = 0.1f - (pop.CurrentPopulation * 0.001f);
                int births = _random.Next(0, Mathf.Max(0, (int)(pop.CurrentPopulation * birthRate)));
                
                // Death from disease/hunger
                float deathRate = 0.05f + (pop.CurrentPopulation > 50 ? 0.1f : 0f);
                int deaths = _random.Next(0, (int)(pop.CurrentPopulation * deathRate));
                
                // Predation (Grendels eat Norns)
                int predation = 0;
                if (pop.Species == "Norn")
                {
                    string grendelKey = $"Grendel_{pop.RegionX}_{pop.RegionZ}"";
                    if (_populations.TryGetValue(grendelKey, out var grendelPop))
                    {
                        predation = _random.Next(0, grendelPop.CurrentPopulation * 2);
                    }
                }
                
                pop.CurrentPopulation = pop.CurrentPopulation + births - deaths - predation;
                pop.CurrentPopulation = Mathf.Max(0, pop.CurrentPopulation);
                
                // Check for extinctions
                if (pop.CurrentPopulation == 0 && pop.HadPopulation)
                {
                    LogEvent(year, pop.RegionX, pop.RegionZ, HistoricalEventType.Extinction, 
                        $"{pop.Species} went extinct in region ({pop.RegionX}, {pop.RegionZ})");
                }
                
                // Check for discoveries
                if (_random.NextDouble() < 0.001 && pop.CurrentPopulation > 10) // 0.1% chance per year
                {
                    string discovery = GetRandomDiscovery(pop.Species);
                    LogEvent(year, pop.RegionX, pop.RegionZ, HistoricalEventType.Discovery,
                        $"{pop.Species} discovered {discovery}");
                    pop.Discovered.Add(discovery);
                }
                
                // Check for wars (Norn vs Norn territorial conflict)
                if (pop.Species == "Norn" && pop.CurrentPopulation > 40 && _random.NextDouble() < 0.01)
                {
                    LogEvent(year, pop.RegionX, pop.RegionZ, HistoricalEventType.War,
                        $"Territorial war in region ({pop.RegionX}, {pop.RegionZ})");
                    pop.CurrentPopulation = Mathf.FloorToInt(pop.CurrentPopulation * 0.7f); // 30% casualties
                }
                
                // Check for migrations
                if (pop.CurrentPopulation > 50 && _random.NextDouble() < 0.02)
                {
                    // Migrate to adjacent region
                    int newPop = pop.CurrentPopulation / 3;
                    pop.CurrentPopulation -= newPop;
                    
                    int newX = pop.RegionX + _random.Next(-1, 2);
                    int newZ = pop.RegionZ + _random.Next(-1, 2);
                    
                    if (IsValidRegion(newX, newZ))
                    {
                        string migrationKey = $"{pop.Species}_{newX}_{newZ}"";
                        if (_populations.TryGetValue(migrationKey, out var target))
                        {
                            target.CurrentPopulation += newPop;
                            LogEvent(year, pop.RegionX, pop.RegionZ, HistoricalEventType.Migration,
                                $"Migration to region ({newX}, {newZ})");
                        }
                    }
                }
            }
        }

        bool IsValidRegion(int x, int z)
        {
            return x >= 0 && x < _numRegionsX && z >= 0 && z < _numRegionsZ;
        }

        string GetRandomDiscovery(string species)
        {
            string[] discoveries = species == "Norn" 
                ? new[] { "fire", "stone tools", "plant cultivation", "basic shelter", "medicine" }
                : new[] { "pack hunting", "structure raiding", "scent marking" };
            
            return discoveries[_random.Next(discoveries.Length)];
        }

        void LogEvent(int year, int regionX, int regionZ, HistoricalEventType type, string description)
        {
            _events.Add(new HistoricalEvent
            {
                Year = year,
                RegionX = regionX,
                RegionZ = regionZ,
                Type = type,
                Description = description
            });
        }
    }

    public class PopulationHistory
    {
        public string Species;
        public int RegionX;
        public int RegionZ;
        public int InitialPopulation;
        public int CurrentPopulation;
        public List<string> Discovered = new List<string>();
        public bool HadPopulation => CurrentPopulation > 0 || InitialPopulation > 0;
    }

    public class HistoricalEvent
    {
        public int Year;
        public int RegionX;
        public int RegionZ;
        public HistoricalEventType Type;
        public string Description;
    }

    public enum HistoricalEventType
    {
        Migration,
        War,
        Extinction,
        Discovery,
        Disease,
        ClimateEvent,
        Cultural
    }
}
