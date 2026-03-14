using UnityEngine;
using UnityEngine.UI;

namespace Albia.Core
{
    /// <summary>
    /// Monitors and displays performance metrics
    /// MVP: Simple FPS counter
    /// Full: Comprehensive profiling
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [SerializeField] private Text fpsText;
        [SerializeField] private Text entityText;
        
        [Header("Settings")]
        [SerializeField] private float updateInterval = 1f;
        
        private float timer = 0f;
        private int frameCount = 0;
        private float fps = 0f;
        
        void Update()
        {
            frameCount++;
            timer += Time.unscaledDeltaTime;
            
            if (timer >= updateInterval)
            {
                fps = frameCount / timer;
                frameCount = 0;
                timer = 0f;
                
                UpdateDisplay();
            }
        }
        
        void UpdateDisplay()
        {
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            }
            
            if (entityText != null)
            {
                int norns = GameObject.FindGameObjectsWithTag("Creature").Length;
                int plants = GameObject.FindGameObjectsWithTag("Plant").Length;
                entityText.text = $"N:{norns} P:{plants}";
            }
        }
    }
}