# Creature Lifecycle System

This folder contains the reproduction and population management systems for Albia Reborn.

## Files

### ReproductionSystem.cs
Central system managing creature reproduction:
- **CanReproduce()** - Checks age, health, energy, and cooldown requirements
- **Genome Crossover** - Combines parent genomes with mutation support
- **Pregnancy/Gestation** - MVP instant birth or full gestation period
- **Mate Finding** - Automatic partner detection within range
- Events: `OnReproductionAttempted`, `OnReproductionSuccessful`, `OnBirth`, `OnPregnancyStarted`

### PopulationRegistry.cs
Tracks all living organisms:
- **Query Methods** - Filter by species, age, health, position, pregnancy status
- **Events** - `OnOrganismRegistered`, `OnOrganismDied`, `OnPopulationChanged`, `OnPopulationEmpty`
- **Statistics** - Get population stats, species breakdown, generation tracking
- **Family Tree** - Get offspring, siblings, ancestors

### Corpse.cs
Individual corpse behavior:
- **Decay System** - Visual and nutritional decay over time
- **Edible** - Other creatures can eat corpses for nutrition
- **Cleanup Timer** - Automatic removal after configured delay
- Events: `OnDecayStart`, `OnFullyDecayed`, `OnCleanup`

### CorpseManager.cs
Global corpse management:
- **Limits** - Max corpses total and per species
- **Priority Queue** - Older corpses cleaned first
- **Culling** - Disable distant corpses for performance
- **Factory Methods** - Create corpses from prefab or simple primitives

### DeathSystem.cs
Handles organism death:
- **Cause Detection** - Starvation, health depletion, old age, exhaustion
- **Corpse Creation** - Automatic or manual corpse spawning
- **Delay Support** - Death animations before processing
- Integration with PopulationRegistry and CorpseManager

### LifecycleBridge.cs
Integrates Norn with lifecycle systems:
- Implements `IReproducible` and `ITrackedOrganism`
- Automatic registration with all lifecycle systems
- Handles reproduction state transitions
- Manages pregnancy and birth

## Configuration

### ReproductionConfig (ScriptableObject)
Create via: `Assets > Create > Albia > Reproduction Config`
- `minReproductionAge` - Minimum age to reproduce
- `maxReproductionAge` - Age limit for reproduction
- `minHealth` - Required health percentage
- `minEnergy` - Required energy percentage
- `useGestation` - Enable/disable pregnancy duration
- `baseGestationDuration` - How long pregnancy lasts
- `mutationRate` - Chance of mutation
- `mutationStrength` - How strong mutations are

### CorpseConfig (ScriptableObject)
Create via: `Assets > Create > Albia > Corpse Config`
- `decayDelay` - Time before decay starts
- `decayDuration` - How long to fully decay
- `cleanupDelay` - Time before removal
- `canBeEaten` - Allow consumption
- `nutritionalValue` - Food value (0-1)

## Usage

### Basic Setup
1. Add `LifecycleBridge` and `DeathSystem` to your Norn prefab
2. Create `ReproductionConfig` and `CorpseConfig` assets
3. Add `ReproductionSystem` and `CorpseManager` singletons to scene
4. Add `PopulationRegistry` singleton to scene

### Spawning a Creature
```csharp
var norn = Instantiate(nornPrefab, position, rotation);
var lifecycle = norn.GetComponent<LifecycleBridge>();
lifecycle.Initialize(parentA: null, parentB: null, gen: 1);
```

### Querying Population
```csharp
// Get all living Norns
var norns = PopulationRegistry.Instance.GetBySpecies("Norn");

// Get nearby creatures
var nearby = PopulationRegistry.Instance.GetNearby(position, maxDistance: 10f);

// Get pregnant organisms
var pregnant = PopulationRegistry.Instance.GetPregnantOrganisms();

// Get population stats
var stats = PopulationRegistry.Instance.GetStatistics();
```

### Reproduction
```csharp
// Automatic (find partner and reproduce)
lifecycleBridge.TryReproduce();

// Manual (with specific partner)
ReproductionSystem.Instance.TryReproduce(mother, father);

// Check if ready
bool canReproduce = ReproductionSystem.Instance.CanReproduce(organism);
```

### Death & Corpse
```csharp
// Trigger death
GetComponent<DeathSystem>().TriggerDeath("Old Age");

// Create manual corpse
Corpse corpse = CorpseManager.Instance.CreateSimpleCorpse(
    position, organismId, species, age, genome
);
```

## Events

Subscribe to lifecycle events:
```csharp
// Reproduction
ReproductionSystem.Instance.OnBirth += (sender, args) => {
    Debug.Log($"New birth: {args.ChildGenome} at {args.BirthPosition}");
};

// Population
PopulationRegistry.Instance.OnOrganismDied += (sender, args) => {
    Debug.Log($"Organism died: {args.Organism.Name}");
};

PopulationRegistry.Instance.OnPopulationEmpty += (sender, args) => {
    Debug.Log("Population extinct!");
};

// Corpses
CorpseManager.Instance.OnCorpseCreated += (sender, corpse) => {
    Debug.Log($"Corpse created: {corpse.SpeciesName}");
};
```
