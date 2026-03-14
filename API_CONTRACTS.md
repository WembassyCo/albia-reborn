# Albia Reborn - API Contracts

> **Status:** Draft v0.1 | **Last Updated:** 2026-03-13  
> **Scope:** C# Interface Definitions for Cross-System Integration

---

## Overview

This document defines the formal contracts between major systems in Albia Reborn. All inter-system communication must occur through these interfaces.

---

## 1. Neural Network Interfaces

### 1.1 INeuralInputProvider

**Purpose:** Bridge between creature sensory perception and brain input

**Location:** `AI/Neural/INeuralInputProvider.cs`

```csharp
using System;

namespace Albia.AI.Neural
{
    /// <summary>
    /// Provides normalized float inputs for a creature's neural network.
    /// Called once per simulation tick before brain processing.
    /// </summary>
    public interface INeuralInputProvider
    {
        /// <summary>
        /// Gets the current sensory inputs for neural processing.
        /// Array length must match the brain's input layer size.
        /// </summary>
        /// <param name="state">Current creature state including position, health, etc.</param>
        /// <returns>Array of normalized float values (typically 0.0 to 1.0)</returns>
        float[] GetInputs(CreatureState state);
        
        /// <summary>
        /// Returns the number of inputs this provider generates.
        /// Used to validate brain architecture compatibility.
        /// </summary>
        int InputCount { get; }
        
        /// <summary>
        /// Gets descriptive labels for each input index.
        /// Used for debugging and brain visualization tools.
        /// </summary>
        string[] GetInputLabels();
    }
    
    /// <summary>
    /// Standard implementation combining multiple sensory sources
    /// </summary>
    public class CreatureInputProvider : INeuralInputProvider
    {
        private readonly ITerrainQuery _terrainQuery;
        private readonly IOrganism _organism;
        
        public int InputCount => 24; // 8 visual + 4 smell + 6 internal + 6 hearing
        
        public CreatureInputProvider(ITerrainQuery terrain, IOrganism organism)
        {
            _terrainQuery = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _organism = organism ?? throw new ArgumentNullException(nameof(organism));
        }
        
        public float[] GetInputs(CreatureState state)
        {
            var inputs = new float[InputCount];
            int idx = 0;
            
            // Visual inputs (8 directions)
            inputs[idx++] = GetVisualInput(state.Position, state.Facing + 0, 10f);   // Ahead
            inputs[idx++] = GetVisualInput(state.Position, state.Facing + 45, 10f);  // Ahead-Right
            inputs[idx++] = GetVisualInput(state.Position, state.Facing + 90, 8f);   // Right
            inputs[idx++] = GetVisualInput(state.Position, state.Facing - 45, 10f);  // Ahead-Left
            inputs[idx++] = GetVisualInput(state.Position, state.Facing - 90, 8f);  // Left
            inputs[idx++] = GetVisualInput(state.Position, state.Facing + 180, 6f); // Behind
            inputs[idx++] = GetVisualInput(state.Position, state.Facing + 135, 8f); // Back-Right
            inputs[idx++] = GetVisualInput(state.Position, state.Facing - 135, 8f); // Back-Left
            
            // Smell inputs (4 directions)
            inputs[idx++] = GetSmellInput(state.Position, Vector3Int.up);
            inputs[idx++] = GetSmellInput(state.Position, Vector3Int.down);
            inputs[idx++] = GetSmellInput(state.Position, state.Forward);
            inputs[idx++] = GetSmellInput(state.Position, -state.Forward);
            
            // Internal state inputs
            inputs[idx++] = _organism.Chemicals.GetConcentration("Energy") / 100f;
            inputs[idx++] = _organism.Chemicals.GetConcentration("Pain") / 100f;
            inputs[idx++] = _organism.Chemicals.GetConcentration("Fear") / 100f;
            inputs[idx++] = _organism.Chemicals.GetConcentration("Comfort") / 100f;
            inputs[idx++] = _organism.Chemicals.GetConcentration("SexDrive") / 100f;
            inputs[idx++] = _organism.Chemicals.GetConcentration("Poison") / 100f;
            
            // Hearing inputs (6 directional bands)
            // ... hearing implementation
            
            return inputs;
        }
        
        private float GetVisualInput(Vector3Int pos, float angle, float distance)
        {
            // Raycast or voxel sampling in direction
            return 0f; // Placeholder
        }
        
        private float GetSmellInput(Vector3Int pos, Vector3Int direction)
        {
            // Chemical sampling
            return 0f; // Placeholder
        }
        
        public string[] GetInputLabels() => new[]
        {
            "VisCenter", "Vis45R", "Vis90R", "Vis45L", "Vis90L", "Vis180", "Vis135R", "Vis135L",
            "SmellUp", "SmellDown", "SmellFwd", "SmellBack",
            "IntEnergy", "IntPain", "IntFear", "IntComfort", "IntSexDrive", "IntPoison",
            "Hear1", "Hear2", "Hear3", "Hear4", "Hear5", "Hear6"
        };
    }
}
```

