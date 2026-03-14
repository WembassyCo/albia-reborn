# Albia Reborn - Playtest Guide

## Quick Start

1. Open Unity 6
2. Load `Assets/Scenes/MainScene.unity`
3. Select Ground plane → Window → AI → Navigation → **Bake**
4. Press **Play**

## Controls

| Key | Action |
|-----|--------|
| W/A/S/D | Camera movement |
| Right-click drag | Camera rotation |
| Scroll | Camera height |
| Left-click | Select creature |
| Shift | Sprint camera |
| Space | Pause/Resume |
| 1-5 | Time speed presets |
| Escape | Deselect |

## What to Expect

### First 30 Seconds
- 3 Norns spawn with random genomes
- They have energy, will seek food
- Food appears around the world

### First 2 Minutes
- Norns consume energy while moving
- Chemicals change (hunger rises)
- They learn to find food via neural network
- Watch inspector panel (click a Norn)

### First 5 Minutes
- First reproduction may occur
- Grendels may spawn (if Norn count > 5)
- Plants grow and spread
- Population dynamics emerge

## Debugging

Press **`** (backtick) to open debug console:
- `spawn` - Create new Norn
- `timescale 5` - Speed up time
- `help` - Show all commands

## Known Limitations (MVP)

- Neural learning is basic (Hebbian)
- Voxels are flat terrain only
- Limited creature animations
- No sound effects

## Report Issues

Check console (red errors) and note:
1. What you were doing
2. What you expected
3. What actually happened

---

**Version:** Wave 3+ MVP
**Last Updated:** March 14, 2026