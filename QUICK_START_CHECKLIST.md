# Quick Start Checklist

Use this checklist to get the MVP up and running.

## ☑️ Phase 1: Package Installation

- [ ] Unity 2022.3 LTS installed
- [ ] Project opened in Unity
- [ ] Mirror Networking installed (via Git URL or Asset Store)
- [ ] Input System installed (via Package Manager)
- [ ] TextMeshPro imported
- [ ] Project Settings > Player > Active Input Handling = "Input System Package (New)"
- [ ] Unity restarted after input system config

**Resources:** See `PACKAGES_GUIDE.md`

---

## ☑️ Phase 2: Input Actions Setup

- [ ] Created Input Actions asset: `Assets/InputSystem_Actions.inputactions`
- [ ] Player Action Map created with 8 actions:
  - [ ] Move (Vector2, WASD)
  - [ ] Look (Vector2, Mouse)
  - [ ] Jump (Button, Space)
  - [ ] Fire (Button, Left Mouse)
  - [ ] UseAbility (Button, Q)
  - [ ] ToggleBuild (Button, B)
  - [ ] Interact (Button, E)
  - [ ] SwitchWeapon (Button, Tab)
- [ ] Build Action Map created with 5 actions:
  - [ ] Place (Button, Left Mouse)
  - [ ] Rotate (Button, R)
  - [ ] SelectWall (Button, 1)
  - [ ] SelectPlatform (Button, 2)
  - [ ] SelectRamp (Button, 3)
- [ ] "Generate C# Class" checked
- [ ] Applied and saved

**Resources:** See `SETUP_GUIDE.md` Step 3

---

## ☑️ Phase 3: Layer Configuration

- [ ] Edit > Project Settings > Tags and Layers
- [ ] Layer 6: `Player`
- [ ] Layer 7: `Structure`
- [ ] Layer 8: `Trap`
- [ ] Layer 9: `Ground`

**Resources:** See `SETUP_GUIDE.md` Step 4

---

## ☑️ Phase 4: Materials Creation

- [ ] Created `Assets/Materials/TeamA_Material` (Blue: 0, 0.5, 1)
- [ ] Created `Assets/Materials/TeamB_Material` (Red: 1, 0.2, 0)
- [ ] Created `Assets/Materials/ValidPlacement_Material` (Green transparent)
- [ ] Created `Assets/Materials/InvalidPlacement_Material` (Red transparent)

---

## ☑️ Phase 5: Structure Prefabs

### Wall Prefab
- [ ] Created Wall GameObject (Cube, scale 2,2,0.5)
- [ ] Added Structure component
- [ ] Added Health component
- [ ] Added NetworkIdentity component
- [ ] Set Layer to Structure
- [ ] Added Renderer with TeamA material (placeholder)
- [ ] Saved to `Assets/Prefabs/Structures/Wall.prefab`

### Platform Prefab
- [ ] Created Platform GameObject (Cube, scale 2,0.5,2)
- [ ] Added Structure, Health, NetworkIdentity
- [ ] Set Layer to Structure
- [ ] Saved to `Assets/Prefabs/Structures/Platform.prefab`

### Ramp Prefab
- [ ] Created Ramp GameObject (Cube, scale 2,1,2)
- [ ] Added Structure, Health, NetworkIdentity
- [ ] Set Layer to Structure
- [ ] Saved to `Assets/Prefabs/Structures/Ramp.prefab`

---

## ☑️ Phase 6: Trap Prefabs

### Spike Trap
- [ ] Created SpikeTrap GameObject (Cylinder, scale 1,0.2,1)
- [ ] Added SpikeTrap component
- [ ] Added SabotageTarget component
- [ ] Added NetworkIdentity component
- [ ] Added Sphere Collider (Is Trigger, Radius 0.8)
- [ ] Set Layer to Trap
- [ ] Saved to `Assets/Prefabs/Traps/SpikeTrap.prefab`

