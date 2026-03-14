using UnityEngine;
using AlbiaReborn.World.Voxel;
using AlbiaReborn.Creatures;

namespace AlbiaReborn.Player
{
    /// <summary>
    /// Player building system - place structures.
    /// Week 18: Steward Tools.
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        [Header("Building")]
        public VoxelType CurrentMaterial = VoxelType.Stone;
        public int BuildRange = 5;
        public float BuildCooldown = 0.5f;

        [Header("Placement")]
        public GameObject GhostBlock;
        public Material GhostMaterialValid;
        public Material GhostMaterialInvalid;

        private float _lastBuildTime;
        private Vector3 _targetPos;
        private bool _isValidPlacement;

        void Update()
        {
            // Raycast for placement
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                _targetPos = hit.point + hit.normal * 0.5f;
                _isValidPlacement = CanBuildAt(_targetPos);
                
                UpdateGhostBlock();
            }

            // Build on click
            if (Input.GetMouseButton(0) && Time.time - _lastBuildTime > BuildCooldown)
            {
                TryBuild();
                _lastBuildTime = Time.time;
            }

            // Remove on right click
            if (Input.GetMouseButton(1))
            {
                TryRemove();
            }
        }

        void UpdateGhostBlock()
        {
            if (GhostBlock != null)
            {
                GhostBlock.SetActive(true);
                GhostBlock.transform.position = _targetPos;
                
                var renderer = GhostBlock.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = _isValidPlacement ? GhostMaterialValid : GhostMaterialInvalid;
                }
            }
        }

        bool CanBuildAt(Vector3 pos)
        {
            // Check range to player
            if (Vector3.Distance(pos, transform.position) > BuildRange)
                return false;
            
            // Check NavMesh
            if (!UnityEngine.AI.NavMesh.SamplePosition(pos, out _, 1f, UnityEngine.AI.NavMesh.AllAreas))
                return false;
            
            return true;
        }

        void TryBuild()
        {
            if (!_isValidPlacement) return;
            
            Vector3Int voxelPos = Vector3Int.FloorToInt(_targetPos);
            
            // Get ChunkManager
            var chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager != null)
            {
                chunkManager.SetVoxel(voxelPos, CurrentMaterial);
                Debug.Log($"Built {CurrentMaterial} at {voxelPos}");
                
                // Record event
                WorldEventLogger.Instance?.LogEvent(WorldEventType.Cultural, 
                    $"Built {CurrentMaterial} structure", _targetPos);
            }
        }

        void TryRemove()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 30f))
            {
                Vector3Int voxelPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.5f);
                
                var chunkManager = FindObjectOfType<ChunkManager>();
                if (chunkManager != null)
                {
                    var current = chunkManager.GetVoxel(voxelPos);
                    if (current != VoxelType.Air)
                    {
                        chunkManager.SetVoxel(voxelPos, VoxelType.Air);
                        Debug.Log($"Removed {current} at {voxelPos}");
                    }
                }
            }
        }
    }

    public enum WorldEventType
    {
        Birth,
        Death,
        Cultural,
        Ecological,
        Climate
    }
}
