using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Albia.World.History
{
    /// <summary>
    /// Stores and queries historical data.
    /// Generates prose from raw events.
    /// </summary>
    public class HistoryLedger
    {
        public List<HistoricalEvent> Events { get; }
        public Dictionary<string, PopulationHistory> Populations { get; }

        public HistoryLedger(List<HistoricalEvent> events, Dictionary<string, PopulationHistory> populations)
        {
            Events = events;
            Populations = populations;
        }

        /// <summary>
        /// Get events for a specific region.
        /// </summary>
        public List<HistoricalEvent> GetEventsForRegion(int regionX, int regionZ)
        {
            return Events.Where(e => e.RegionX == regionX && e.RegionZ == regionZ).ToList();
        }

        /// <summary>
        /// Get events in Year range.
        /// </summary>
        public List<HistoricalEvent> GetEventsInRange(int startYear, int endYear)
        {
            return Events.Where(e => e.Year >= startYear && e.Year <= endYear).ToList();
        }

        /// <summary>
        /// Generate prose vignette for starting region.
        /// </summary>
        public string GenerateRegionDescription(int regionX, int regionZ)
        {
            var events = GetEventsForRegion(regionX, regionZ);
            var population = Populations.Values.FirstOrDefault(p => p.RegionX == regionX && p.RegionZ == regionZ);
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Region ({regionX}, {regionZ})");
            sb.AppendLine();
            
            // Population summary
            if (population != null)
            {
                sb.AppendLine($"Current Population: {population.CurrentPopulation} {population.Species}");
                sb.AppendLine($"Historical: Started with {population.InitialPopulation}");
            }
            
            // Key events
            if (events.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Historical Events:");
                foreach (var e in events.Take(5)) // Top 5
                {
                    sb.AppendLine($"  Year {e.Year}: {e.Description}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Get archaeological sites for region.
        /// </summary>
        public List<ArchaeologicalSite> GenerateArchaeologicalSites(int regionX, int regionZ)
        {
            var sites = new List<ArchaeologicalSite>();
            var events = GetEventsForRegion(regionX, regionZ);
            
            // War sites become battlefields
            foreach (var war in events.Where(e => e.Type == HistoricalEventType.War))
            {
                sites.Add(new ArchaeologicalSite
                {
                    SiteType = ArchaeologicalSiteType.Battlefield,
                    Year = war.Year,
                    Description = war.Description
                });
            }
            
            // Old settlements (regions with population > 100 at any point)
            var pop = Populations.Values.FirstOrDefault(p => p.RegionX == regionX && p.RegionZ == regionZ);
            if (pop != null && pop.HadPopulation && pop.InitialPopulation > 20)
            {
                sites.Add(new ArchaeologicalSite
                {
                    SiteType = ArchaeologicalSiteType.Ruins,
                    Year = _yearsToSimulate / 2, // Approximate
                    Description = $"Ancient {pop.Species} settlement"
                });
            }
            
            return sites;
        }

        private int _yearsToSimulate = 500;
    }

    public class ArchaeologicalSite
    {
        public ArchaeologicalSiteType SiteType;
        public int Year;
        public string Description;
        
        public Vector3 GetWorldPosition(int chunkSize)
        {
            // Convert region to approximate world position
            int rx = 0, rz = 0; // Need to extract from context
            return new Vector3(rx * 32 * chunkSize, 0, rz * 32 * chunkSize);
        }
    }

    public enum ArchaeologicalSiteType
    {
        Ruins,
        Battlefield,
        Fossil,
        ArtifactCache
    }
}
