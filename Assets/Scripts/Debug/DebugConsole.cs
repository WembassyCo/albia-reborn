using UnityEngine;
using System.Collections.Generic;

namespace Albia.Debug
{
    /// <summary>
    /// In-game debug console
    /// MVP: Simple command line
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
        [SerializeField] private int maxLines = 50;
        
        private bool visible = false;
        private string input = "";
        private List<string> logLines = new List<string>();
        private Vector2 scrollPosition;
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                visible = !visible;
        }
        
        void OnGUI()
        {
            if (!visible) return;
            
            float height = Screen.height * 0.4f;
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, height), GUI.skin.box);
            
            // Log area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var line in logLines)
                GUILayout.Label(line);
            GUILayout.EndScrollView();
            
            // Input area
            GUILayout.BeginHorizontal();
            GUILayout.Label(>", GUILayout.Width(20));
            input = GUILayout.TextField(input);
            GUILayout.EndHorizontal();
            
            // Execute on Enter
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                ExecuteCommand(input);
                input = "";
            }
            
            GUILayout.EndArea();
        }
        
        void ExecuteCommand(string cmd)
        {
            Log(> " + cmd);
            
            var parts = cmd.Split(' ');
            if (parts.Length == 0) return;
            
            switch (parts[0].ToLower())
            {
                case "spawn":
                    Log("Spawning Norn...");
                    // Call GameManager spawn
                    break;
                case "kill":
                    Log("Killing selected...");
                    break;
                case "timescale":
                    if (parts.Length > 1 && float.TryParse(parts[1], out float scale))
                    {
                        Time.timeScale = scale;
                        Log($"Time scale set to {scale}");
                    }
                    break;
                case "help":
                    Log("Commands: spawn, kill, timescale <value>, clear");
                    break;
                case "clear":
                    logLines.Clear();
                    break;
                default:
                    Log($"Unknown command: {parts[0]}");
                    break;
            }
        }
        
        public void Log(string msg)
        {
            logLines.Add($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
            if (logLines.Count > maxLines)
                logLines.RemoveAt(0);
        }
    }
}