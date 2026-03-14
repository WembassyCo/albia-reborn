# MVP Scene Setup Guide

## Unity Setup Steps

### 1. Create Main Scene
- File → New Scene
- Save as `Assets/Scenes/MainScene.unity`

### 2. Setup Ground Plane
- Create → 3D Object → Plane
- Scale: (10, 1, 10)
- Add NavMesh:
  - Window → AI → Navigation
  - Select Plane
  - Check "Navigation Static"
  - Bake NavMesh

### 3. Create Norn Prefab
- Create → 3D Object → Capsule
- Rename to "Norn"
- Add Components:
  - Norn.cs
  - NavMeshAgent
  - Rigidbody (optional)
  - Capsule Collider
- Assign in GameManager (see step 5)

### 4. Create Food Prefab
- Create → 3D Object → Sphere
- Rename to "Food"
- Scale: (0.5, 0.5, 0.5)
- Add Components:
  - FoodSource.cs
  - Sphere Collider (Is Trigger: true)
- Layer: "Food"
- Tag: "Food"

### 5. Setup GameManager
- Create Empty GameObject
- Rename to "GameManager"
- Add Components:
  - GameManager.cs
  - TimeManager.cs
- Assign in Inspector:
  - Norn Prefab
  - Food Prefab

### 6. Setup Camera
- Select Main Camera
- Add Components:
  - ObserverCamera.cs
  - InteractionController.cs
- Position: (0, 10, -10)
- Rotation: (30, 0, 0)

### 7. UI Setup
- Create → UI → Canvas
- Add child panels:
  - CreatureInspector
  - StatsPanel
  - TimeControls
- Assign scripts to respective panels

### 8. Layers
- Edit → Project Settings → Tags and Layers
- Add: "Food", "Creature"
- Assign to respective objects

## Play Test
1. Press Play
2. Watch 3 Norns spawn
3. Use WASD to move camera
4. Click on Norn to inspect
5. Watch energy consumption
6. Food should spawn periodically

## Expected MVP Behavior
- Norns spawn with full energy
- Energy drains over time
- Norns seek food when hungry
- Norns wander when not hungry
- Death when energy reaches 0
- Inspector shows real-time stats