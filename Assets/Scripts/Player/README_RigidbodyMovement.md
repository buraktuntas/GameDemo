# AAA-Grade Rigidbody FPS Movement System

## 📦 Components

### 1. **RigidbodyPlayerMovement.cs**
Professional FPS movement controller using Rigidbody physics.

**Features:**
- ✅ Smooth acceleration/deceleration (no instant speed changes)
- ✅ Rigidbody-based (realistic physics)
- ✅ Slope handling with friction and sliding
- ✅ Air control (reduced compared to ground)
- ✅ Sprint system
- ✅ Jump system (configurable multi-jump)
- ✅ "Heavy" physics feel (mass-based)
- ✅ Network-ready structure

**Exposed Settings:**
- Walk Speed: 4.5 m/s (default)
- Sprint Speed: 7.0 m/s (default)
- Acceleration: 0.15 (smooth)
- Deceleration: 0.25 (smooth stop)
- Jump Force: 7 (realistic)
- Player Mass: 80kg (heavy feel)
- Slope Physics: Friction, slide force, max angle

---

### 2. **RigidbodyPlayerCamera.cs**
AAA-grade camera system with advanced features.

**Features:**
- ✅ Camera rotation with damping (NOT locked 1:1 to movement)
- ✅ Strafe tilt (2-4 degrees when moving sideways)
- ✅ Realistic head bob (scales with velocity)
- ✅ Sprint FOV kick (8 degrees default)
- ✅ Landing impact shake
- ✅ Smooth transitions for all effects

**Exposed Settings:**
- Mouse Sensitivity X/Y
- Rotation Damping: 0.1 (smooth camera)
- Strafe Tilt: 3 degrees max
- Head Bob: Vertical/horizontal amounts
- Sprint FOV: 8 degrees increase
- All effects can be toggled on/off

---

## 🎮 Setup Instructions

### Step 1: Create New Input Actions

1. Right-click in Project → Create → Input Actions
2. Name it `PlayerInputActions`
3. Add Action Map: `Player`
4. Add Actions:
   - **Move** (Value, Vector2) → Binding: WASD / Left Stick
   - **Look** (Value, Vector2) → Binding: Mouse Delta / Right Stick
   - **Jump** (Button) → Binding: Space / South Button
   - **Sprint** (Button) → Binding: Left Shift / Left Trigger

5. Generate C# Class (Inspector → "Generate C# Class")

### Step 2: Create Player GameObject

1. Create Empty GameObject: "Player"
2. Add components:
   - **Rigidbody** (auto-added by script)
   - **CapsuleCollider** (auto-added by script)
     - Height: 2
     - Radius: 0.5
     - Center: (0, 1, 0)
   - **RigidbodyPlayerMovement** (script)

3. Create child GameObject: "CameraHolder"
   - Position: (0, 1.6, 0) [eye level]
   - Add **Camera** component
   - Add **RigidbodyPlayerCamera** (script)
   - Add **AudioListener**

### Step 3: Configure Components

**RigidbodyPlayerMovement:**
- Assign `Camera Transform` → CameraHolder
- Configure speeds, acceleration (use defaults for Battlefield feel)

**RigidbodyPlayerCamera:**
- Assign `Player Camera` → Camera component
- Assign `Player Body` → Player root transform
- Adjust mouse sensitivity to preference

### Step 4: Connect Input System

1. Add **PlayerInput** component to Player
2. Assign `Actions` → PlayerInputActions asset
3. Behavior: `Invoke Unity Events`
4. Wire up events:
   - Move → `RigidbodyPlayerMovement.OnMove`
   - Look → `RigidbodyPlayerCamera.OnLook`
   - Jump → `RigidbodyPlayerMovement.OnJump`
   - Sprint → `RigidbodyPlayerMovement.OnSprint`

### Step 5: Setup Layers

Create layers for ground detection:
- **Ground** - Floors, terrain
- Assign Ground layer to floor objects
- Set `RigidbodyPlayerMovement.groundMask` to include Ground layer

---

## 🎯 Physics Feel Comparison

| Setting | Arcade | Balanced | Heavy (Battlefield) |
|---------|--------|----------|---------------------|
| Acceleration | 0.3 | 0.15 | 0.08 |
| Deceleration | 0.5 | 0.25 | 0.15 |
| Player Mass | 50kg | 80kg | 100kg |
| Linear Drag | 1.0 | 2.0 | 3.0 |
| Air Control | 0.8 | 0.3 | 0.1 |

**Current defaults = Balanced (slightly heavy)**

---

## 📝 Code Architecture

### Movement Flow:
```
FixedUpdate()
  ├─ CheckGroundStatus()      // SphereCast ground detection
  ├─ HandleMovement()          // Smooth acceleration, slope projection
  ├─ HandleSlopePhysics()      // Friction, sliding
  ├─ HandleJump()              // Jump force, fall gravity
  └─ UpdateMovementState()     // Send state to camera
```

