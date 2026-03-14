using System;
using System.Collections.Generic;
using UnityEngine;

namespace Albia.Core
{
    /// <summary>
    /// Tracks and unlocks achievements based on in-game events.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Serializable]
        public class Achievement
        {
            public string id;
            public string title;
            public string description;
            public bool isUnlocked;
            public Sprite icon;
        }

        public List<Achievement> achievements;
        private Dictionary<string, Achievement> _achievementLookup;

        public event Action<Achievement> OnAchievementUnlocked;

        void Awake()
        {
            Instance = this;
            _achievementLookup = new Dictionary<string, Achievement>();
            foreach (var ach in achievements)
            {
                _achievementLookup[ach.id] = ach;
            }
        }

        /// <summary>
        /// Unlocks an achievement by its ID.
        /// </summary>
        public void UnlockAchievement(string id)
        {
            if (_achievementLookup.TryGetValue(id, out var ach) && !ach.isUnlocked)
            {
                ach.isUnlocked = true;
                OnAchievementUnlocked?.Invoke(ach);
                Debug.Log($"Achievement Unlocked: {ach.title}");
            }
        }

        /// <summary>
        /// Checks if an achievement is unlocked.
        /// </summary>
        public bool IsUnlocked(string id)
        {
            return _achievementLookup.TryGetValue(id, out var ach) && ach.isUnlocked;
        }
    }
}