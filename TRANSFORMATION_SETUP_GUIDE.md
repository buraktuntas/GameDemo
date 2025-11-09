# 🛠️ GAME TRANSFORMATION - SETUP GUIDE

**Date:** 2025-01-26  
**Purpose:** Step-by-step guide to set up the transformed game in Unity Editor

---

## 📋 PRE-SETUP CHECKLIST

- [ ] Unity 6.0+ installed
- [ ] Mirror Networking package installed
- [ ] All scripts compiled without errors
- [ ] Project opens without errors

---

## 🎯 STEP 1: SCENE SETUP

### 1.1 Add ObjectiveManager to Scene

1. Create empty GameObject: `[ObjectiveManager]`
2. Add component: `ObjectiveManager`
3. Add component: `NetworkIdentity` (required for NetworkBehaviour)
4. Configure in Inspector:
   - **Core Object Prefab**: Create/assign Core Object prefab
   - **Team A Core Spawns**: Create Transform array, add spawn points
   - **Team B Core Spawns**: Create Transform array, add spawn points
   - **Team A Return Points**: Create Transform array, add return zones
   - **Team B Return Points**: Create Transform array, add return zones
   - **Sudden Death Tunnel Prefab**: Create/assign tunnel prefab
   - **Tunnel Spawn Point**: Create Transform for tunnel spawn location

### 1.2 Add BuildManager to Scene

1. Create empty GameObject: `[BuildManager]`
2. Add component: `BuildManager`
3. Add component: `NetworkIdentity`
4. Configure:
   - **Build Validator**: Assign BuildValidator reference
   - **Max Build Distance From Spawn**: 50 (default)

### 1.3 Add ScoreManager to Scene

1. Create empty GameObject: `[ScoreManager]`
2. Add component: `ScoreManager`
3. Add component: `NetworkIdentity`

### 1.4 Add BlueprintSystem to Scene

1. Create empty GameObject: `[BlueprintSystem]`
2. Add component: `BlueprintSystem`
3. Add component: `NetworkIdentity`
4. Configure:
   - **Max Blueprints Per Player**: 5 (default)

### 1.5 Add ThrowableSystem to Scene

1. Create empty GameObject: `[ThrowableSystem]`
2. Add component: `ThrowableSystem`
3. Add component: `NetworkIdentity`
4. Configure:
   - **Smoke Prefab**: Create/assign
   - **EMP Prefab**: Create/assign
   - **Sticky Bomb Prefab**: Create/assign
   - **Reveal Dart Prefab**: Create/assign

---

## 🎨 STEP 2: PREFAB CREATION

### 2.1 Core Object Prefab

1. Create GameObject: `CoreObject`
2. Add components:
   - `NetworkIdentity`
   - `CoreObject` (script)
   - `Collider` (Sphere, IsTrigger = true)
   - `Rigidbody` (optional, for physics)
3. Add visual model (cube/sphere)
4. Set Layer: `Default` or custom layer
5. Save as prefab: `Assets/Prefabs/CoreObject.prefab`

### 2.2 Throwable Prefabs

**Smoke Prefab:**
1. Create GameObject: `SmokeThrowable`
2. Add components:
   - `NetworkIdentity`
   - `ThrowableItem` (script)
   - `Rigidbody`
   - `Collider` (Sphere)
3. Add particle system for smoke effect
4. Save as: `Assets/Prefabs/Throwables/Smoke.prefab`

**EMP Prefab:**
1. Similar to Smoke, but with EMP visual effect
2. Save as: `Assets/Prefabs/Throwables/EMP.prefab`

**Sticky Bomb Prefab:**
1. Similar structure
2. Add explosion effect
3. Save as: `Assets/Prefabs/Throwables/StickyBomb.prefab`

**Reveal Dart Prefab:**
1. Similar structure
2. Add dart visual
3. Save as: `Assets/Prefabs/Throwables/RevealDart.prefab`

### 2.3 Sudden Death Tunnel Prefab

1. Create GameObject: `SuddenDeathTunnel`
2. Add components:
   - `NetworkIdentity`
   - Visual model (tunnel/portal)
   - Collider (trigger)
3. Save as: `Assets/Prefabs/SuddenDeathTunnel.prefab`

### 2.4 Info Tower Prefab

1. Create GameObject: `InfoTower`
2. Add components:
   - `NetworkIdentity`
   - `InfoTower` (script)
   - `Collider` (Sphere, IsTrigger = true)
   - `Structure` (script) - for team ownership
   - `Health` (script) - for breakable health
3. Add visual model
4. Save as: `Assets/Prefabs/Structures/InfoTower.prefab`

---

## 🖥️ STEP 3: UI SETUP

### 3.1 Update GameHUD

1. Open GameHUD GameObject in scene
2. Add new UI elements:
   - **Core Carrying Panel** (GameObject with Image/Canvas Group)
   - **Core Carrying Text** (TextMeshProUGUI)
   - **Return Core Hint Text** (TextMeshProUGUI)
   - **Sudden Death Panel** (GameObject)
   - **Sudden Death Text** (TextMeshProUGUI)
   - **Game Mode Text** (TextMeshProUGUI)
3. Assign references in GameHUD component

### 3.2 Update Scoreboard

1. Open Scoreboard GameObject
2. Update Player Entry Prefab to include:
   - **AssistsText** (TextMeshProUGUI)
   - **StructuresText** (TextMeshProUGUI)
   - **TrapKillsText** (TextMeshProUGUI)
   - **CapturesText** (TextMeshProUGUI)
   - **ScoreText** (TextMeshProUGUI)
