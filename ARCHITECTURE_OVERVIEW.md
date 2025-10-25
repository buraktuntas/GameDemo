# Architecture Overview

## 🏗️ System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        TACTICAL COMBAT MVP                       │
│                     (Unity 2022.3 LTS + URP)                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      NETWORK LAYER (Mirror)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Host Server  │  │  P2P Clients │  │ Server Auth  │          │
│  │  Authority   │  │   Movement   │  │  Validation  │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       CORE GAME SYSTEMS                          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              MatchManager (Game Orchestrator)             │  │
│  │  • Phase Flow (Lobby → Build → Combat → RoundEnd)        │  │
│  │  • BO3 Tracking                                           │  │
│  │  • Win Condition Detection                                │  │
│  │  • Player State Management                                │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
         │              │              │              │
         ▼              ▼              ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   PLAYER     │ │   BUILDING   │ │   COMBAT     │ │  OBJECTIVES  │
│   SYSTEM     │ │   SYSTEM     │ │   SYSTEM     │ │   SYSTEM     │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
│              │ │              │ │              │ │              │
│ Controller   │ │ Placement    │ │ Health       │ │ Vision       │
│ Camera       │ │ Validation   │ │ Weapons      │ │ Sabotage     │
│ Abilities    │ │ Structures   │ │ Projectiles  │ │ Traps        │
│ Input        │ │ Budgets      │ │ Damage       │ │ Control Pts  │
└──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘
                                         │
                                         ▼
                              ┌──────────────────┐
                              │   UI/HUD LAYER   │
                              │  • Phase Display │
                              │  • Resources     │
                              │  • Health        │
                              │  • Abilities     │
                              │  • Team Status   │
                              └──────────────────┘
```

## 🎮 Game Flow Architecture

```
┌────────────┐
│   LOBBY    │  Players connect, select roles
└────────────┘
      │
      ▼ Auto-start (2+ players)
┌────────────┐
│   BUILD    │  Duration: 2:30
│   PHASE    │  • Free placement
└────────────┘  • Role budgets
      │         • Structure validation
      ▼ Timer expires
┌────────────┐
│   COMBAT   │  Duration: 8:00 max
│   PHASE    │  • Single life
└────────────┘  • Weapons active
      │         • Abilities enabled
      │         • Mid control
      ▼ Win condition met
┌────────────┐
│ ROUND END  │  Duration: 5s
└────────────┘  • Update BO3 score
      │         • Show winner
      │
      ├──► If match not over (< 2 wins): Go to BUILD
      │
      └──► If match over (2 wins): MATCH END
```

## 🔄 Data Flow Examples

### Build Placement Flow
```
Client                      Server                    All Clients
  │                           │                            │
  │ Ghost Preview (local)     │                            │
  │──────────────────────────►│                            │
  │ CmdRequestPlace()          │                            │
  │                           │                            │
  │                      Validate:                         │
  │                      • Budget check                    │
  │                      • Phase check                     │
  │                      • Overlap check                   │
  │                           │                            │
  │                      Spawn Structure                   │
  │                           │──────────────────────────►│
  │                           │  RPC: Structure Spawned    │
  │◄────────────────────────────────────────────────────────│
  │         Structure appears on all clients               │
```

### Combat Damage Flow
```
Player A                   Server                    Player B
  │                          │                           │
  │ Fire Input               │                           │
  │─────────────────────────►│                           │
  │ CmdFire()                │                           │
  │                          │                           │
  │                   Hit Detection                      │
  │                   Apply Damage                       │
  │                          │──────────────────────────►│
  │                          │  RpcTakeDamage()          │
  │                          │                      Update Health
  │                          │                           │
  │◄─────────────────────────│──────────────────────────►│
  │         RpcShowHitEffect (to all)                    │
```

## 📦 Component Dependencies

### Player GameObject Composition
```
Player (Root)
├─ CharacterController        [Unity Built-in]
├─ NetworkIdentity           [Mirror]
├─ NetworkTransform          [Mirror]
├─ PlayerController          [Custom] ──┐
├─ CameraController          [Custom]   │
├─ Health                    [Custom]   │ All required
├─ AbilityController         [Custom]   │ for full
├─ WeaponController          [Custom]   │ functionality
├─ SabotageController        [Custom]   │
├─ BuildPlacementController  [Custom]   │
├─ PlayerHUDController       [Custom] ──┘
└─ PlayerInput               [Unity Input System]
```

### Structure GameObject Composition
```
Wall/Platform/Ramp
├─ Structure                 [Custom] ──┐ Required
├─ Health                    [Custom]   │ for all
├─ NetworkIdentity           [Mirror] ──┘ structures
└─ SabotageTarget            [Custom] (Optional, for targetable structures)
```

### Trap GameObject Composition
```
Trap (Any Type)
├─ TrapBase-derived          [Custom] ──┐ Required
├─ NetworkIdentity           [Mirror]   │ for traps
├─ Collider (Trigger)        [Unity]  ──┘
└─ SabotageTarget            [Custom] (Optional)
```

## 🎯 Role System Architecture

```
┌──────────────────────────────────────────────────┐
│          RoleDefinition (ScriptableObject)       │
│  Defines: Stats, Budgets, Abilities per role    │
└──────────────────────────────────────────────────┘
                    │
        ┌───────────┼───────────┬───────────┐
        ▼           ▼           ▼           ▼
  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
  │ Builder │ │Guardian │ │ Ranger  │ │Saboteur │
  └─────────┘ └─────────┘ └─────────┘ └─────────┘
        │           │           │           │
        ▼           ▼           ▼           ▼
  Budget: 60/40  Budget: 20/10  Budget: 10/10  Budget: 5/5
  Ability:      Ability:       Ability:       Ability:
  RapidDeploy   Bulwark        ScoutArrow     ShadowStep
