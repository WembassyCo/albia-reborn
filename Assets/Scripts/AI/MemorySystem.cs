using System.Collections.Generic;
using UnityEngine;

namespace Albia.AI
{
    /// <summary>
    /// Manages an entity's memory of locations, events, and other agents.
    /// </summary>
    public class MemorySystem
    {
        private class MemoryEvent
        {
            public Vector3 Location;
            public float Importance;
            public float DecayRate;
        }
        
        private Dictionary<Vector3, MemoryEvent> _memories;
        
        public MemorySystem()
        {
            _memories = new Dictionary<Vector3, MemoryEvent>();
        }
        
        /// <summary>
        /// Adds a memory of a location.
        /// </summary>
        public void RememberLocation(Vector3 location, float importance)
        {
            var mem = new MemoryEvent()
            {
                Location = location,
                Importance = importance,
                DecayRate = 0.05f // Configurable decay
            };
            
            _memories[location] = mem;
        }
        
        /// <summary>
        /// Gets the most important remembered location.
        /// </summary>
        public Vector3 GetMostImportantMemory()
        {
            Vector3 bestLoc = Vector3.zero;
            float bestImportance = -1f;
            
            foreach (var mem in _memories.Values)
            {
                if (mem.Importance > bestImportance)
                {
                    bestImportance = mem.Importance;
                    bestLoc = mem.Location;
                }
            }
            
            return bestLoc;
        }
        
        /// <summary>
        /// Updates memories, causing them to decay over time.
        /// </summary>
        public void UpdateMemories()
        {
            List<Vector3> keysToRemove = new List<Vector3>();
            
            foreach (var kvp in _memories)
            {
                kvp.Value.Importance -= kvp.Value.DecayRate * Time.deltaTime;
                if (kvp.Value.Importance <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _memories.Remove(key);
            }
        }
    }
}
