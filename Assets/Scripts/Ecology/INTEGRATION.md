# Ecology System Integration Guide

## Overview
The ecology foundation for Albia Reborn MVP has been implemented with the following components:

## Created Files

### 1. Organism.cs (`Assets/Scripts/Organism.cs`)
Base class for all living organisms. Provides:
- Health/Energy management
- Age tracking
- Death events
- Abstract UpdateBiology method for derived classes

### 2. PlantOrganism.cs (`Assets/Scripts/Ecology/PlantOrganism.cs`)
Inherits from Organism. Features:
- Growth rate calculation with environmental factors
- Maturation system (time-based or size-based)
- Seed spawning/reproduction
- Energy value for consumption
- MVP: Simple growth
- Full: Biome-aware, seasonal growth

### 3. FoodSource.cs (`Assets/Scripts/Ecology/FoodSource.cs`)
Simple food object for Norns. Features:
- Visual: Sphere or capsule mesh (configurable)
- **Layer: "Food" (Layer 8)** - Required for Norn detection
- Trigger collider for proximity detection
- Energy and health restoration values
- Auto-respawn system (instant or gradual regrowth)
- Animation support (rotation, bobbing)
- Tag: "Food" for easy finding

### 4. EcologyManager.cs (`Assets/Scripts/Ecology/EcologyManager.cs`)
Global ecosystem manager. Features:
- Plant population management with carrying capacity per region
- Food respawn system
- Regional spawn zones with configurable limits
- Random spawn in valid areas (ground detection)
- Seasonal cycle support (Full version)
- Events for spawn/death/consumption
- Singleton pattern for easy access

## Unity Setup Required

### 1. Create the "Food" Layer
1. Go to Edit → Project Settings → Tags and Layers
2. Under Layers, set User Layer 8 to: `Food`
3. This is REQUIRED for Norn detection via sensory system

### 2. Create Food Prefab
1. Create an empty GameObject named "Food"
2. Add FoodSource.cs component
3. Configure:
   - Energy Value: 30 (configurable)
   - Auto Respawn: true
   - Food Mesh Type: Capsule (or Sphere)
   - Assign layer "Food" (Layer 8)
4. Save as prefab in Prefabs folder

### 3. Create Plant Prefab
1. Create empty GameObject named "Plant"
2. Add PlantOrganism.cs component
3. Configure:
   - Base Growth Rate: 0.1
   - Time to Mature: 60
   - Food Energy Value: 25
   - Seed Prefab: Assign the plant prefab itself (for reproduction)
   - Assign layer "Plant"
4. Create visual (add a simple mesh child object)
5. Save as prefab

### 4. Setup Ecology Manager
1. Create empty GameObject named "EcologyManager"
2. Add EcologyManager.cs component
3. Assign:
   - Plant Prefabs: Array with your plant prefab(s)
   - Food Prefab: Your food prefab
4. Configure spawn regions or leave default

## Integration with Norn.cs

### Method 1: Direct Consumption (Simple)
```csharp
// In Norn.cs - Eating logic
Collider[] nearbyFood = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Food"));
foreach (var foodCollider in nearbyFood)
{
    FoodSource food = foodCollider.GetComponent<FoodSource>();
    if (food != null && food.IsAvailable)
    {
        float energyGained = food.Consume(10f); // Consume up to 10 energy
        state.Energy = Mathf.Min(1f, state.Energy + energyGained / 100f);
        break;
    }
}
```

### Method 2: Sensory System Integration (Recommended)
Extend the existing SensorySystem to detect Food layer:

```csharp
// In SensorySystem.cs
public float DetectFoodProximity(Vector3 position)
{
    Collider[] food = Physics.OverlapSphere(position, detectionRadius, LayerMask.GetMask("Food"));
    if (food.Length == 0) return 0f;
    
    // Return distance to nearest food (inverse - closer = higher value)
    float closest = float.MaxValue;
    foreach (var f in food)
    {
        float dist = Vector3.Distance(position, f.transform.position);
        if (dist < closest) closest = dist;
    }
    return 1f - Mathf.Clamp01(closest / detectionRadius);
}
```

## Key Constants

### FoodSource.cs
```csharp
public const string FOOD_LAYER_NAME = "Food";
public const int FOOD_LAYER = 8;  // MUST MATCH UNITY PROJECT SETTINGS
```

## API Quick Reference

### PlantOrganism
```csharp
SpawnSeeds(int count)           // Spawn new plants
HarvestSeeds(out GameObject[])  // Get seeds without killing
Consume(float amount)           // Get energy from plant
UpdateEnvironmentalConditions(float sunlight, string biome)
```

### FoodSource
```csharp
Consume(float amount)           // Eat food, returns energy gained
ConsumeFully()                  // Eat all remaining
ForceRespawn(float delay)       // Respawn after delay
```

### EcologyManager
```csharp
SpawnRandomPlant()              // Spawn a plant
SpawnRandomFood()               // Spawn food
GetNearestFood(Vector3 pos)     // Find closest food
GetPlantsInRadius(Vector3, float)  // Get plants nearby
Instance.PlantCount             // Get total plant count
Instance.FoodCount            // Get total food count
```

## Constraints Met

✅ No paid assets - All scripts use Unity built-in functionality
✅ Integrates with Organism base class - PlantOrganism inherits from Organism
✅ Food on "Food" layer (Layer 8) - Configurable and detectable
✅ Simple visual meshes - Sphere/Capsule generated procedurally
✅ MVP complete with extension points for Full version

## Notes

- FoodSource creates its own visual mesh programmatically (no assets needed)
- PlantOrganism expects a child object named "Visual" or uses the parent transform
- EcologyManager uses Physics.Raycast to find valid ground positions
- All scripts are fully commented with XML documentation