---

### 1.2 INeuralOutputConsumer

**Purpose:** Executes actions based on neural network outputs

**Location:** `AI/Neural/INeuralOutputConsumer.cs`

```csharp
using System;

namespace Albia.AI.Neural
{
    /// <summary>
    /// Receives action decisions from a creature's neural network.
    /// Maps discrete action indices to creature behaviors.
    /// </summary>
    public interface INeuralOutputConsumer
    {
        /// <summary>
        /// Number of possible actions (output layer size).
        /// </summary>
        int ActionCount { get; }
        
        /// <summary>
        /// Executes the selected action with the given strength.
        /// </summary>
        /// <param name="actionIndex">Index of the action to execute (0 to ActionCount-1)</param>
        /// <param name="strength">Action intensity, typically 0.0 to 1.0</param>
        void ExecuteAction(int actionIndex, float strength);
        
        /// <summary>
        /// Gets descriptive labels for each action index.
        /// </summary>
        string[] GetActionLabels();
        
        /// <summary>
        /// Called when multiple actions fire simultaneously.
        /// Implementations should handle action prioritization or combination.
        /// </summary>
        void ExecuteMultipleActions((int actionIndex, float strength)[] actions);
    }
    
    /// <summary>
    /// Standard creature action mapping
    /// </summary>
    public class CreatureActionConsumer : INeuralOutputConsumer
    {
        private readonly IOrganism _organism;
        private readonly IMover _mover;
        private readonly IItemInteractor _interactor;
        
        public int ActionCount => 16;
        
        public CreatureActionConsumer(IOrganism organism, IMover mover, IItemInteractor interactor)
        {
            _organism = organism ?? throw new ArgumentNullException(nameof(organism));
            _mover = mover ?? throw new ArgumentNullException(nameof(mover));
            _interactor = interactor ?? throw new ArgumentNullException(nameof(interactor));
        }
        
        public void ExecuteAction(int actionIndex, float strength)
        {
            switch (actionIndex)
            {
                case 0: _mover.MoveForward(strength); break;
                case 1: _mover.MoveBackward(strength * 0.5f); break;
                case 2: _mover.TurnLeft(strength); break;
                case 3: _mover.TurnRight(strength); break;
                case 4: _mover.Jump(strength); break;
                case 5: _mover.Rest(); break;
                case 6: _interactor.PickUp(strength); break;
                case 7: _interactor.Drop(strength); break;
                case 8: _interactor.Activate(strength); break; // Push, pull, use
                case 9: _interactor.Attack(strength); break;
                case 10: _organism.Reproduce(); break; // Mating call
                case 11: _organism.Chemicals.Emit("Pheromone", strength * 10f); break;
                case 12: _interactor.Eat(); break;
                case 13: _interactor.Drink(); break;
                case 14: _mover.FaceNearestToy(); break;
                case 15: _organism.Sleep(); break;
                default: throw new ArgumentOutOfRangeException(nameof(actionIndex));
            }
        }
        
        public void ExecuteMultipleActions((int actionIndex, float strength)[] actions)
        {
            // Sort by strength, execute top actions up to energy budget
            foreach (var (index, strength) in actions)
            {
                ExecuteAction(index, strength);
            }
        }
        
        public string[] GetActionLabels() => new[]
        {
            "MoveForward", "MoveBackward", "TurnLeft", "TurnRight",
            "Jump", "Rest", "PickUp", "Drop", "Activate", "Attack",
            "Reproduce", "EmitPheromone", "Eat", "Drink", "FaceToy", "Sleep"
        };
    }
}
```

