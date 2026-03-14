using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Albia.SaveSystem
{
    /// <summary>
    /// Logs game events to file for debugging/analysis
    /// MVP: Simple text log
    /// Full: Structured JSON, analytics
    /// </summary>
    public class GameLogger : MonoBehaviour
    {
        public static GameLogger Instance { get; private set; }
        
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private int maxLogLines = 1000;
        
        private List<string> logLines = new List<string>();
        private string logFilePath;
        
        void Awake()
        {
            Instance = this;
            logFilePath = Path.Combine(Application.persistentDataPath, "game_log.txt");
        }
        
        public void LogEvent(string category, string message)
        {
            if (!enableLogging) return;
            
            string line = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {message}";
            logLines.Add(line);
            
            if (logLines.Count > maxLogLines)
                logLines.RemoveAt(0);
            
            // Also write to file
            File.AppendAllText(logFilePath, line + "\n");
        }
        
        public void LogCreatureBirth(string name, string parents)
        {
            LogEvent("BIRTH", $"{name} born. Parents: {parents}");
        }
        
        public void LogCreatureDeath(string name, string cause)
        {
            LogEvent("DEATH", $"{name} died. Cause: {cause}");
        }
        
        public void LogReproduction(string parent1, string parent2, string offspring)
        {
            LogEvent("REPRODUCTION", $"{parent1} + {parent2} = {offspring}");
        }
        
        public string[] GetRecentLogs(int count = 100)
        {
            int start = Mathf.Max(0, logLines.Count - count);
            return logLines.GetRange(start, logLines.Count - start).ToArray();
        }
    }
}