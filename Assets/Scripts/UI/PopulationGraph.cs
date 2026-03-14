using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Albia.Lifecycle;

namespace Albia.UI
{
    /// <summary>
    /// Real-time population graph
    /// MVP: Simple line graph
    /// Full: Multiple series, zoom, history
    /// </summary>
    public class PopulationGraph : MonoBehaviour
    {
        [SerializeField] private Image graphImage;
        [SerializeField] private int maxDataPoints = 100;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private Color lineColor = Color.cyan;
        [SerializeField] private float lineWidth = 2f;
        
        private List<int> populationHistory = new List<int>();
        private float updateTimer;
        private Texture2D graphTexture;
        
        void Start()
        {
            CreateTexture();
            PopulationRegistry.Instance?.OnPopulationChanged.AddListener(OnPopulationChanged);
        }
        
        void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0;
                UpdateGraph();
            }
        }
        
        void CreateTexture()
        {
            graphTexture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
            graphImage.sprite = Sprite.Create(graphTexture, new Rect(0, 0, 256, 128), Vector2.zero);
        }
        
        void OnPopulationChanged()
        {
            int count = PopulationRegistry.Instance?.GetPopulationCount() ?? 0;
            populationHistory.Add(count);
            
            if (populationHistory.Count > maxDataPoints)
                populationHistory.RemoveAt(0);
        }
        
        void UpdateGraph()
        {
            if (populationHistory.Count < 2) return;
            
            // Clear
            Color[] pixels = new Color[256 * 128];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
            
            // Draw line
            float maxPop = Mathf.Max(populationHistory.ToArray());
            maxPop = Mathf.Max(maxPop, 1);
            
            for (int i = 0; i < populationHistory.Count - 1; i++)
            {
                float x1 = (i / (float)maxDataPoints) * 256;
                float x2 = ((i + 1) / (float)maxDataPoints) * 256;
                float y1 = (populationHistory[i] / maxPop) * 128;
                float y2 = (populationHistory[i + 1] / maxPop) * 128;
                
                DrawLine(pixels, (int)x1, (int)y1, (int)x2, (int)y2);
            }
            
            graphTexture.SetPixels(pixels);
            graphTexture.Apply();
        }
        
        void DrawLine(Color[] pixels, int x1, int y1, int x2, int y2)
        {
            // Simple Bresenham
            int dx = Mathf.Abs(x2 - x1);
            int dy = Mathf.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                if (x1 >= 0 && x1 < 256 && y1 >= 0 && y1 < 128)
                    pixels[y1 * 256 + x1] = lineColor;
                
                if (x1 == x2 && y1 == y2) break;
                
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x1 += sx; }
                if (e2 < dx) { err += dx; y1 += sy; }
            }
        }
    }
}