---

## 2. Terrain System Interface

### 2.1 ITerrainQuery

**Purpose:** Read-only access to world voxel data

**Location:** `Terrain/ITerrainQuery.cs`

```csharp
using System;
using UnityEngine;

namespace Albia.Terrain
{
    /// <summary>
    /// Enumeration of voxel types for terrain classification.
    /// </summary>
    public enum VoxelType
    {
        Air = 0,
        Soil = 1,           // Standard dirt
        Rock = 2,           // Stone/rock
        Sand = 3,           // Desert/beach
        Water = 4,          // Liquid water
        NutrientSoil = 5,   // Rich soil for plants
        Wall = 6,           // Immovable boundary
        Wood = 7,           // Tree/wood
        Food = 8,           // Edible substances
        Metal = 9,          // Metallic substances
        Poison = 10,        // Toxic substances
        Goo = 11,           // Organic slime
        Ice = 12            // Frozen terrain
    }
    
    /// <summary>
    /// Source of terrain modification (for permissions/logging).
    /// </summary>
    public enum ChangeSource
    {
        Natural,
        Player,
        Creature,
        System,
        Script
    }
    
    /// <summary>
    /// Primary interface for terrain data access.
    /// Implemented by TerrainManager and used by CreatureSystem, AISystem, etc.
    /// </summary>
    public interface ITerrainQuery
    {
        /// <summary>
        /// Gets the voxel type at the specified grid position.
        /// </summary>
        /// <param name="pos">Grid coordinates</param>
        /// <returns>VoxelType at position, or VoxelType.Air if out of bounds</returns>
        VoxelType GetVoxel(Vector3Int pos);
        
        /// <summary>
        /// Gets the voxel type at world-space position.
        /// </summary>
        VoxelType GetVoxelAtWorld(Vector3 worldPos);
        
        /// <summary>
        /// Checks if the voxel at position is solid (impassable).
        /// </summary>
        bool IsSolid(Vector3Int pos);
        
        /// <summary>
        /// Gets the height of the terrain surface at given X,Z coordinates.
        /// </summary>
        int GetSurfaceHeight(int x, int z);
        
        /// <summary>
        /// Finds the nearest voxel of a specific type from a starting position.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="type">Voxel type to search for</param>
        /// <param name="maxRadius">Maximum search radius</param>
        /// <returns>Position of nearest matching voxel, or null if not found</returns>
        Vector3Int? FindNearest(Vector3Int start, VoxelType type, float maxRadius);
        
        /// <summary>
        /// Queries voxels within a spherical radius.
        /// </summary>
        (Vector3Int pos, VoxelType type)[] QuerySphere(Vector3Int center, float radius);
        
        /// <summary>
        /// Queries voxels within an axis-aligned bounding box.
        /// </summary>
        (Vector3Int pos, VoxelType type)[] QueryBox(BoundsInt bounds);
        
        /// <summary>
        /// Event fired whenever any voxel changes.
        /// Subscribers: CreatureSystem (sensory updates), AISystem (environment learning),
        ///             PlayerSystem (visual refresh), ChemicalSystem (diffusion).
        /// </summary>
        event Action<Vector3Int, VoxelType> OnVoxelChanged;
        
        /// <summary>
        /// Event fired when a significant terrain event occurs (cave formation, landslide, etc.)
        /// </summary>
        event Action<TerrainEvent> OnTerrainEvent;
    }
    
    /// <summary>
    /// Extended interface including modification capabilities.
    /// Only available to systems with terrain modification permissions.
    /// </summary>
    public interface ITerrainModify : ITerrainQuery
    {
        /// <summary>
        /// Sets the voxel at the specified position.
        /// </summary>
        /// <param name="pos">Target grid position</param>
        /// <param name="type">New voxel type</param>
        /// <param name="source">Source of the change (for permissions)</param>
        void SetVoxel(Vector3Int pos, VoxelType type, ChangeSource source);
        
        /// <summary>
        /// Attempts to set a voxel, returns success status.
        /// </summary>
        bool TrySetVoxel(Vector3Int pos, VoxelType type, ChangeSource source);
        
        /// <summary>
        /// Fills a region with a voxel type.
        /// </summary>
        void FillBox(BoundsInt bounds, VoxelType type, ChangeSource source);
        
        /// <summary>
        /// Generates terrain from a heightmap.
        /// </summary>
        void GenerateFromHeightmap(float[,] heights, VoxelType surfaceType, VoxelType subsurfaceType);
    }
    
    /// <summary>
    /// Data structure for terrain events.
    /// </summary>
    public readonly struct TerrainEvent
    {
        public readonly Vector3Int Location;
        public readonly TerrainEventType Type;
        public readonly float Magnitude;
        public readonly ChangeSource Source;
        
        public TerrainEvent(Vector3Int loc, TerrainEventType type, float mag, ChangeSource source)
        {
            Location = loc;
            Type = type;
            Magnitude = mag;
            Source = source;
        }
    }
    
    public enum TerrainEventType
    {
        Landslide,
        Erosion,
        CaveCollapse,
        WaterFlow,
        PlantGrowth,
        Explosion,
        ManualModification
    }
}
```

