using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Albia.Creatures;

namespace Albia.UI
{
    /// <summary>
    /// Central UI manager for the Albia world.
    /// Handles pause/play controls, time scale slider, and coordinates other UI panels.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Events
        public event Action OnPause;
        public event Action OnPlay;
        public event Action<float> OnTimeScaleChanged;
        public event Action<Norn> OnCreatureSelected;
        
        [Header("Pause Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button stepButton;
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite playIcon;
        [SerializeField] private Image pausePlayImage;
        
        [Header("Time Scale Slider")]
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private TextMeshProUGUI timeScaleText;
        [SerializeField] private float minTimeScale = 0f;
        [SerializeField] private float maxTimeScale = 5f;
        [SerializeField] private float[] presetTimeScales = new[] { 0f, 0.5f, 1f, 2f, 5f };
        
        [Header("UI Panels")]
        [SerializeField] private CreatureInspector creatureInspector;
        [SerializeField] private StatsPanel statsPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        
        [Header("Selection")]
        [SerializeField] private LayerMask creatureLayer;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private GameObject selectionIndicator;
        
        [Header("Hotkeys")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Space;
        [SerializeField] private KeyCode stepKey = KeyCode.Period;
        [SerializeField] private KeyCode[] timeScaleHotkeys = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
        
        // Runtime state
        private bool isPaused = false;
        private float currentTimeScale = 1f;
        private float previousTimeScale = 1f;
        private Norn selectedCreature;
        private bool stepRequested = false;
        
        // Singleton instance
        public static UIManager Instance { get; private set; }
        
        // Properties
        public bool IsPaused => isPaused;
        public float CurrentTimeScale => currentTimeScale;
        public CreatureInspector Inspector => creatureInspector;
        public StatsPanel Stats => statsPanel;
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Find main camera if not set
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            // Find panels if not set
            FindPanels();
            
            // Initialize UI
            InitializeUI();
        }
        
        private void Start()
        {
            ApplyTimeScale(1f);
        }
        
        private void Update()
        {
            HandleInput();
            UpdateSelectionIndicator();
        }
        
        private void HandleInput()
        {
            // Pause/Play toggle
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
            
            // Step (only when paused)
            if (Input.GetKeyDown(stepKey) && isPaused)
            {
                RequestStep();
            }
            
            // Time scale hotkeys
            for (int i = 0; i < timeScaleHotkeys.Length && i < presetTimeScales.Length; i++)
            {
                if (Input.GetKeyDown(timeScaleHotkeys[i]))
                {
                    SetTimeScale(presetTimeScales[i]);
                    break;
                }
            }
            
            // Mouse click to select creature
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectCreature();
            }
            
            // Deselect on escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (selectedCreature != null)
                {
                    DeselectCreature();
                }
                else if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
        
        /// <summary>
        /// Initializes all UI controls
        /// </summary>
        private void InitializeUI()
        {
            // Setup pause controls
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(Pause);
            }
            
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(Resume);
            }
            
            if (stepButton != null)
            {
                stepButton.onClick.RemoveAllListeners();
                stepButton.onClick.AddListener(RequestStep);
            }
            
            // Setup time scale slider
            if (timeScaleSlider != null)
            {
                timeScaleSlider.minValue = minTimeScale;
                timeScaleSlider.maxValue = maxTimeScale;
                timeScaleSlider.value = currentTimeScale;
                timeScaleSlider.onValueChanged.RemoveAllListeners();
                timeScaleSlider.onValueChanged.AddListener(OnSliderChanged);
            }
            
            // Hide panels initially
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            if (selectionIndicator != null)
                selectionIndicator.SetActive(false);
            
            UpdatePausePlayButtons();
            UpdateTimeScaleDisplay();
        }
        
        /// <summary>
        /// Auto-finds UI panels if not assigned
        /// </summary>
        private void FindPanels()
        {
            if (creatureInspector == null)
                creatureInspector = FindObjectOfType<CreatureInspector>();
            
            if (statsPanel == null)
                statsPanel = FindObjectOfType<StatsPanel>();
        }
        
        #region Pause/Play Control
        
        /// <summary>
        /// Pauses the simulation
        /// </summary>
        public void Pause()
        {
            if (isPaused) return;
            
            isPaused = true;
            previousTimeScale = currentTimeScale;
            
            // Set Unity time scale
            Time.timeScale = stepRequested ? currentTimeScale : 0f;
            
            // Show pause menu
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
            
            OnPause?.Invoke();
            UpdatePausePlayButtons();
        }
        
        /// <summary>
        /// Resumes the simulation
        /// </summary>
        public void Resume()
        {
            if (!isPaused) return;
            
            isPaused = false;
            
            // Restore time scale
            Time.timeScale = currentTimeScale;
            
            // Hide pause menu
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            
            OnPlay?.Invoke();
            UpdatePausePlayButtons();
        }
        
        /// <summary>
        /// Toggles between pause and play
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
        
        /// <summary>
        /// Requests a single step when paused (advances one frame)
        /// </summary>
        public void RequestStep()
        {
            if (!isPaused) return;
            
            stepRequested = true;
            Time.timeScale = currentTimeScale;
            
            // This will be reset next frame via FixedUpdate or coroutine
            StartCoroutine(ResetStepFlag());
        }
        
