using System;
using UnityEngine;
using Albia.Core;

namespace Albia.Creatures
{
    /// <summary>
    /// Defines Grendel species traits and genome configuration.
    /// Predator species with aggressive traits.
    /// </summary>    
    [CreateAssetMenu(fileName = "GrendelSpecies", menuName = "Albia/Species/Grendel")]
    public class GrendelSpecies : ScriptableObject
    {
        [Header("Base Stats")]
        [Tooltip("Base max energy")]
        [Range(50f, 150f)]
        public float baseMaxEnergy = 80f;
        
        [Tooltip("Base metabolism rate (energy drain per second)")]
        [Range(0.05f, 0.5f)]
        public float baseMetabolism = 0.2f;
        
        [Tooltip("Base movement speed multiplier")]
        [Range(1f, 2f)]
        public float baseSpeed = 1.4f;
        
        [Tooltip("Base attack damage")]
        [Range(5f, 30f)]
        public float baseDamage = 15f;
        
        [Tooltip("Maximum lifespan in seconds")]
        [Range(30f, 120f)]
        public float lifespan = 80f;

        [Header("Genetic Traits")]
        [Tooltip("Base aggression level (0-1)")]
        [Range(0.7f, 1f)]
        public float aggressionBase = 0.85f;
        
        [Tooltip("Speed gene multiplier range")]
        [Range(1.2f, 2f)]
        public float minSpeedGene = 1.2f;
        
        [Range(1.2f, 2f)]
        public float maxSpeedGene = 1.5f;
        
        [Tooltip("Lifespan gene range")]
        [Range(30f, 120f)]
        public float minLifespanGene = 60f;
        
        [Range(30f, 150f)]
        public float maxLifespanGene = 100f;
        
        [Tooltip("Cannibalism enabled")]
        public bool enableCannibalism = true;
        
        [Tooltip("Cannibalism threshold (energy % when they start eating own kind)")]
        [Range(0f, 0.5f)]
        public float cannibalismThreshold = 0.3f;

        [Header("Detection")]
        [Tooltip("How far Grendels can detect Norns")]
        [Range(10f, 50f)]
        public float detectionRadius = 25f;
        
        [Tooltip("How often they scan for new targets (seconds)")]
        [Range(1f, 5f)]
        public float scanInterval = 3f;

        [Header("Attack")]
        [Tooltip("Attack range")]
        [Range(0.5f, 3f)]
        public float attackRange = 1.5f;
        
        [Tooltip("Time between attacks")]
        [Range(0.5f, 3f)]
        public float attackCooldown = 1f;
        
        [Tooltip("Damage dealt per attack")]
        [Range(5f, 25f)]
        public float attackDamage = 15f;

        [Header("Appearance")]
        [Tooltip("Base color for Grendels")]
        public Color bodyColor = new Color(0.2f, 0.6f, 0.2f); // Green
        
        [Tooltip("Eye color")]
        public Color eyeColor = new Color(1f, 0.2f, 0.2f); // Red eyes
        
        [Tooltip("Scale/size multiplier")]
        [Range(0.8f, 1.5f)]
        public float sizeMultiplier = 1.1f;

        /// <summary>
        /// Generate a new Grendel genome with predator traits
        /// </summary>
        public GenomeData GenerateGenome(int seed = 0)
        {
            var random = new System.Random(seed != 0 ? seed : Environment.TickCount);
            var genes = new float[192]; // Using standard genome size
            
            // Initialize with random values
            for (int i = 0; i < genes.Length; i++)
            {
                genes[i] = (float)(random.NextDouble() * 2.0 - 1.0); // -1 to 1
            }
            
            // Apply Grendel-specific trait genes
            // Aggression: 0.7 - 1.0 range (high aggression)
            genes[9] = aggressionBase * 2 - 1;
            
            // Speed gene
            float speedGene = UnityEngine.Random.Range(minSpeedGene, maxSpeedGene);
            genes[10] = (speedGene - 1f) * 2;
            
            // Lifespan gene
            float lifespanGene = UnityEngine.Random.Range(minLifespanGene, maxLifespanGene);
            genes[11] = (lifespanGene - 60f) / 60f * 2 - 1;
            
            // Damage gene
            float damageGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[12] = damageGene * 2 - 1;
            
            // Metabolism efficiency
            float metabolismGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[13] = metabolismGene * 2 - 1;
            
            // Detection gene
            float detectionGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[14] = detectionGene * 2 - 1;
            
            // Cannibalism trait
            genes[15] = enableCannibalism ? 0.5f : -0.5f;
            
            // Species markers
            genes[100] = 0.9f;
            genes[101] = -0.9f;
            genes[102] = 0.5f;
            genes[103] = -0.5f;
            
            return new GenomeData(genes);
        }
        
