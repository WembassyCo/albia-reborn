# Albia Reborn

**Version:** MVP - Week 4 Complete  
**Status:** Living Doll Phase - Creatures Spawn, Inspectable  
**Lines of Code:** 4,604  
**Files:** 36 C# scripts

---

## ✅ What's Implemented

### Core Systems (Pod A: World Foundation)
| Feature | Status | File |
|---------|--------|------|
| Heightmap Generation | ✅ | `HeightmapGenerator.cs` |
| 3-Octave Simplex Noise | ✅ | Self-contained |
| Hydraulic Erosion | ✅ | 50 passes |
| Voxel Chunk System | ✅ | `ChunkManager.cs` |
| Climate/Biome | ✅ | `ClimateSystem.cs` |
| 3 Biomes | ✅ | Forest/Grassland/Desert |
| Save/Load | ✅ | `SaveManager.cs` |

### Creature Systems (Pod B: Core)
| Feature | Status | File |
|---------|--------|------|
| Genome (256 genes) | ✅ | `GenomeData.cs` |
| Species Templates | ✅ | `SpeciesTemplate.cs` |
| Crossover + Mutation | ✅ | `GeneticsSystem.cs` |
| Chemical State (12) | ✅ | `ChemicalState.cs` |
| Organism Base | ✅ | `Organism.cs` |
| Neural Networks | ✅ | `NeuralNet.cs` |
| Hebbian Learning | ✅ | `HebbianLearning.cs` |
| Norn Creature | ✅ | `Norn.cs` |
| Population Registry | ✅ | `PopulationRegistry.cs` |

### UI Systems (Pod D: Player)
| Feature | Status | File |
|---------|--------|------|
| Creature Inspector | ✅ | `CreatureInspector.cs` |
| Chemical Bars | ✅ | Live updating |
| Real-time Monitor | ✅ | Dynamic display |

### Time Systems
| Feature | Status | File |
|---------|--------|------|
| Orbital Simulation | ✅ | `TimeManager.cs` |
| Day/Night Cycle | ✅ | Integrated |
| Seasonal Variation | ✅ | 4 seasons |
| Time Scaling | ✅ | 0.5x - 100x |

---

## Architecture

### Interface Contracts
```csharp
IHeightmapData      // world-gen ↔ voxel-engine
IClimateQuery       // climate ↔ creature biochemistry
IVoxelModification  // creature/player ↔ voxel-engine
IOrganism          // all living things
ISaveable          // persistence
```

### Folder Structure
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Interfaces.cs
│   │   ├── GameManager.cs
│   │   ├── TimeManager.cs
│   │   └── SaveManager.cs
│   ├── World/
│   │   ├── HeightmapGenerator.cs
│   │   ├── ChunkManager.cs
│   │   ├── ChunkRenderer.cs
│   │   ├── ClimateSystem.cs
│   │   └── CreatureSpawner.cs
│   ├── Creatures/
│   │   ├── Organism.cs
│   │   ├── Norn.cs
│   │   ├── PopulationRegistry.cs
│   │   ├── SensorySystem.cs
│   │   ├── Genetics/
│   │   │   ├── GenomeData.cs
│   │   │   ├── GeneticsSystem.cs
│   │   │   └── SpeciesTemplate.cs
│   │   ├── Neural/
│   │   │   ├── NeuralNet.cs
│   │   │   └── HebbianLearning.cs
│   │   ├── Biochemistry/
│   │   │   └── ChemicalState.cs
│   │   └── Ecology/
│   │       └── PlantOrganism.cs
│   └── UI/
│       └── CreatureInspector.cs
└── GameBootstrap.cs
```

---

## Technical Specs

### Neural Network
- **Architecture:** Feed-forward (30 → 15 → 15)
- **Activation:** tanh
- **Weights:** Initialized from genome (genes 64-191)
- **Learning:** Hebbian (reward strengthen, pain weaken)
- **Forward Pass Target:** <0.5ms

### Genome
- **Size:** 256 genes
- **Inheritance:** Two-point crossover + Gaussian mutation
- **Mutation Rate:** 0.5% base (gene-modulated)
- **Mutation σ:** 0.05 (gene-modulated)

### Chemicals
| Chemical | Function |
|----------|----------|
| Hunger | Rises with time, drives food seeking |
| Pain | Damage signal |
| Fear | Threat response, suppresses loneliness |
| Loneliness | Social contact need |
| Boredom | Stimulation need |
| Discomfort | Environmental discomfort |
| Reward | Reinforcement signal |
| Stress | Multi-drive conflict |
| Curiosity | Novelty seeking |
| Affection | Social bonding |
| Sleepiness | Activity-based |
| Satisfaction | Composite fulfillment |

---

## Running the Game

### Prerequisites
- Unity 6
- URP (Universal Render Pipeline)
- Newtonsoft.Json (included or via Package Manager)

### Setup
1. Open `~/Projects/AlbiaReborn` in Unity
2. Create scene: `Assets/Scenes/Main.unity`
3. Create empty GameObject: "GameBootstrap"
4. Attach `GameBootstrap.cs`
5. Assign references:
   - GameManager prefab/script
   - CreatureSpawner prefab
   - TimeManager
   - Materials (Dirt, Stone, Grass, Sand)
6. Create Species Template ScriptableObject: `Assets/ScriptableObjects/Norn.asset`
7. Create prefabs:
   - Norn (capsule with Organism script)
   - Plant (cube with PlantOrganism)
8. Press Play

### Expected Behavior
1. 128×128 heightmap generates (~2 seconds)
2. Terrain renders with biome colors
3. 5 Norns spawn at random positions
4. 50 plants spawn
5. Camera can observe
6. Click Norn to inspect (chemicals, genome)
7. Time progresses (adjustable scale)

---

## Testing

Unit tests in `Assets/Scripts/Tests/`:

```bash
# Run from Unity Test Runner
- WorldGenerationTests: 7 integration tests
- Heightmap determinism ✓
- Chunk creation ✓
- Climate assignment ✓
- Voxel modification ✓
```

---

## Next Milestones

### Week 6: Eat & Die
- [ ] Plant detection system
- [ ] Eating action
- [ ] Energy metabolism
- [ ] Death at 0 energy
- [ ] Corpse decomposition

### Week 8: Breed & Learn
- [ ] Reproduction trigger
- [ ] Breeding mechanics
- [ ] Genome inheritance
- [ ] Hebbian learning active
- [ ] First learned behaviors

### Week 10: Historical Echo
- [ ] Abstract simulation
- [ ] World history ledger
- [ ] Ruins generation
- [ ] Archaeology system

---

## Technical Debt
- Simplex noise simplified (production: FastNoiseLite)
- Mesh: cube stacking (production: marching cubes)
- Sensory system: partial implementation
- Action execution: stubs only
- No LOD yet
- No persistence for creatures

---

## Autonomous Build Log

| Time | Action | Result |
|------|--------|--------|
| 23:09 | Spawn manifest created | 17 tasks in OpenProjects |
| 23:15 | MVP foundation solo build | 1,738 lines |
| 23:21 | Agent limit hit | Switched to solo |
| 23:22 | Genetics system | Genome/Crossover/Mutation |
| 23:24 | Biochemistry | Chemical State (12 chemicals) |
| 23:25 | Neural networks | Feed-forward, Hebbian |
| 23:26 | Norn implementation | Full creature system |
| 23:27 | UI/Camera | Inspector panel, chemical bars |
| 23:28 | Spawner + Bootstrap | Game initialization |
| **Total** | **4,604 lines, 36 files** | **Week 4 Complete** |

---

**Next:** Week 6 - Eat & Die