---

## 3. Organism Interface

### 3.1 IOrganism

**Purpose:** Core biological entity abstraction

**Location:** `Creatures/IOrganism.cs`

```csharp
using System;

namespace Albia.Creatures
{
    /// <summary>
    /// Unique identifier for organisms using Guid for global uniqueness.
    /// </summary>
    public readonly struct OrganismId
    {
        public readonly Guid Value;
        public OrganismId(Guid value) => Value = value;
        public static OrganismId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString("N")[..8];
    }
    
    /// <summary>
    /// Core interface representing any living organism in Albia.
    /// Implemented by Creature class and any future organism types.
    /// </summary>
    public interface IOrganism : IDisposable
    {
        /// <summary>
        /// Globally unique identifier for this organism.
        /// Persists across save/load cycles.
        /// </summary>
        OrganismId Id { get; }
        
        /// <summary>
        /// Genetic blueprint defining creature appearance and behavior predisposition.
        /// </summary>
        GenomeData Genome { get; }
        
        /// <summary>
        /// Current chemical state (hormones, nutrients, toxins, etc.)
        /// </summary>
        ChemicalState Chemicals { get; }
        
        /// <summary>
        /// Physical state for rendering and collision.
        /// </summary>
        TransformInfo Transform { get; }
        
        /// <summary>
        /// Current life stage of the organism.
        /// </summary>
        LifeStage Stage { get; }
        
        /// <summary>
        /// Age in simulation ticks.
        /// </summary>
        long Age { get; }
        
        /// <summary>
        /// Name for player reference (may be generated from genome).
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Event fired when this organism dies.
        /// Provides full organism reference for cleanup/handlers.
        /// </summary>
        event Action<IOrganism> OnDeath;
        
        /// <summary>
        /// Event fired when a significant chemical change occurs.
        /// </summary>
        event Action<string, float, float> OnChemicalChanged; // chemical name, old value, new value
        
        /// <summary>
        /// Event fired when this organism reproduces.
        /// </summary>
        event Action<IOrganism, IOrganism> OnReproduce; // parent, offspring
        
        /// <summary>
        /// Advances the organism one simulation tick.
        /// Called by SimulationCore, delegates to subsystems.
        /// </summary>
        void Tick(float deltaTime);
        
        /// <summary>
        /// Attempts to mate with another organism.
        /// </summary>
        bool AttemptReproduction(IOrganism partner);
        
        /// <summary>
        /// Applies damage from external sources.
        /// </summary>
        void TakeDamage(float amount, DamageType type, object source);
        
        /// <summary>
        /// Adds energy/nutrition from consumed resources.
        /// </summary>
        void Consume(IFoodSource food);
        
        /// <summary>
        /// Enters sleep state.
        /// </summary>
        void Sleep();
        
        /// <summary>
        /// Wakes from sleep.
        /// </summary>
        void Wake();
        
        /// <summary>
        /// Current health percentage (0.0 to 1.0).
        /// </summary>
        float Health { get; }
        
        /// <summary>
        /// Current energy level (affects action availability).
        /// </summary>
        float Energy { get; }
        
        /// <summary>
        /// True if organism is dead.
        /// </summary>
        bool IsDead { get; }
    }
    
    /// <summary>
    /// Life stages for organisms.
    /// </summary>
    public enum LifeStage
    {
        Embryo,     // Developing in egg/womb
        Baby,       // Newborn, high learning rate
        Child,      // Growing, developing
        Adolescent, // Sexual maturity begins
        Adult,      // Fully mature
        Elder,      // Aging, declining
        Corpse      // Dead, decaying
    }
    
    /// <summary>
    /// Types of damage sources.
    /// </summary>
    public enum DamageType
    {
        Physical,   // Impact, crushing
        Thermal,    // Heat, cold
        Chemical,   // Acids, toxins
        Biological, // Disease, parasites
        Hunger,     // Starvation
        Drowning,   // Suffocation
        Fall        // Impact from height
    }
    
    /// <summary>
    /// Structure holding organism transform data.
    /// </summary>
    public struct TransformInfo
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Vector3 Forward;
        public Vector3 Up;
    }
}
```

