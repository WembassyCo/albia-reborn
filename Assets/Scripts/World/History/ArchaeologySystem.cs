using System.Collections.Generic;
using UnityEngine;

namespace Albia.World.History
{
    /// <summary>
    /// Places physical traces of history in the world.
    /// Ruins, battlefields, fossils, artifacts.
    /// </summary>
    public class ArchaeologySystem : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject RuinsPrefab;
        public GameObject BattlefieldPrefab;
        public GameObject FossilPrefab;
        public GameObject ArtifactPrefab;

        private HistoryLedger _ledger;

        public void Initialize(HistoryLedger ledger)
        {
            _ledger = ledger;
        }

        /// <summary>
        /// Place archaeological sites in the world.
        /// Called after world generation.
        /// </summary>
        public void PlaceSitesInRegion(int regionX, int regionZ, int worldChunkSize)
        {
            var sites = _ledger.GenerateArchaeologicalSites(regionX, regionZ);
            
            foreach (var site in sites)
            {
                Vector3 pos = GetRandomPositionInRegion(regionX, regionZ, worldChunkSize);
                PlaceSite(site, pos);
            }
        }

        void PlaceSite(ArchaeologicalSite site, Vector3 position)
        {
            GameObject prefab = site.SiteType switch
            {
                ArchaeologicalSiteType.Ruins => RuinsPrefab,
                ArchaeologicalSiteType.Battlefield => BattlefieldPrefab,
                ArchaeologicalSiteType.Fossil => FossilPrefab,
                ArchaeologicalSiteType.ArtifactCache => ArtifactPrefab,
                _ => null
            };

            if (prefab == null) return;

            GameObject instance = Instantiate(prefab, position, Random.rotation);
            
            // Add decay based on age
            float decay = (500f - site.Year) / 500f; // 500 years of history
            
            // Scale down for older sites
            float scale = 1f - (decay * 0.5f);
            instance.transform.localScale *= scale;
            
            // Visual decay
            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color *= 1f - (decay * 0.3f); // Darker with age
                renderer.material.color = color;
            }
            
            // Add description component
            var siteInfo = instance.AddComponent<ArchaeologicalMarker>();
            siteInfo.Description = site.Description;
            siteInfo.Age = 500 - site.Year;
            siteInfo.SiteType = site.SiteType;
        }

        Vector3 GetRandomPositionInRegion(int regionX, int regionZ, int chunkSize)
        {
            // Region is 32x32 tiles
            float x = (regionX * 32 + Random.Range(0, 32)) * chunkSize;
            float z = (regionZ * 32 + Random.Range(0, 32)) * chunkSize;
            float y = 0; // Ground level, adjust later
            
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Marker component for archaeological sites.
    /// </summary>
    public class ArchaeologicalMarker : MonoBehaviour
    {
        public string Description;
        public int Age; // Years
        public ArchaeologicalSiteType SiteType;
        
        // Interaction
        void OnMouseDown()
        {
            Debug.Log($"[{SiteType}] {Description} (from {Age} years ago)");
            
            // Log discovery
            WorldEventLogger.Instance?.LogEvent(WorldEventType.Cultural, 
                $"Discovered {SiteType}: {Description}", transform.position);
        }
    }
}
