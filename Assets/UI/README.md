# Albia Reborn - UI System

## Overview
This folder contains the Unity UI system for observing and controlling creatures in Albia Reborn.

## Scripts

### 1. CreatureInspector.cs
Displays detailed information about a selected creature.

**Features:**
- Name, Age, and Life Stage (Baby, Child, Youth, Adult, Elder)
- Health, Energy, and Hunger bars
- Chemical state visualization (11 chemical values with color-coded bars)
- Critical state indicators (flashing health when low)
- Auto-updating display

**Required UI Setup:**
- Assign TextMeshProUGUI components for text displays
- Assign Image components with Fill type for bars
- Create `chemicalBarPrefab` with: Fill (Image), Label (TextMeshProUGUI), Value (TextMeshProUGUI) children

### 2. StatsPanel.cs
Shows world-level statistics and global information.

**Features:**
- Population count (alive/deceased)
- World time and day display
- Season/Month tracking
- Selected creature summary
- Biome/temperature/moisture display
- Tracks all creatures automatically

**World Simulation:**
- 2 real minutes = 1 in-game day
- 8 months = 1 year
- Seasons: Spring, Summer, Fall, Winter

### 3. UIManager.cs
Central controller for all UI functionality.

**Features:**
- Pause/Play toggle (Spacebar)
- Time scale slider (0x - 5x speed)
- Hotkeys 1-5 for preset speeds
- Step mode when paused (Period key)
- Creature selection via click
- Selection indicator visualization
- Escape for deselect/menu

**Event System:**
- `OnPause` - Simulation paused
- `OnPlay` - Simulation resumed
- `OnTimeScaleChanged(float)` - Speed modified
- `OnCreatureSelected(Norn)` - Creature clicked

## Quick Setup

1. Create a Canvas (Screen Space - Overlay)
2. Add `UIManager` to a top-level GameObject
3. Assign references:
   - Pause/Play buttons
   - Time scale slider
   - CreatureInspector panel
   - StatsPanel
4. Configure layers for creature selection
5. Assign main camera for raycasting

## Inspector UI Layout Example

```
Canvas
├── UIManager (Script: UIManager)
│   └── Panel: CreatureInspector
│       ├── Header (Name, Stage, Icon)
│       ├── Core Stats (Age, Energy with bars)
│       ├── Status Bars (Health, Hunger, Energy, Comfort)
│       └── Chemical States (Scroll view with bars)
├── Panel: StatsPanel
│   ├── World Stats (Population, Time, Season)
│   └── Selected Info (Brief creature summary)
└── Panel: PauseMenu (shown when paused)
    ├── Pause/Play buttons
    └── Time Scale slider
```

## Creature Selection
- Left-click on creature to inspect
- Selection indicator appears above creature
- Escape to deselect
- Click empty space to deselect

## Hotkeys
| Key | Action |
|-----|--------|
| Space | Toggle Pause/Play |
| 1-5 | Time Scale Presets (0x, 0.5x, 1x, 2x, 5x) |
| . (Period) | Step One Frame (when paused) |
| Escape | Deselect / Pause Menu |

## Dependencies
- Unity UI System (Canvas, Image, Button, Slider)
- TextMeshPro (for all text)
- Albia.Creatures namespace
- Albia.Creatures.Neural namespace
- AlbiaReborn.Climate namespace

## Customization

### Time Scale Presets
Edit `presetTimeScales` array in UIManager:
```csharp
[SerializeField] private float[] presetTimeScales = new[] { 0f, 0.5f, 1f, 2f, 5f };
```

### Bar Colors
All bar colors are customizable in CreatureInspector inspector:
- Health, Energy, Hunger, etc.

### Update Intervals
Set `updateInterval` to control refresh rate (default: 0.5s for inspector, 1s for stats).