using UnityEngine;
using UnityEngine.UI;
using Albia.Core;

namespace Albia.UI
{
    /// <summary>
    /// Time scale controls for player
    /// </summary>
    public class TimeControls : MonoBehaviour
    {
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private Text timeScaleText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite playIcon;
        
        private bool isPaused = false;

        void Start()
        {
            timeScaleSlider.minValue = 0f;
            timeScaleSlider.maxValue = 300f;
            timeScaleSlider.value = 60f; // Default: 1 real sec = 1 game min

            timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
            pauseButton.onClick.AddListener(TogglePause);
        }

        void OnTimeScaleChanged(float value)
        {
            if (!isPaused && TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTimeScale(value);
                timeScaleText.text = $"{value:F0}x";
            }
        }

        void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            // Note: TimeManager.Instance.SetTimeScale overrides this - 
            // may need shared pause state
        }
    }
}