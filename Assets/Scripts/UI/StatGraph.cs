using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Albia.UI
{
    /// <summary>
    /// Displays a historical graph of creature statistics.
    /// </summary>
    public class StatGraph : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private List<float> dataPoints;
        
        [Header("Display")]
        [SerializeField] private float maxDataPoints = 100;

        // Example usage: tracks a creature's energy or happiness
        public void AddDataPoint(float value)
        {
            if (dataPoints.Count > maxDataPoints)
            {
                dataPoints.RemoveAt(0);
            }
            dataPoints.Add(value);
            UpdateGraphDisplay();
        }
        
        private void UpdateGraphDisplay()
        {
            // Simple line drawing on UI using UI Line Renderer or similar
            // This is a placeholder for the graphing logic
            if (TryGetComponent<Image>(out var image))
            {
                image.fillAmount = dataPoints.Count > 0 ? dataPoints[dataPoints.Count - 1] / 100f : 0f; 
            }
        }
    }
}
