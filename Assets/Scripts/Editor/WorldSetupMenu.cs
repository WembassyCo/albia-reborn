using UnityEditor;
using UnityEngine;
using Albia.Creatures;
using Albia.Creatures.Genetics;

namespace Albia.Editor
{
    /// <summary>
    /// Quick world setup menu for testing.
    /// </summary>
    public class WorldSetupMenu : MonoBehaviour
    {
        [MenuItem("Albia/Setup Test World")]
        static void SetupTestWorld()
        {
            // Create bootstrap
            if (FindObjectOfType<GameBootstrap>() == null)
            {
                GameObject bootstrapObj = new GameObject("GameBootstrap");
                bootstrapObj.AddComponent<GameBootstrap>();
                bootstrapObj.AddComponent<GameManager>();
            }

            // Create TimeManager
            if (FindObjectOfType<TimeManager>() == null)
            {
                GameObject timeObj = new GameObject("TimeManager");
                timeObj.AddComponent<TimeManager>();
            }

            // Create ground plane for raycasting
            if (GameObject.Find("GroundPlane") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "GroundPlane";
                ground.transform.localScale = new Vector3(50, 1, 50);
                ground.GetComponent<Renderer>().enabled = false; // Invisible
            }

            // Setup lighting
            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightObj = new GameObject("DirectionalLight");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Create main camera
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("MainCamera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(64, 30, 64);
                cam.transform.LookAt(Vector3.zero);
            }

            // Create materials (basic)
            string[] materialNames = { "Dirt", "Stone", "Grass", "Sand" };
            foreach (var name in materialNames)
            {
                Material mat = Resources.Load<Material>(name);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Standard"));
                    mat.name = name;
                }
            }

            // Setup Player
            if (FindObjectOfType<PlayerController>() == null)
            {
                GameObject playerObj = new GameObject("Player");
                playerObj.AddComponent<PlayerController>();
                playerObj.transform.position = new Vector3(64, 5, 64);
            }

            Debug.Log("Test world setup complete!");
        }

        [MenuItem("Albia/Create Species Templates")]
        static void CreateSpeciesTemplates()
        {
            string path = "Assets/ScriptableObjects";
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            // Create Norn template
            var nornTemplate = ScriptableObject.CreateInstance<SpeciesTemplate>();
            nornTemplate.SpeciesName = "Norn";
            nornTemplate.NeuralInputCount = 30;
            nornTemplate.NeuralHiddenCount = 15;
            nornTemplate.NeuralOutputCount = 15;
            nornTemplate.BaseMetabolism = 1.0f;
            nornTemplate.LifespanRange = new Vector2(600f, 1200f);

            AssetDatabase.CreateAsset(nornTemplate, $"{path}/Norn.asset");
            AssetDatabase.SaveAssets();

            Debug.Log("Created Norn species template");
        }
    }
}

/// <summary>
/// Basic player controller for testing.
/// WASD + mouse look + click to inspect creatures.
/// </summary>
#if UNITY_EDITOR
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float MoveSpeed = 10f;
    public float LookSensitivity = 2f;
    public float Gravity = -9.81f;
    
    private CharacterController _controller;
    private float _verticalRotation = 0f;
    private Vector3 _velocity;
    
    private CreatureInspector _inspector;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        
        // Setup inspector UI
        SetupInspectorUI();
    }

    void SetupInspectorUI()
    {
        GameObject canvasObj = new GameObject("InspectionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create inspector panel
        GameObject panel = new GameObject("InspectorPanel");
        panel.transform.parent = canvasObj.transform;
        
        _inspector = panel.AddComponent<CreatureInspector>();
        // TODO: Setup UI elements
    }

    void Update()
    {
        // Movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move * MoveSpeed * Time.deltaTime);
        
        // Gravity
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -0.5f;
        }
        _velocity.y += Gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);

        // Look
        float mouseX = Input.GetAxis("Mouse X") * LookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * LookSensitivity;
        
        transform.Rotate(Vector3.up * mouseX);
        
        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);

        // Inspect on click
        if (Input.GetMouseButtonDown(0))
        {
            TryInspect();
        }
        
        // Unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked 
                ? CursorLockMode.None 
                : CursorLockMode.Locked;
        }
    }

    void TryInspect()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            Organism organism = hit.collider.GetComponent<Organism>();
            if (organism != null)
            {
                _inspector?.Inspect(organism);
            }
        }
    }
}
#endif
