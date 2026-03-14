using System;
using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Guides new players through basic gameplay
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        [Serializable]
        public class TutorialStep
        {
            public string title;
            public string description;
            public bool isCompleted;
            public string completionCondition; // e.g., "select_norn", "feed_creature"
        }
        
        public TutorialStep[] steps;
        private int currentStepIndex = 0;
        
        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;
        public event Action OnTutorialFinished;
        
        void Start()
        {
            if (steps.Length > 0)
            {
                StartStep(0);
            }
        }
        
        void StartStep(int index)
        {
            if (index >= steps.Length)
            {
                OnTutorialFinished?.Invoke();
                return;
            }
            
            currentStepIndex = index;
            steps[index].isCompleted = false;
            OnStepStarted?.Invoke(steps[index]);
        }
        
        public void CompleteStep(string condition)
        {
            if (currentStepIndex < steps.Length && steps[currentStepIndex].completionCondition == condition)
            {
                steps[currentStepIndex].isCompleted = true;
                OnStepCompleted?.Invoke(steps[currentStepIndex]);
                StartStep(currentStepIndex + 1);
            }
        }
    }
}