---

## 4. Supporting Data Structures

### 4.1 GenomeData

```csharp
namespace Albia.Creatures
{
    /// <summary>
    /// Complete genetic blueprint for an organism.
    /// Immutable once created, supports crossover and mutation.
    /// </summary>
    public readonly struct GenomeData
    {
        public readonly uint[] Genes;
        public readonly byte SpeciesId;
        public readonly byte VariantId;
        public readonly uint Generation;
        public readonly uint MutationCount;
        public readonly OrganismId ParentA;
        public readonly OrganismId ParentB;
        public readonly long CreationTick;
        
        public GenomeData(
            uint[] genes,
            byte speciesId,
            byte variantId,
            uint generation = 0,
            uint mutationCount = 0,
            OrganismId parentA = default,
            OrganismId parentB = default)
        {
            Genes = genes ?? throw new ArgumentNullException(nameof(genes));
            SpeciesId = speciesId;
            VariantId = variantId;
            Generation = generation;
            MutationCount = mutationCount;
            ParentA = parentA;
            ParentB = parentB;
            CreationTick = DateTime.UtcNow.Ticks;
        }
        
        /// <summary>
        /// Gets a specific gene value.
        /// </summary>
        public uint GetGene(int index) => Genes[index % Genes.Length];
        
        /// <summary>
        /// Creates a normalized float from gene range.
        /// </summary>
        public float GetGeneNormalized(int index) => GetGene(index) / (float)uint.MaxValue;
        
        /// <summary>
        /// Performs sexual reproduction gene crossover.
        /// </summary>
        public GenomeData Crossover(GenomeData partner, float mutationRate)
        {
            // Implementation in genetics subsystem
            throw new NotImplementedException();
        }
        
        public static GenomeData GenerateRandom(byte speciesId, int geneCount)
        {
            var random = new Random();
            var genes = new uint[geneCount];
            for (int i = 0; i < geneCount; i++)
                genes[i] = (uint)random.NextInt64();
            return new GenomeData(genes, speciesId, (byte)random.Next(256));
        }
    }
}
```

---

### 4.2 ChemicalState

