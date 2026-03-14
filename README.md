# Albia Reborn 🧬

> *"Watch them live. Help them thrive. Learn their ways."*

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

An artificial life simulation game built in Unity, inspired by the classic Creatures series. Spawn organisms with unique genetic blueprints, watch them evolve neural networks that drive their behavior, and shape their world through direct interaction or gentle guidance.

---

## 🎮 Vision

Albia Reborn recreates the magic of digital life: creatures with biological drives, emergent behaviors shaped by neural networks, and a living world that responds to their presence. Unlike traditional AI, these creatures learn and adapt in real-time through simulated biochemistry and evolved brains.

**Core Pillars:**
- 🧬 **Genetics:** Every creature carries a unique genome that shapes appearance and predisposition
- 🧠 **Neural Networks:** Simple but flexible brains that learn from experience (not pre-trained)
- 🌍 **Living World:** Voxel-based terrain with chemical diffusion, resources, and environmental hazards
- 👤 **Player Agency:** Observe, experiment, guide, or intervene as a benevolent (or chaotic) force

---

## 🏗️ Architecture

This project follows clean architecture principles with well-defined system boundaries:

```
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│    Terrain    │ ◀─│   Creatures   │─▶ │  AI/Neural    │
│     System    │   │    System     │   │   Networks    │
└───────────────┘   └───────────────┘   └───────────────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            ▼
                    ┌───────────────┐
                    │   Simulation  │
                    │     Core      │
                    └───────────────┘
```

**Key Documents:**
- 📐 [`ARCHITECTURE.md`](ARCHITECTURE.md) — System boundaries, data flows, integration points
- 📋 [`API_CONTRACTS.md`](API_CONTRACTS.md) — C# interfaces for cross-system communication
- 📊 [`INTEGRATION_STATUS.md`](INTEGRATION_STATUS.md) — Health dashboard, blockers, checkpoints

---

## 🚀 Getting Started

### Prerequisites

- **Unity 2023.2 LTS** or newer
- **.NET 8.0 SDK** (for tooling)
- **Git LFS** (for asset storage)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/WembassyCo/albia-reborn.git
cd albia-reborn

# Open in Unity Hub
open -a Unity\ Hub albia-reborn
```

### Project Structure

```
albia-reborn/
├── Assets/                 # Unity assets
│   ├── Scripts/
│   │   ├── Core/          # Simulation core, ECS
│   │   ├── Terrain/       # Voxel world, heightmaps
│   │   ├── Creatures/     # Organisms, genomes, biochemistry
│   │   ├── AI/            # Neural networks, decision making
│   │   └── Player/        # Tools, UI, observation
│   ├── Prefabs/
│   ├── Scenes/
│   └── Resources/
├── Docs/                   # Architecture and design docs
├── Tests/                  # PlayMode and EditMode tests
└── CI/                     # Build scripts and automation
```

---

## 🛠️ Development Roadmap

| Phase | Focus | Target |
|-------|-------|--------|
| **Week 1** | Architecture & Contracts | ✅ Foundation, Interfaces defined |
| **Week 2** | Terrain & Sim Core | 🔄 ECS, Voxel storage, Rendering |
| **Week 3** | Creatures & AI | ⏸️ First spawn, Brain wiring |
| **Week 4** | Player Tools & Integration | ⏸️ Observation, Spawning |
| **Week 5** | Polish & Save/Load | ⏸️ Alpha build |

See [GitHub Projects](https://github.com/WembassyCo/albia-reborn/projects) for detailed task tracking.

---

## 🔧 Contributing

This is an internal Wembassy project. For team members:

1. **Branch naming:** `feature/weekN-short-description`
2. **PR required:** All changes via pull request
3. **Tests:** EditMode tests for systems, PlayMode for integration
4. **Documentation:** Update ARCHITECTURE.md for interface changes

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for system design decisions.

---

## 📜 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgments

- Inspired by **Creatures** (CyberLife, 1996) and the Norns
- Built with **Unity** and gratitude for the gamedev community

---

*Built with 💙 by Wembassy*