### Glue Trap
- [ ] Created GlueTrap GameObject
- [ ] Added GlueTrap, SabotageTarget, NetworkIdentity
- [ ] Added Sphere Collider (Is Trigger)
- [ ] Set Layer to Trap
- [ ] Saved to `Assets/Prefabs/Traps/GlueTrap.prefab`

### Springboard
- [ ] Created Springboard GameObject
- [ ] Added Springboard, NetworkIdentity
- [ ] Added Box Collider (Is Trigger)
- [ ] Set Layer to Trap
- [ ] Saved to `Assets/Prefabs/Traps/Springboard.prefab`

### Dart Turret
- [ ] Created DartTurret GameObject
- [ ] Added DartTurret, SabotageTarget, Health, NetworkIdentity
- [ ] Set Layer to Trap
- [ ] Saved to `Assets/Prefabs/Traps/DartTurret.prefab`

---

## ☑️ Phase 7: Ghost Prefabs

- [ ] Duplicated Wall prefab → WallGhost
- [ ] Removed Structure, Health, NetworkIdentity
- [ ] Added BuildGhost component
- [ ] Set material to ValidPlacement_Material
- [ ] Saved to `Assets/Prefabs/Structures/WallGhost.prefab`
- [ ] Repeated for Platform and Ramp

---

## ☑️ Phase 8: Player Prefab

### Player Structure
- [ ] Created Player GameObject (Capsule)
- [ ] Created child: Model (Capsule, scale 1,2,1)
- [ ] Created child: CameraTarget (Empty, position 0,1.6,0)
- [ ] Created child: Weapons (Empty)
- [ ] Created grandchild: Bow (Empty)
- [ ] Created grandchild: Spear (Empty)

### Player Components (on root Player)
- [ ] CharacterController (Height 2, Radius 0.5)
- [ ] NetworkIdentity
- [ ] NetworkTransform
- [ ] PlayerController
- [ ] CameraController
- [ ] Health
- [ ] AbilityController
- [ ] WeaponController
- [ ] SabotageController
- [ ] BuildPlacementController
- [ ] PlayerHUDController
- [ ] PlayerInput (Actions: InputSystem_Actions, Default Map: Player)

### Weapon Components
- [ ] Bow: Added WeaponBow component
- [ ] Bow: Created child "ArrowSpawnPoint" (position forward)
- [ ] Spear: Added WeaponSpear component
- [ ] Spear: Created child "StabPoint" (position forward)

### Save Prefab
- [ ] Saved to `Assets/Prefabs/Player.prefab`

---

## ☑️ Phase 9: Scene Setup

### Created GameScene
- [ ] File > New Scene
- [ ] Saved as `Assets/Scenes/GameScene.unity`

### Environment
- [ ] Created Ground (Plane, scale 20,1,20, Layer: Ground)
- [ ] Created MidPoint (Sphere at center, Layer: Ground)
- [ ] Added Sphere Collider to MidPoint (Radius 5, Is Trigger)
- [ ] Added ControlPoint component to MidPoint

### Spawn Points
- [ ] Created SpawnPoints_TeamA parent object
- [ ] Created 4 empty GameObjects as children (scattered on one side)
- [ ] Created SpawnPoints_TeamB parent object
- [ ] Created 4 empty GameObjects as children (opposite side)

### Network Objects
- [ ] Created NetworkManager GameObject
- [ ] Added NetworkGameManager component
- [ ] Added KcpTransport component
- [ ] Created MatchManager GameObject
- [ ] Added MatchManager component
- [ ] Created BuildValidator GameObject
- [ ] Added BuildValidator component

### UI
- [ ] Created Canvas (Screen Space - Overlay)
- [ ] Created GameHUD child GameObject
- [ ] Added GameHUD component
- [ ] Created UI children (PhaseText, TimerText, RoundText, etc.)

---

## ☑️ Phase 10: Component Configuration

### NetworkManager Configuration
- [ ] Player Prefab: Assigned Player prefab
- [ ] Team A Spawn Points: Assigned array of Team A spawn points
- [ ] Team B Spawn Points: Assigned array of Team B spawn points
- [ ] Registered Spawnable Prefabs:
  - [ ] Player
  - [ ] Wall, Platform, Ramp
  - [ ] SpikeTrap, GlueTrap, Springboard, DartTurret