### Camera Flow:
```
Update()
  ├─ HandleCameraRotation()    // Pitch (camera) + Yaw (body) with damping
  ├─ HandleStrafeTilt()        // Left/right tilt
  ├─ HandleHeadBob()           // Sin/cos wave position offset
  ├─ HandleSprintFOV()         // Smooth FOV transition
  └─ HandleLandingShake()      // Impact shake (if timer > 0)
```

---

## 🔧 Advanced Customization

### Slope Behavior

**Gentle slopes (< 15°):**
- No friction, normal movement

**Medium slopes (15-45°):**
- Friction applied when moving
- Strong friction when standing still

**Steep slopes (> 45°):**
- Player slides down automatically
- `slopeSlideForce` controls speed

**Adjust:**
- `maxSlopeAngle`: 45° (walkable limit)
- `minSlopeFrictionAngle`: 15° (friction starts)
- `slopeFriction`: 8 (friction strength)
- `slopeSlideForce`: 10 (slide speed)

### Head Bob Realism

**Current setup:**
- Vertical: 0.03 (subtle)
- Horizontal: 0.015 (half of vertical)
- Frequency: 10 cycles/sec at full speed
- Minimum speed: 0.5 m/s

**For more/less bob:**
- Increase `bobVerticalAmount` for exaggerated bob
- Decrease `bobFrequency` for slower bob
- Increase `minBobSpeed` to only bob when running

### Camera Tilt Style

**Battlefield style (current):**
- Max tilt: 3°
- Tilt speed: 8 (smooth)

**Call of Duty style:**
- Max tilt: 2°
- Tilt speed: 12 (snappy)

**Disable:**
- `enableStrafeTilt = false`

---

## 🐛 Debugging

### Ground Detection Issues?
- Check `groundMask` layer settings
- Adjust `groundCheckDistance` (default 0.3)
- Use Gizmos in Scene view (select Player in Hierarchy)
  - Green sphere = grounded
  - Red sphere = in air
  - Yellow line = ground normal

### Movement feels "floaty"?
- Increase `playerMass` (80 → 100kg)
- Increase `linearDrag` (2 → 3)
- Decrease `acceleration` (0.15 → 0.1)

### Movement too "heavy"?
- Decrease `playerMass` (80 → 60kg)
- Decrease `linearDrag` (2 → 1.5)
- Increase `acceleration` (0.15 → 0.2)

### Camera rotation too smooth?
- Decrease `rotationDamping` (0.1 → 0.05)
- Or set to 0 for instant rotation

---

## 🚀 Network Integration (Mirror)

Both scripts are structured for easy network integration:

1. Add `NetworkBehaviour` inheritance:
```csharp
public class RigidbodyPlayerMovement : NetworkBehaviour
```

2. Add authority checks:
```csharp
private void FixedUpdate()
{
    if (!isLocalPlayer) return;
    // ... existing code
}
```

3. Use `[Command]` for server-side validation if needed

4. Use `[ClientRpc]` for visual effects

**Scripts are already component-based, making network sync easy.**

---

## 📊 Performance

**Optimizations included:**
- Cached component references (Awake)
- Cached capsule values
- SphereCast (more efficient than multiple raycasts)
- Conditional effect execution (only when needed)
- No FindObjectOfType calls
- No per-frame allocations

**Expected performance:**
- **CPU:** < 0.1ms per frame (Unity Profiler)
- **Physics:** Standard Rigidbody cost
- **Memory:** Minimal (no allocations in Update/FixedUpdate)

---

## ✅ Checklist

Before testing:
- [ ] Input Actions created and C# class generated
- [ ] PlayerInput component added and events wired
- [ ] CapsuleCollider configured (Height: 2, Radius: 0.5)
- [ ] Camera at correct height (1.6 units)
- [ ] Ground layer setup and assigned
- [ ] PlayerCamera references assigned (Camera, PlayerBody)
- [ ] PlayerMovement references assigned (CameraTransform)

---

## 🎮 Controls (Default)

| Action | Keyboard/Mouse | Gamepad |
|--------|----------------|---------|
| Move | WASD | Left Stick |
| Look | Mouse | Right Stick |
| Jump | Space | South Button (A/X) |
| Sprint | Left Shift (hold) | Left Trigger |

---

## 📚 Comparison to CharacterController

| Feature | CharacterController | Rigidbody (This System) |
|---------|---------------------|-------------------------|
| Physics interactions | ❌ Limited | ✅ Full physics |
| Slope sliding | ❌ Teleports | ✅ Realistic slide |
| Pushable objects | ❌ No | ✅ Yes (with mass) |
| Network sync | ⚠️ Manual | ✅ Easier (Transform sync) |
| Performance | ✅ Faster | ⚠️ Slightly slower |
| Setup complexity | ✅ Simple | ⚠️ More settings |
| Feel | ⚠️ Arcade | ✅ Heavy/realistic |

---

## 🔗 References

Inspired by:
- Battlefield series movement
- Call of Duty: Modern Warfare
- AAA FPS best practices

Created for: **Tactical Combat** Unity project