3. Update layout to accommodate new columns

### 3.3 Create EndGameScoreboard

1. Create GameObject: `[EndGameScoreboard]`
2. Add component: `EndGameScoreboard`
3. Create UI structure:
   - **Scoreboard Panel** (main container)
   - **Winner Panel** (shows winner)
   - **Winner Text** (TextMeshProUGUI)
   - **Player List Content** (VerticalLayoutGroup)
   - **Awards Content** (VerticalLayoutGroup)
4. Create Player Entry Prefab (same as Scoreboard but with DefenseTime column)
5. Create Award Entry Prefab (simple text entry)
6. Assign all references

---

## 🎮 STEP 4: INPUT SETUP

### 4.1 Add Return Core Input

1. Open `InputSystem_Actions.inputactions`
2. Add new action to "Player" action map:
   - **Name**: `ReturnCore`
   - **Type**: Button
   - **Binding**: `E` key
3. Save asset

### 4.2 Update PlayerController Input Handling

Add input handling for Return Core (if not already present):
```csharp
// In PlayerController or InputManager
if (Input.GetKeyDown(KeyCode.E))
{
    if (IsCarryingCore())
    {
        CmdTryReturnCore();
    }
}
```

---

## 🗺️ STEP 5: SCENE LAYOUT

### 5.1 Spawn Points

1. Create spawn points for each team:
   - **Team A Spawn Points** (4-8 points)
   - **Team B Spawn Points** (4-8 points)
2. Assign to NetworkGameManager:
   - `teamASpawnPoints` array
   - `teamBSpawnPoints` array

### 5.2 Core Spawn Locations

1. **Team A Core Spawn**: Place at Team A base center
2. **Team B Core Spawn**: Place at Team B base center
3. Assign to ObjectiveManager arrays

### 5.3 Return Points

1. **Team A Return Point**: Place at Team A base (3m radius)
2. **Team B Return Point**: Place at Team B base (3m radius)
3. Assign to ObjectiveManager arrays

### 5.4 Sudden Death Tunnel Location

1. Place tunnel spawn point between bases
2. Assign to ObjectiveManager

---

## 🔗 STEP 6: NETWORK SETUP

### 6.1 Update NetworkManager

1. Open NetworkManager GameObject
2. Ensure `NetworkGameManager` component is attached
3. Verify player prefab is assigned
4. Add new prefabs to spawnable list:
   - CoreObject prefab
   - Throwable prefabs
   - SuddenDeathTunnel prefab
   - InfoTower prefab

### 6.2 Network Prefab Registration

All new NetworkBehaviour prefabs must be registered:
- CoreObject
- ThrowableItem (for all throwable types)
- InfoTower
- SuddenDeathTunnel

---

## ✅ STEP 7: VERIFICATION

### 7.1 Component Checklist

- [ ] ObjectiveManager has all references assigned
- [ ] BuildManager has BuildValidator reference
- [ ] ThrowableSystem has all prefabs assigned
- [ ] MatchManager is in scene
- [ ] ScoreManager is in scene
- [ ] BlueprintSystem is in scene
- [ ] All systems have NetworkIdentity components

### 7.2 Prefab Checklist

- [ ] CoreObject prefab created
- [ ] All throwable prefabs created
- [ ] SuddenDeathTunnel prefab created
- [ ] InfoTower prefab created
- [ ] All prefabs have NetworkIdentity

### 7.3 UI Checklist

- [ ] GameHUD updated with new elements
- [ ] Scoreboard updated with new columns
- [ ] EndGameScoreboard created and configured
- [ ] All UI references assigned

---

## 🧪 STEP 8: TESTING

### 8.1 Basic Tests

1. **Build Phase Test:**
   - Start match
   - Verify build phase starts (3 minutes)
   - Verify resource panel shows
   - Place structures
   - Verify distance limit works

2. **Combat Phase Test:**
   - Wait for build phase to end
   - Verify combat phase starts (15 minutes)
   - Verify cores spawn
   - Pick up core
   - Verify speed reduction
   - Return core
   - Verify win condition

3. **Sudden Death Test:**
   - Wait until 2 minutes remaining
   - Verify sudden death activates
   - Verify tunnel opens
   - Verify UI notification shows

4. **Scoring Test:**
   - Get kills
   - Build structures
   - Trigger traps
   - Return cores
   - Verify scores update
   - Verify end-game awards

---

## 🐛 TROUBLESHOOTING

### Common Issues:

1. **"ObjectiveManager not found"**
   - Ensure ObjectiveManager is in scene
   - Ensure it has NetworkIdentity
   - Ensure it's spawned on server

2. **"Core not spawning"**
   - Check spawn point arrays are assigned
   - Check core prefab is assigned
   - Check NetworkIdentity on prefab

3. **"Scoreboard not updating"**
   - Check MatchManager is in scene
   - Check ScoreManager is in scene
   - Verify player entries have correct text components

4. **"Throwables not working"**
   - Check prefabs are assigned
   - Check NetworkIdentity on prefabs
   - Check ThrowableItem component exists

---

## 📝 NOTES

- All new systems use Mirror Networking
- Server-authoritative validation is enforced
- Event-driven scoring reduces manual tracking
- UI updates are throttled for performance
- All systems are backward compatible with existing code

---

**Setup Complete!** Ready for testing and play.