### BuildValidator Configuration
- [ ] Obstacle Mask: Structure, Trap layers
- [ ] Wall Prefab: Assigned
- [ ] Platform Prefab: Assigned
- [ ] Ramp Prefab: Assigned

### Player Prefab Configuration
- [ ] BuildPlacementController:
  - [ ] Placement Surface: Ground, Structure layers
  - [ ] Obstacle Mask: Structure, Trap layers
  - [ ] Wall/Platform/Ramp Ghost Prefabs: Assigned
  - [ ] Wall/Platform/Ramp Prefabs: Assigned
- [ ] WeaponController:
  - [ ] Bow: Reference to Bow child
  - [ ] Spear: Reference to Spear child

### GameHUD Configuration
- [ ] PhaseText: Assigned TextMeshProUGUI
- [ ] TimerText: Assigned TextMeshProUGUI
- [ ] RoundText: Assigned TextMeshProUGUI
- [ ] ResourcePanel: Assigned GameObject
- [ ] WallPointsText, etc.: Assigned
- [ ] HealthSlider: Assigned Slider
- [ ] (Other UI elements as created)

---

## ☑️ Phase 11: Testing

### First Test (Solo)
- [ ] Saved all changes
- [ ] Pressed Play
- [ ] NetworkManager auto-starts as Host
- [ ] Player spawns
- [ ] Can move with WASD
- [ ] Camera follows player
- [ ] HUD shows phase and timer
- [ ] Build phase starts automatically
- [ ] Can toggle build mode with B
- [ ] Combat phase starts after timer
- [ ] No errors in Console

### Second Test (Two Players)
- [ ] Edit > Project Settings > Player > "Run in Background" checked
- [ ] File > Build Settings > Build and Run
- [ ] One instance = Host (Unity Editor)
- [ ] One instance = Client (Built game)
- [ ] Both players spawn
- [ ] Both are on different teams
- [ ] Can see each other
- [ ] Build phase syncs
- [ ] Can place structures (host validates)
- [ ] Combat phase starts for both
- [ ] Weapons work
- [ ] Damage syncs
- [ ] Death triggers properly

---

## ☑️ Phase 12: Final Verification

### Core Loop Test
- [ ] Match starts in Lobby phase
- [ ] Transitions to Build phase automatically
- [ ] Can place walls, platforms, ramps
- [ ] Resources decrease when placing
- [ ] Build phase timer counts down
- [ ] Transitions to Combat phase
- [ ] Weapons fire (bow/spear)
- [ ] Health decreases when hit
- [ ] Death disables player
- [ ] Round ends when all enemies dead
- [ ] BO3 score updates
- [ ] Next round starts

### Systems Test
- [ ] Traps can be placed
- [ ] Traps trigger on enemies
- [ ] Sabotage interaction works (Saboteur only)
- [ ] Mid control point can be captured
- [ ] Abilities activate with Q
- [ ] Cooldowns work
- [ ] HUD updates correctly

---

## 🎉 Completion

- [ ] All checkboxes above completed
- [ ] No critical errors in Console
- [ ] Basic gameplay loop functional
- [ ] Ready for playtesting

**Next Steps:**
1. Gather 4-8 playtesters
2. Run multiple BO3 matches
3. Collect feedback
4. Iterate on balance (edit `GameConstants.cs`)
5. Polish visuals and VFX
6. Add SFX and music

---

**Estimated Time:**
- Phases 1-3: 1-2 hours (setup)
- Phases 4-8: 3-4 hours (prefab creation)
- Phases 9-10: 2-3 hours (scene and configuration)
- Phases 11-12: 1-2 hours (testing)

**Total: 7-11 hours** (for someone familiar with Unity)

---

**Need Help?** Check:
- `README.md` - Architecture and overview
- `SETUP_GUIDE.md` - Detailed step-by-step
- `PACKAGES_GUIDE.md` - Package installation
- `PROJECT_SUMMARY.md` - Implementation status



