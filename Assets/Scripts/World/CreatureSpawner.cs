using UnityEngine;
using AlbiaReborn.Creatures;
using AlbiaReborn.Creatures.Genetics;

namespace AlbiaReborn.World
{
    /// <summary>
    /// Spawns creatures into the world.
    /// MVP: Static placement, procedural positions.
    /// </summary>
    public class CreatureSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject NornPrefab;
        public GameObject PlantPrefab;
        
        [Header("Spawn Settings")]
        public int InitialNornCount = 5;
        public int InitialPlantCount = 50;
        public float SpawnHeight = 2f;

        [Header("References")]
        public SpeciesTemplate NornTemplate;
        public HeightmapGenerator Heightmap;

        public void SpawnInitialPopulation()
        {
            SpawnPlants();
            SpawnNorns();
        }

        void SpawnNorns()
        {
            for (int i = 0; i < InitialNornCount; i++)
            {
                Vector3 pos = GetRandomGroundPosition();
                SpawnNorn(pos);
            }
        }

        void SpawnPlants()
        {
            for (int i = 0; i < InitialPlantCount; i++)
            {
                Vector3 pos = GetRandomGroundPosition();
                SpawnPlant(pos);
            }
        }

        GameObject SpawnNorn(Vector3 position)
        {
            if (NornPrefab == null) return null;

            GameObject nornObj = Instantiate(NornPrefab, position, Quaternion.identity);
            Norn norn = nornObj.GetComponent<Norn>();

            if (norn != null)
            {
                // Generate genome from template
                if (NornTemplate != null)
                {
                    norn.Species = NornTemplate;
                    norn.Genome = GenomeData.FromTemplate(NornTemplate);
                }
                else
                {
                    // Default genome
                    norn.Genome = new GenomeData();
                }

                norn.InitializeFromGenome();
            }

            return nornObj;
        }

        GameObject SpawnPlant(Vector3 position)
        {
            if (PlantPrefab == null) return null;
            
            return Instantiate(PlantPrefab, position, Quaternion.identity);
        }

        Vector3 GetRandomGroundPosition()
        {
            if (Heightmap == null)
            {
                // Fallback: random position
                return new Vector3(
                    UnityEngine.Random.Range(-50f, 50f),
                    SpawnHeight,
                    UnityEngine.Random.Range(-50f, 50f)
                );
            }

            int x = UnityEngine.Random.Range(0, Heightmap.Width);
            int z = UnityEngine.Random.Range(0, Heightmap.Height);
            float height = Heightmap.GetHeightAt(x, z);
            
            return new Vector3(x, height * 10f + SpawnHeight, z);
        }

        /// <summary>
        /// Spawn specific creature at position (for player tools).
        /// </summary>
        public GameObject SpawnCreatureAt(Vector3 position, SpeciesTemplate template)
        {
            if (template == null) return null;

            // Determine prefab from template
            GameObject prefab = template.SpeciesName switch
            {
                "Norn" => NornPrefab,
                _ => NornPrefab
            };

            if (prefab == null) return null;

            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            var organism = obj.GetComponent<Organism>();
            
            if (organism != null)
            {
                organism.Species = template;
                organism.Genome = GenomeData.FromTemplate(template);
                organism.InitializeFromGenome();
            }

            return obj;
        }
    }
}
