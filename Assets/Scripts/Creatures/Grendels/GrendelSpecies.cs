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
        [MinMaxRange(1f, 2f)]
        public Vector2 speedGeneRange = new Vector2(1.2f, 1.5f);
        
        [Tooltip("Lifespan gene range")]
        [MinMaxRange(30f, 120f)]
        public Vector2 lifespanGeneRange = new Vector2(60f, 100f);
        
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
            var genome = new GenomeData();
            
            // Initialize with random values
            var genes = new float[GenomeData.TotalGenes];
            for (int i = 0; i < GenomeData.TotalGenes; i++)
            {
                genes[i] = (float)(random.NextDouble() * 2.0 - 1.0); // -1 to 1
            }
            
            // Apply Grendel-specific trait genes (first 64 are physical/biochemical)
            
            // Gene 0-9: Physical/biochemical base
            // Index 9 is specifically aggression
            
            // Aggression: 0.7 - 1.0 range (high aggression)
            // Map -1..1 to 0.7..1.0
            genes[9] = aggressionBase * 2 - 1; // Convert 0.85 to ~0.7 in -1..1 space
            
            // Speed gene (affects movement speed)
            // Store as normalized multiplier
            float speedGene = UnityEngine.Random.Range(speedGeneRange.x, speedGeneRange.y);
            genes[10] = (speedGene - 1f) * 2; // Map 1.0..2.0 to -1..1
            
            // Lifespan gene
            float lifespanGene = UnityEngine.Random.Range(lifespanGeneRange.x, lifespanGeneRange.y);
            genes[11] = (lifespanGene - 60f) / 60f * 2 - 1; // Map to -1..1
            
            // Damage gene
            float damageGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[12] = damageGene * 2 - 1;
            
            // Metabolism efficiency (0.8 - 1.2 scale)
            float metabolismGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[13] = metabolismGene * 2 - 1;
            
            // Detection gene (affects detection radius)
            float detectionGene = UnityEngine.Random.Range(0.8f, 1.2f);
            genes[14] = detectionGene * 2 - 1;
            
            // Cannibalism trait (stored as separate boolean-ish gene)
            genes[15] = enableCannibalism ? 0.5f : -0.5f;
            
            // Species marker genes (224-255)
            // Use 224-227 to mark as Grendel species
            genes[224] = 0.9f; // Grendel marker 1
            genes[225] = -0.9f; // Grendel marker 2
            genes[226] = 0.5f; // Grendel marker 3
            genes[227] = -0.5f; // Grendel marker 4
            
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
            
            // Use reflection to set private fields
            var type = typeof(Grendel);
            
            // Set serialized fields
            SetPrivateField(grendel, "attackRange", attackRange, type);
            SetPrivateField(grendel, "attackDamage", attackDamage, type);
            SetPrivateField(grendel, "attackCooldown", attackCooldown, type);
            SetPrivateField(grendel, "nornDetectionRadius", detectionRadius, type);
            SetPrivateField(grendel, "speedMultiplier", baseSpeed * UnityEngine.Random.Range(speedGeneRange.x, speedGeneRange.y) / 1.4f, type);
            SetPrivateField(grendel, "maxLifespan", UnityEngine.Random.Range(lifespanGeneRange.x, lifespanGeneRange.y), type);
            SetPrivateField(grendel, "cannibalismThreshold", cannibalismThreshold, type);
            SetPrivateField(grendel, "aggressionLevel", aggressionBase, type);
            SetPrivateField(grendel, "enableCannibalism", enableCannibalism, type);
            
            // Apply initial stats
            var energyField = typeof(Organism).GetField("currentEnergy", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            var maxEnergyField = typeof(Organism).GetField("maxEnergy", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (maxEnergyField != null)
                maxEnergyField.SetValue(grendel, baseMaxEnergy);
            if (energyField != null)
                energyField.SetValue(grendel, baseMaxEnergy * 0.8f); // Start at 80% energy
        }
        
        private void SetPrivateField(object target, string fieldName, object value, System.Type type = null)
        {
            var t = type ?? target.GetType();
            var field = t.GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public);
            
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        
        private void CreateVisuals(GameObject grendelObj)
        {
            // Create a simple visual representation
            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(grendelObj.transform);
            body.transform.localPosition = new Vector3(0, 0.9f * sizeMultiplier, 0);
            body.transform.localScale = Vector3.one * sizeMultiplier;
            body.transform.localRotation = Quaternion.identity;
            
            // Apply body color
            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material = new Material(Shader.Find("Standard"));
                bodyRenderer.material.color = bodyColor;
            }
            
            // Destroy collider from visual (we have our own)
            Destroy(body.GetComponent<Collider>());
            
            // Eyes (2 small spheres)
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
                eyeRenderer.material.SetFloat("_Emission", 0.5f); // Slight glow
            }
            
            Destroy(eye.GetComponent<Collider>());
        }
    }
    
    /// <summary>    
    /// Attribute for Min/Max range in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float Min { get; }
        public float Max { get; }
        
        public MinMaxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}