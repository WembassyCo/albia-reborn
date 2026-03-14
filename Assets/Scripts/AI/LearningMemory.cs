using UnityEngine;
using System.Collections.Generic;

namespace Albia.AI
{
    /// <summary>
    /// Ring buffer of recent experiences for neural learning
    /// MVP: Simple experience store
    /// Full: Eligibility traces, prioritized replay
    /// </summary>
    public class LearningMemory
    {
        public struct Experience
        {
            public float[] State;
            public int Action;
            public float Reward;
            public float[] NextState;
            public bool Done;
        }
        
        private Experience[] buffer;
        private int capacity;
        private int count = 0;
        private int index = 0;
        
        public LearningMemory(int capacity)
        {
            this.capacity = capacity;
            buffer = new Experience[capacity];
        }
        
        public void Store(Experience exp)
        {
            buffer[index] = exp;
            index = (index + 1) % capacity;
            if (count < capacity) count++;
        }
        
        public Experience[] Sample(int batchSize)
        {
            int size = Mathf.Min(batchSize, count);
            Experience[] batch = new Experience[size];
            
            for (int i = 0; i < size; i++)
            {
                int idx = Random.Range(0, count);
                batch[i] = buffer[idx];
            }
            
            return batch;
        }
        
        public void Clear() => count = index = 0;
        public int Count => count;
        public bool IsFull => count >= capacity;
    }
}