```csharp
using System;
using System.Collections.Generic;

namespace Albia.Creatures
{
    /// <summary>
    /// Dynamic chemical composition of an organism.
    /// Modeled after the original Creatures' biochemistry.
    /// </summary>
    public class ChemicalState
    {
        private readonly Dictionary<string, float> _chemicals = new();
        
        /// <summary>
        /// Gets or sets a chemical concentration.
        /// </summary>
        public float this[string chemical]
        {
            get => _chemicals.TryGetValue(chemical, out float val) ? val : 0f;
            set => _chemicals[chemical] = Math.Clamp(value, 0f, 1000f);
        }
        
        /// <summary>
        /// Gets a chemical value (0 if not present).
        /// </summary>
        public float GetConcentration(string chemical) => this[chemical];
        
        /// <summary>
        /// Adds to a chemical concentration.
        /// </summary>
        public void Inject(string chemical, float amount) => this[chemical] += amount;
        
        /// <summary>
        /// Emits a chemical into the environment.
        /// </summary>
        public void Emit(string chemical, float amount, Vector3Int? location = null)
        {
            this[chemical] -= amount;
            OnEmitted?.Invoke(chemical, amount, location ?? Vector3Int.zero);
        }
        
        /// <summary>
        /// Absorbs a chemical from the environment.
        /// </summary>
        public void Absorb(string chemical, float amount) => this[chemical] += amount;
        
        /// <summary>
        /// Ticks chemical reactions and diffusion.
        /// </summary>
        public void TickReactions(float deltaTime)
        {
            // Process reaction rules
            // Called by SimulationCore
        }
        
        public event Action<string, float, Vector3Int> OnEmitted;
        public event Action<string, float, float> OnChanged; // chemical, old, new
        
        // Common chemical constants
        public const string Energy = "Energy";
        public const string Pain = "Pain";
        public const string Fear = "Fear";
        public const string Comfort = "Comfort";
        public const string SexDrive = "SexDrive";
        public const string Poison = "Poison";
        public const string Antibody = "Antibody";
        public const string Aging = "Aging";
        public const string Hunger = "Hunger";
        public const string Thirst = "Thirst";
        public const string Sleepiness = "Sleepiness";
    }
}
```

---

## 5. Event System Contracts

### 5.1 ISimulationEvent

```csharp
namespace Albia.Core
{
    /// <summary>
    /// Marker interface for all simulation events.
    /// </summary>
    public interface ISimulationEvent
    {
        long Tick { get; }
        double Timestamp { get; }
    }
    
    /// <summary>
    /// Central event bus for loose coupling between systems.
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T eventData) where T : ISimulationEvent;
        void Subscribe<T>(Action<T> handler) where T : ISimulationEvent;
        void Unsubscribe<T>(Action<T> handler) where T : ISimulationEvent;
    }
    
    // Standard events
    public readonly struct SimulationTickEvent : ISimulationEvent
    {
        public long Tick { get; init; }
        public double Timestamp { get; init; }
        public float DeltaTime { get; init; }
    }
    
    public readonly struct CreatureDeathEvent : ISimulationEvent
    {
        public long Tick { get; init; }
        public double Timestamp { get; init; }
        public OrganismId CreatureId { get; init; }
        public Vector3Int Position { get; init; }
        public GenomeData Genome { get; init; }
        public LifeStage StageAtDeath { get; init; }
        public DamageType? CauseOfDeath { get; init; }
    }
    
    public readonly struct ReproductionEvent : ISimulationEvent
    {
        public long Tick { get; init; }
        public double Timestamp { get; init; }
        public OrganismId ParentA { get; init; }
        public OrganismId ParentB { get; init; }
        public OrganismId Offspring { get; init; }
        public Vector3Int Location { get; init; }
        public bool CrossSpecies { get; init; }
    }
}
```

---

## 6. Interface Implementation Checklist

| Interface | Primary Implementer | Consumers | Status |
|-----------|---------------------|-----------|--------|
| `INeuralInputProvider` | `CreatureInputProvider` | `BrainProcessor` | ⬜ Not Started |
| `INeuralOutputConsumer` | `CreatureActionConsumer` | `BrainProcessor` | ⬜ Not Started |
| `ITerrainQuery` | `TerrainManager` | `CreatureInputProvider`, `Pathfinding` | 🟡 Partial |
| `ITerrainModify` | `TerrainManager` | `PlayerTools`, `CreatureDigSystem` | 🟡 Partial |
| `IOrganism` | `Creature` | `AISystem`, `PlayerUI`, `Statistics` | 🟡 Partial |

---

## 7. Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.1 | 2026-03-13 | Initial contract definitions |

---

*These contracts are binding for all Week 1+ development. Changes require Architecture Review.*