        private System.Collections.IEnumerator ResetStepFlag()
        {
            yield return new WaitForFixedUpdate();
            stepRequested = false;
            Time.timeScale = 0f;
        }
        
        private void UpdatePausePlayButtons()
        {
            if (pausePlayImage != null)
            {
                pausePlayImage.sprite = isPaused ? playIcon : pauseIcon;
            }
            
            // Enable/disable buttons
            if (pauseButton != null)
                pauseButton.interactable = !isPaused;
            
            if (playButton != null)
                playButton.interactable = isPaused;
            
            if (stepButton != null)
                stepButton.interactable = isPaused;
        }
        
        #endregion
        
        #region Time Scale
        
        /// <summary>
        /// Sets the time scale (speed of simulation)
        /// </summary>
        /// <param name="scale">Time multiplier (0 = paused, 1 = normal, 2 = double speed, etc.)</param>
        public void SetTimeScale(float scale)
        {
            ApplyTimeScale(Mathf.Clamp(scale, minTimeScale, maxTimeScale));
            
            // Update slider if it exists
            if (timeScaleSlider != null)
                timeScaleSlider.value = currentTimeScale;
        }
        
        /// <summary>
        /// Gets a preset time scale
        /// </summary>
        public void SetPresetTimeScale(int presetIndex)
        {
            if (presetIndex >= 0 && presetIndex < presetTimeScales.Length)
                SetTimeScale(presetTimeScales[presetIndex]);
        }
        
        private void ApplyTimeScale(float scale)
        {
            currentTimeScale = scale;
            
            if (!isPaused || stepRequested)
            {
                Time.timeScale = currentTimeScale;
            }
            
            OnTimeScaleChanged?.Invoke(currentTimeScale);
            UpdateTimeScaleDisplay();
        }
        
        private void OnSliderChanged(float value)
        {
            ApplyTimeScale(value);
        }
        
        private void UpdateTimeScaleDisplay()
        {
            if (timeScaleText != null)
            {
                if (currentTimeScale == 0f)
                    timeScaleText.text = "Paused";
                else
                    timeScaleText.text = $"{currentTimeScale:F1}x";
            }
        }
        
        #endregion
        
        #region Creature Selection
        
        /// <summary>
        /// Attempts to select a creature at the mouse position
        /// </summary>
        private void TrySelectCreature()
        {
            if (mainCamera == null) return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Raycast for creatures
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, creatureLayer))
            {
                Norn creature = hit.collider.GetComponent<Norn>();
                if (creature != null)
                {
                    SelectCreature(creature);
                    return;
                }
            }
            
            // Simple sphere check for creatures without colliders
            Collider[] hits = Physics.OverlapSphere(ray.origin, 2f, creatureLayer);
            foreach (Collider col in hits)
            {
                Norn creature = col.GetComponent<Norn>();
                if (creature != null)
                {
                    SelectCreature(creature);
                    return;
                }
            }
            
            // Deselect if clicked empty space
            DeselectCreature();
        }
        
        /// <summary>
        /// Selects a creature for inspection
        /// </summary>
        public void SelectCreature(Norn creature)
        {
            if (creature == null) return;
            
            selectedCreature = creature;
            
            // Update inspector
            if (creatureInspector != null)
                creatureInspector.Inspect(creature);
            
            // Update stats panel
            if (statsPanel != null)
                statsPanel.SetSelectedCreature(creature);
            
            // Show selection indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
                selectionIndicator.transform.SetParent(creature.transform, false);
                selectionIndicator.transform.localPosition = Vector3.up * 2f;
            }
            
            OnCreatureSelected?.Invoke(creature);
        }
        
        /// <summary>
        /// Deselects the current creature
        /// </summary>
        public void DeselectCreature()
        {
            selectedCreature = null;
            
            if (creatureInspector != null)
                creatureInspector.ClearInspection();
            
            if (statsPanel != null)
                statsPanel.ClearSelection();
            
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
                selectionIndicator.transform.SetParent(transform, false);
            }
        }
        
        private void UpdateSelectionIndicator()
        {
            if (selectionIndicator == null || selectedCreature == null) return;
            
            // Hide if creature died
            if (!selectedCreature.IsAlive)
            {
                selectionIndicator.SetActive(false);
            }
        }
        
        /// <summary>
        /// Gets the currently selected creature
        /// </summary>
        public Norn GetSelectedCreature() => selectedCreature;
        
        #endregion
        
        #region Panel Management
        
        /// <summary>
        /// Shows the creature inspector for the given creature
        /// </summary>
        public void ShowInspector(Norn creature)
        {
            if (creatureInspector != null)
                creatureInspector.Inspect(creature);
        }
        
        /// <summary>
        /// Hides the creature inspector
        /// </summary>
        public void HideInspector()
        {
            if (creatureInspector != null)
                creatureInspector.ClearInspection();
        }
        
        /// <summary>
        /// Toggles the HUD visibility
        /// </summary>
        public void ToggleHUD(bool visible)
        {
            if (hudPanel != null)
                hudPanel.SetActive(visible);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            
            // Ensure time scale is reset
            Time.timeScale = 1f;
        }
    }
}