```

## 🔐 Network Authority Model

### Server-Authoritative Actions
- ✅ Structure placement validation
- ✅ Damage calculation
- ✅ Trap triggering
- ✅ Sabotage success/failure
- ✅ Win condition detection
- ✅ Phase transitions
- ✅ Budget spending

### Client-Predicted Actions
- ⚡ Player movement
- ⚡ Camera rotation
- ⚡ Build ghost preview
- ⚡ UI updates

### Hybrid (Client Request → Server Validate)
- 🔄 Weapon firing
- 🔄 Ability activation
- 🔄 Structure placement
- 🔄 Sabotage interaction

## 🧩 System Interaction Matrix

|                  | Player | Building | Combat | Traps | Sabotage | Vision |
|------------------|--------|----------|--------|-------|----------|--------|
| **MatchManager** |   ✅   |    ✅    |   ✅   |   ✅  |    ✅    |   ✅   |
| **Player**       |   -    |    ✅    |   ✅   |   ✅  |    ✅    |   ✅   |
| **Building**     |   ✅   |    -     |   ✅   |   ❌  |    ✅    |   ❌   |
| **Combat**       |   ✅   |    ✅    |   -    |   ✅  |    ❌    |   ❌   |
| **Traps**        |   ✅   |    ❌    |   ✅   |   -   |    ✅    |   ❌   |
| **Sabotage**     |   ✅   |    ✅    |   ❌   |   ✅  |    -     |   ❌   |
| **Vision**       |   ✅   |    ❌    |   ❌   |   ❌  |    ❌    |   -    |

✅ = Interacts | ❌ = No interaction | - = Self

## 📊 Code Organization Pattern

All systems follow this pattern:

```
System/
├── [System]Base.cs          # Abstract base class
├── [System]Controller.cs    # Player-attached controller
├── [Implementation]A.cs     # Concrete implementation A
├── [Implementation]B.cs     # Concrete implementation B
└── [System]UI.cs           # UI integration (if needed)

Example:
Traps/
├── TrapBase.cs             # Abstract base
├── SpikeTrap.cs            # Implementation
├── GlueTrap.cs             # Implementation
├── Springboard.cs          # Implementation
└── DartTurret.cs           # Implementation
```

## 🔌 Event System

### Global Events (via MatchManager)
```csharp
MatchManager.Instance.OnPhaseChangedEvent     // Phase transitions
MatchManager.Instance.OnRoundWonEvent         // Round victories
MatchManager.Instance.OnMatchWonEvent         // Match victories
```

### Component Events
```csharp
Health.OnHealthChangedEvent                   // Health updates
Health.OnDeathEvent                           // Death notification
AbilityController.OnCooldownChanged           // Ability CD
SabotageController.OnSabotageProgress         // Sabotage progress
ControlPoint.OnControlChanged                 // Mid capture
```

## 🎨 Extensibility Points

Want to add new content? These are designed for extension:

### New Role
1. Create new RoleDefinition ScriptableObject
2. Add case in `AbilityController.ActivateAbility()`
3. Add budget in `BuildBudget.GetRoleBudget()`

### New Structure
1. Inherit from `Structure`
2. Add to `StructureType` enum
3. Add prefab reference in `BuildValidator`
4. Define cost in `Structure.GetStructureCost()`

### New Trap
1. Inherit from `TrapBase`
2. Implement `Trigger()` method
3. Add to `StructureType` enum if needed
4. Create prefab

### New Weapon
1. Inherit from `WeaponBase`
2. Implement `Fire()` method
3. Add to `WeaponController`

## 🧪 Testing Architecture

### Unit Testing Points
- ✅ `BuildBudget` calculations
- ✅ `Structure.GetStructureCost()` logic
- ✅ Phase transition logic
- ✅ Win condition detection

### Integration Testing
- ✅ Build → Combat flow
- ✅ Damage → Health → Death chain
- ✅ Ability → Cooldown → Reuse
- ✅ Sabotage → Disable → Re-enable

### Network Testing
- ✅ 2 players (Host + Client)
- ✅ 4 players (2v2)
- ✅ 8 players (4v4)
- ✅ Host migration (not in MVP)

## 📈 Performance Considerations

### Network Optimization
- `SyncVar` used sparingly (only critical state)
- `[Command]` validates before processing
- `[ClientRpc]` only for visual feedback
- Physics on server, prediction on client

### Scalability
- Object pooling for projectiles (recommended)
- Structure limit per team (enforced by budget)
- LOD for distant structures (future)
- Occlusion culling (Unity built-in)

---

## 🔑 Key Takeaways

1. **Server Authority**: All gameplay decisions on host
2. **Event-Driven**: Systems communicate via events
3. **Component-Based**: Modular design for flexibility
4. **Network-Ready**: Mirror integration throughout
5. **Extensible**: Easy to add roles, structures, weapons
6. **Balanced**: Constants in one place for tuning

---

**This architecture supports:**
- ✅ 2-8 simultaneous players
- ✅ 100+ structures per match
- ✅ Multiple rounds (BO3+)
- ✅ Role-based asymmetry
- ✅ Phase-based gameplay
- ✅ Competitive balance tuning

**Next Evolution:**
- Dedicated servers (post-MVP)
- Matchmaking (post-MVP)
- Persistence (post-MVP)
- Workshop (post-MVP)