        /// <summary>
        /// Create a Grendel from this species definition
        /// </summary>
        public Grendel CreateGrendel(Vector3 position, Quaternion rotation)
        {
            // Create game object
            var grendelObj = new GameObject($"Grendel_{Guid.NewGuid().ToString().Substring(0, 8)}");
            grendelObj.transform.position = position;
            grendelObj.transform.rotation = rotation;
            grendelObj.layer = LayerMask.NameToLayer("Creatures");
            
            // Add components
            var grendel = grendelObj.AddComponent<Grendel>();
            
            // Apply settings from this species
            ConfigureGrendel(grendel);
            
            // Add visual representation
            CreateVisuals(grendelObj);
            
            // Add collider for detection and collision
            var collider = grendelObj.AddComponent<CapsuleCollider>();
            collider.height = 1.8f * sizeMultiplier;
            collider.radius = 0.4f * sizeMultiplier;
            collider.center = new Vector3(0, 0.9f * sizeMultiplier, 0);
            
            // Add Rigidbody for physics
            var rb = grendelObj.AddComponent<Rigidbody>();
            rb.mass = 5f * sizeMultiplier;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            return grendel;
        }
        
        /// <summary>
        /// Configure an existing Grendel with these settings
        /// </summary>
        public void ConfigureGrendel(Grendel grendel)
        {
            if (grendel == null) return;
            
            var type = typeof(Grendel);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | 
                              System.Reflection.BindingFlags.Public | 
                              System.Reflection.BindingFlags.Instance;
            
            // Set serialized fields via reflection
            SetFieldValue(grendel, "attackRange", attackRange, type, bindingFlags);
            SetFieldValue(grendel, "attackDamage", attackDamage, type, bindingFlags);
            SetFieldValue(grendel, "attackCooldown", attackCooldown, type, bindingFlags);
            SetFieldValue(grendel, "nornDetectionRadius", detectionRadius, type, bindingFlags);
            SetFieldValue(grendel, "speedMultiplier", baseSpeed * UnityEngine.Random.Range(minSpeedGene, maxSpeedGene) / 1.4f, type, bindingFlags);
            SetFieldValue(grendel, "maxLifespan", UnityEngine.Random.Range(minLifespanGene, maxLifespanGene), type, bindingFlags);
            SetFieldValue(grendel, "cannibalismThreshold", cannibalismThreshold, type, bindingFlags);
            SetFieldValue(grendel, "aggressionLevel", aggressionBase, type, bindingFlags);
            SetFieldValue(grendel, "enableCannibalism", enableCannibalism, type, bindingFlags);
            
            // Initialize energy
            InitializeGrendelEnergy(grendel);
        }
        
        private void InitializeGrendelEnergy(Grendel grendel)
        {
            var organismType = typeof(Organism);
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | 
                              System.Reflection.BindingFlags.Instance;
            
            var maxEnergyField = organismType.GetField("maxEnergy", bindingFlags);
            var currentEnergyField = organismType.GetField("currentEnergy", bindingFlags);
            
            if (maxEnergyField != null)
                maxEnergyField.SetValue(grendel, baseMaxEnergy);
            if (currentEnergyField != null)
                currentEnergyField.SetValue(grendel, baseMaxEnergy * 0.8f);
        }
        
        private void SetFieldValue(object target, string fieldName, object value, System.Type type, System.Reflection.BindingFlags flags)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        
        private void CreateVisuals(GameObject grendelObj)
        {
            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(grendelObj.transform);
            body.transform.localPosition = new Vector3(0, 0.9f * sizeMultiplier, 0);
            body.transform.localScale = Vector3.one * sizeMultiplier;
            body.transform.localRotation = Quaternion.identity;
            
            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material = new Material(Shader.Find("Standard"));
                bodyRenderer.material.color = bodyColor;
            }
            
            Destroy(body.GetComponent<Collider>());
            
            // Eyes
            CreateEye(grendelObj, new Vector3(-0.15f, 1.5f, 0.35f) * sizeMultiplier);
            CreateEye(grendelObj, new Vector3(0.15f, 1.5f, 0.35f) * sizeMultiplier);
        }
        
        private void CreateEye(GameObject parent, Vector3 localPos)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent.transform);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * 0.1f * sizeMultiplier;
            
            var eyeRenderer = eye.GetComponent<Renderer>();
            if (eyeRenderer != null)
            {
                eyeRenderer.material = new Material(Shader.Find("Standard"));
                eyeRenderer.material.color = eyeColor;
            }
            
            Destroy(eye.GetComponent<Collider>());
        }
    }
}