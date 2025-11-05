# 🎯 TRAP SYSTEM COMPREHENSIVE AUDIT REPORT

**Date:** 2024  
**Game:** Competitive TPS Shooter with Building & Traps  
**Focus:** Network Sync, Performance, Security, Bugs

---

## 📋 EXECUTIVE SUMMARY

**Total Issues Found:** 12  
- 🔴 **Critical:** 3
- 🟠 **High Priority:** 4  
- 🟡 **Medium Priority:** 5

**System Status:** ⚠️ **Functional but has critical bugs and incomplete features**

---

## 🔴 CRITICAL BUGS

### C1: **SlowEffect DOES NOT WORK - Movement Speed Never Modified**
**Location:** `Assets/Scripts/Traps/GlueTrap.cs:47-63`  
**Severity:** 🔴 CRITICAL  
**Impact:** GlueTrap completely broken - players are not slowed

**Problem:**
```csharp
public class SlowEffect : MonoBehaviour
{
    private float multiplier;
    private Player.PlayerController player;

    public void Initialize(float dur, float mult)
    {
        multiplier = mult;
        player = GetComponent<Player.PlayerController>(); // ❌ GetComponent, not TryGetComponent
        Destroy(this, dur);
    }

    private void OnDestroy()
    {
        // Movement speed restored on destroy
        // ❌ NO CODE - movement speed is NEVER modified!
    }
}
```

**Why Critical:**
- `SlowEffect` component is added but never modifies movement speed
- `FPSController` has no mechanism to read `SlowEffect` multiplier
- Players are NOT slowed when stepping on GlueTrap
- Game mechanic completely broken

**Fix:**
```csharp
// Option 1: Add speed multiplier system to FPSController
// FPSController.cs
public float speedMultiplier = 1f; // Default 1.0

private Vector3 CalculateHorizontalMovement(Vector3 input)
{
    // ... existing code ...
    float currentSpeed = (wantsToSprint ? runSpeed : walkSpeed) * speedMultiplier;
    // ... rest of code ...
}

// SlowEffect.cs
public class SlowEffect : MonoBehaviour
{
    private float multiplier;
    private Player.FPSController fpsController;

    public void Initialize(float dur, float mult)
    {
        multiplier = mult;
        if (!TryGetComponent<Player.FPSController>(out fpsController))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SlowEffect: FPSController not found!");
            #endif
            Destroy(this);
            return;
        }
        
        // Apply slow
        fpsController.speedMultiplier *= multiplier;
        
        Destroy(this, dur);
    }

    private void OnDestroy()
    {
        // Restore speed
        if (fpsController != null)
        {
            fpsController.speedMultiplier /= multiplier;
        }
    }
}
```

**Alternative (Better):** Use a centralized effect system:
```csharp
// EffectManager.cs (new file)
public class EffectManager : MonoBehaviour
{
    private Dictionary<EffectType, float> activeEffects = new Dictionary<EffectType, float>();
    
    public void ApplyEffect(EffectType type, float value, float duration)
    {
        activeEffects[type] = value;
        StartCoroutine(RemoveEffectAfter(type, duration));
    }
    
    public float GetSpeedMultiplier()
    {
        return activeEffects.ContainsKey(EffectType.Slow) ? activeEffects[EffectType.Slow] : 1f;
    }
}
```

---

### C2: **Springboard LaunchPlayer NOT IMPLEMENTED**
**Location:** `Assets/Scripts/Traps/Springboard.cs:43-54`  
**Severity:** 🔴 CRITICAL  
**Impact:** Springboard trap completely broken - players are not launched

**Problem:**
```csharp
[ClientRpc]
private void RpcLaunchPlayer(uint targetNetId, Vector3 force)
{
    if (NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
    {
        var player = identity.GetComponent<Player.PlayerController>(); // ❌ GetComponent
        if (player != null)
        {
            // ❌ This would require adding a LaunchPlayer method to PlayerController
            Debug.Log($"Launching player with force {force}"); // ❌ Just logs, does nothing!
        }
    }
}
```

**Why Critical:**
- Springboard triggers but does nothing
- `LaunchPlayer` method doesn't exist in `PlayerController` or `FPSController`
- Players are NOT launched - trap is non-functional
- Game mechanic completely broken

**Fix:**
```csharp
// Springboard.cs - Server-side launch
[Server]
protected override void Trigger(GameObject target)
{
    if (!target.TryGetComponent<NetworkIdentity>(out var targetIdentity))
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("Springboard: Target has no NetworkIdentity");
        #endif
        return;
    }
    
    // Calculate launch force in world space
    Vector3 worldLaunchForce = transform.TransformDirection(launchForce);
    
    // Send RPC to launch player
    RpcLaunchPlayer(targetIdentity.netId, worldLaunchForce);
    
    RpcPlayTriggerEffect();
    
    // Springboard can be reused after cooldown
    isTriggered = true;
    Invoke(nameof(ResetTrap), resetTime);
}

[ClientRpc]
private void RpcLaunchPlayer(uint targetNetId, Vector3 force)
{
    if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        return;
    
    // ✅ FIX: Use TryGetComponent and FPSController
    if (identity.TryGetComponent<Player.FPSController>(out var fpsController))
    {
        fpsController.ApplyImpulse(force);
    }
    else if (identity.TryGetComponent<CharacterController>(out var controller))
    {
        // Fallback: direct velocity (requires velocity system)
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("Springboard: FPSController not found, using CharacterController fallback");
        #endif
    }
}

// FPSController.cs - Add impulse method
public void ApplyImpulse(Vector3 force)
{
    // Add to vertical velocity
    moveDirection.y += force.y;
    
    // Add horizontal force
    Vector3 horizontalForce = new Vector3(force.x, 0, force.z);
    moveDirection += horizontalForce;
}
```

---

### C3: **Multiple GetComponent Calls in Traps (GC Allocations)**
**Location:** `Assets/Scripts/Traps/Springboard.cs:22,26,48` | `Assets/Scripts/Traps/GlueTrap.cs:55`  
**Severity:** 🔴 CRITICAL (Performance)  
**Impact:** GC spikes during trap triggering, frame stalls

**Problem:**
```csharp
// Springboard.cs:22
var controller = target.GetComponent<CharacterController>(); // ❌ GC allocation

// Springboard.cs:26
RpcLaunchPlayer(target.GetComponent<NetworkIdentity>().netId, launchForce); // ❌ GC allocation

// Springboard.cs:48
var player = identity.GetComponent<Player.PlayerController>(); // ❌ GC allocation

// GlueTrap.cs:55
player = GetComponent<Player.PlayerController>(); // ❌ GC allocation
```

**Why Critical:**
- `GetComponent` causes GC allocations on every call
- Traps trigger frequently in combat
- Causes frame stutters and GC spikes
- Already fixed in other trap types (TryGetComponent used in SpikeTrap, DartTurret)

**Fix:**
```csharp
// Springboard.cs
[Server]
protected override void Trigger(GameObject target)
{
    if (!target.TryGetComponent<NetworkIdentity>(out var targetIdentity))
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning("Springboard: Target has no NetworkIdentity");
        #endif
        return;
    }
    
    Vector3 worldLaunchForce = transform.TransformDirection(launchForce);
    RpcLaunchPlayer(targetIdentity.netId, worldLaunchForce);
    
    // ... rest of code ...
}

[ClientRpc]
private void RpcLaunchPlayer(uint targetNetId, Vector3 force)
{
    if (!NetworkClient.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        return;
    
    if (identity.TryGetComponent<Player.FPSController>(out var fpsController))
    {
        fpsController.ApplyImpulse(force);
    }
}

// GlueTrap.cs
public class SlowEffect : MonoBehaviour
{
    private float multiplier;
    private Player.FPSController fpsController;

    public void Initialize(float dur, float mult)
    {
        multiplier = mult;
        if (!TryGetComponent<Player.FPSController>(out fpsController))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("SlowEffect: FPSController not found!");
            #endif
            Destroy(this);
            return;
        }
        
        fpsController.speedMultiplier *= multiplier;
        Destroy(this, dur);
    }
    
    // ... rest of code ...
}
```

---

## 🟠 HIGH PRIORITY ISSUES

### H1: **Debug.Log Not Conditionally Compiled in Traps**
**Location:** `Assets/Scripts/Traps/*.cs` (8 instances)  
**Severity:** 🟠 HIGH PRIORITY  
**Impact:** String allocations in release builds, performance overhead

**Problem:**
```csharp
// TrapBase.cs:44
Debug.Log($"{gameObject.name} armed"); // ❌ Always allocated

// SpikeTrap.cs:51
Debug.Log("Spike trap triggered!"); // ❌ Always allocated

// DartTurret.cs:102
Debug.Log($"Dart turret fired in direction {direction}"); // ❌ Always allocated

// Springboard.cs:52,60
Debug.Log($"Launching player with force {force}"); // ❌ Always allocated
Debug.Log("Springboard activated!"); // ❌ Always allocated

// GlueTrap.cs:25,43
Debug.Log($"Glue trap slowed {target.name}"); // ❌ Always allocated
Debug.Log("Glue trap activated!"); // ❌ Always allocated
```

**Why High Priority:**
- String allocations in hot paths (trap triggering)
- Performance overhead in release builds
- Already fixed in other systems (WeaponSystem, SimpleBuildMode)

**Fix:**
```csharp
// Wrap all Debug.Log calls with conditional compilation
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"Spike trap triggered!");
#endif
```

---

### H2: **Invoke() Usage Without Cleanup on Destroy**
**Location:** `Assets/Scripts/Traps/SpikeTrap.cs:34` | `Assets/Scripts/Traps/GlueTrap.cs:31`  
**Severity:** 🟠 HIGH PRIORITY  
**Impact:** Potential memory leaks if trap destroyed before Invoke completes

**Problem:**
```csharp
// SpikeTrap.cs:34
Invoke(nameof(DestroyTrap), 2f); // ❌ Not cancelled on Destroy

// GlueTrap.cs:31
Invoke(nameof(DestroyTrap), slowDuration + 1f); // ❌ Not cancelled on Destroy
```

**Why High Priority:**
- If trap is destroyed before Invoke completes, method still executes
- Can cause errors (destroying already-destroyed object)
- Should use coroutines or cancel Invoke on Destroy

**Fix:**
```csharp
// SpikeTrap.cs
private Coroutine destroyCoroutine;

[Server]
protected override void Trigger(GameObject target)
{
    // ... damage code ...
    
    RpcPlayTriggerEffect();
    MarkAsTriggered();
    
    // Use coroutine instead of Invoke
    destroyCoroutine = StartCoroutine(DestroyTrapAfterDelay(2f));
}

private IEnumerator DestroyTrapAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    NetworkServer.Destroy(gameObject);
}

private void OnDestroy()
{
    if (destroyCoroutine != null)
    {
        StopCoroutine(destroyCoroutine);
    }
}
```

---

### H3: **TrapBase RpcOnArmed Only Logs - No Visual Feedback**
**Location:** `Assets/Scripts/Traps/TrapBase.cs:40-45`  
**Severity:** 🟠 HIGH PRIORITY  
**Impact:** Players cannot see when traps are armed (poor UX)

**Problem:**
```csharp
[ClientRpc]
protected virtual void RpcOnArmed()
{
    // Visual feedback that trap is armed
    Debug.Log($"{gameObject.name} armed"); // ❌ Only logs, no visual
}
```

**Why High Priority:**
- Players don't know when trap is ready
- Poor user experience
- Should show visual indicator (glow, particle, color change)

**Fix:**
```csharp
[ClientRpc]
protected virtual void RpcOnArmed()
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"{gameObject.name} armed");
    #endif
    
    // Visual feedback
    // Option 1: Change material color
    var renderer = GetComponent<Renderer>();
    if (renderer != null)
    {
        renderer.material.SetColor("_EmissionColor", Color.green * 0.5f);
    }
    
    // Option 2: Enable particle effect
    var particles = GetComponentInChildren<ParticleSystem>();
    if (particles != null)
    {
        particles.Play();
    }
    
    // Option 3: Play sound
    var audioSource = GetComponent<AudioSource>();
    if (audioSource != null)
    {
        audioSource.Play();
    }
}
```

---

### H4: **DartTurret OnDrawGizmos Not Conditionally Compiled**
**Location:** `Assets/Scripts/Traps/DartTurret.cs:111-115`  
**Severity:** 🟠 HIGH PRIORITY  
**Impact:** Potential Gizmos visible in play mode (same issue as SimpleBuildMode)

**Problem:**
```csharp
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRange);
}
```

**Why High Priority:**
- Same issue as SimpleBuildMode Gizmos
- Should be editor-only
- Already fixed in SimpleBuildMode

**Fix:**
```csharp
#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (!UnityEditor.EditorApplication.isPlaying) return;
    
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRange);
}
#endif
```

---

## 🟡 MEDIUM PRIORITY ISSUES

### M1: **TrapBase Initialize Called Multiple Times Risk**
**Location:** `Assets/Scripts/Traps/TrapBase.cs:24-31`  
**Severity:** 🟡 MEDIUM  
**Impact:** Potential double-initialization if called incorrectly

**Problem:**
```csharp
public virtual void Initialize(Team team)
{
    ownerTeam = team;
    if (isServer)
    {
        Invoke(nameof(Arm), armingDelay); // ❌ No check if already initialized
    }
}
```

**Why Medium Priority:**
- If Initialize called twice, Invoke called twice
- Can cause trap to arm twice
- Should add initialization guard

**Fix:**
```csharp
private bool isInitialized = false;

public virtual void Initialize(Team team)
{
    if (isInitialized)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"Trap {gameObject.name} already initialized!");
        #endif
        return;
    }
    
    ownerTeam = team;
    isInitialized = true;
    
    if (isServer)
    {
        Invoke(nameof(Arm), armingDelay);
    }
}
```

---

### M2: **DartTurret Raycast Can Miss - No Validation**
**Location:** `Assets/Scripts/Traps/DartTurret.cs:87`  
**Severity:** 🟡 MEDIUM  
**Impact:** Dart can hit wrong target if obstacle between turret and target

**Problem:**
```csharp
[Server]
private void FireDart(GameObject target)
{
    Vector3 direction = (target.transform.position - transform.position).normalized;
    
    // Simple hitscan
    if (Physics.Raycast(transform.position, direction, out RaycastHit hit, detectionRange))
    {
        // ❌ Hits first thing in path, not necessarily the target
        if (hit.collider.TryGetComponent<Combat.Health>(out var health))
        {
            health.TakeDamage(dartDamage);
        }
    }
}
```

**Why Medium Priority:**
- Raycast can hit wall instead of target
- Should validate that hit is the intended target
- Or use projectile instead of hitscan

**Fix:**
```csharp
[Server]
private void FireDart(GameObject target)
{
    Vector3 direction = (target.transform.position - transform.position).normalized;
    
    if (Physics.Raycast(transform.position, direction, out RaycastHit hit, detectionRange))
    {
        // ✅ Validate hit is the target
        if (hit.collider.gameObject == target)
        {
            if (hit.collider.TryGetComponent<Combat.Health>(out var health))
            {
                health.TakeDamage(dartDamage);
            }
        }
        else
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"DartTurret: Hit {hit.collider.name} instead of target {target.name}");
            #endif
            // Hit obstacle, don't damage
        }
    }
}
```

---

### M3: **Springboard ResetTrap Not Networked**
**Location:** `Assets/Scripts/Traps/Springboard.cs:36-40`  
**Severity:** 🟡 MEDIUM  
**Impact:** isTriggered state may desync if reset happens on server only

**Problem:**
```csharp
[Server]
private void ResetTrap()
{
    isTriggered = false; // ❌ SyncVar but no RPC to notify clients of reset
}
```

**Why Medium Priority:**
- `isTriggered` is SyncVar, but reset happens via Invoke
- Clients may not see reset immediately
- Should add RPC for visual feedback

**Fix:**
```csharp
[Server]
private void ResetTrap()
{
    isTriggered = false;
    RpcOnTrapReset(); // Notify clients
}

[ClientRpc]
private void RpcOnTrapReset()
{
    // Visual feedback (particles, animation, etc.)
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("Springboard reset");
    #endif
}
```

---

### M4: **Trap Triggering Not Rate-Limited**
**Location:** `Assets/Scripts/Traps/TrapBase.cs:47-57`  
**Severity:** 🟡 MEDIUM  
**Impact:** Rapid trigger spam possible if player moves in/out of trigger

**Problem:**
```csharp
protected virtual void OnTriggerEnter(Collider other)
{
    if (!isServer || !isArmed || isTriggered) return;
    // ❌ No cooldown - player can trigger multiple times if moving fast
}
```

**Why Medium Priority:**
- Player can trigger trap multiple times if moving in/out quickly
- Should add short cooldown after trigger

**Fix:**
```csharp
private float lastTriggerTime = 0f;
private const float TRIGGER_COOLDOWN = 0.5f;

protected virtual void OnTriggerEnter(Collider other)
{
    if (!isServer || !isArmed || isTriggered) return;
    
    // ✅ Rate limit triggering
    if (Time.time - lastTriggerTime < TRIGGER_COOLDOWN) return;
    
    if (other.TryGetComponent<Player.PlayerController>(out var player) && player.team != ownerTeam)
    {
        lastTriggerTime = Time.time;
        Trigger(player.gameObject);
    }
}
```

---

### M5: **Trap Placement Validation Already Covered by BuildValidator**
**Location:** `Assets/Scripts/Building/BuildValidator.cs`  
**Severity:** 🟡 MEDIUM  
**Status:** ✅ **ALREADY FIXED**  
**Impact:** Traps are validated via BuildValidator (good)

**Note:** Trap placement is handled by BuildValidator, which already validates:
- Phase check
- Budget check
- Terrain validation
- Overlap check
- Player overlap check
- Enemy base proximity check

No additional fixes needed for trap placement validation.

---

## 📊 PERFORMANCE ANALYSIS

### Performance Issues Found:
1. ✅ **FIXED:** GetComponent → TryGetComponent (SpikeTrap, DartTurret, TrapBase)
2. ❌ **NEEDS FIX:** GetComponent in Springboard, GlueTrap (Critical)
3. ❌ **NEEDS FIX:** Debug.Log allocations (High Priority)
4. ✅ **GOOD:** OverlapSphereNonAlloc used in DartTurret
5. ✅ **GOOD:** Throttled scanning in DartTurret (200ms interval)

### GC Allocation Hotspots:
- **TrapBase.OnTriggerEnter:** ✅ Already optimized (TryGetComponent)
- **DartTurret.ScanForTargets:** ✅ Already optimized (OverlapSphereNonAlloc)
- **Springboard.Trigger:** ❌ GetComponent calls (3 instances)
- **GlueTrap.SlowEffect:** ❌ GetComponent call

---

## 🔒 SECURITY & ANTI-CHEAT ANALYSIS

### Security Issues Found:
1. ✅ **GOOD:** Trap triggering server-only (TrapBase.OnTriggerEnter)
2. ✅ **GOOD:** Damage application server-only (SpikeTrap, DartTurret)
3. ✅ **GOOD:** Team validation (enemy team check)
4. ⚠️ **MEDIUM:** No rate limiting on trigger (can spam if moving fast)

### Anti-Cheat Status:
- ✅ Server-authoritative trap triggering
- ✅ Server-authoritative damage
- ⚠️ Missing trigger cooldown (medium priority)

---

## 🎮 NETWORK SYNC ANALYSIS

### SyncVar Usage:
- ✅ `ownerTeam` - Synced correctly
- ✅ `isArmed` - Synced correctly
- ✅ `isTriggered` - Synced correctly

### RPC Usage:
- ✅ `RpcOnArmed()` - Synced correctly
- ✅ `RpcPlayTriggerEffect()` - Synced correctly
- ✅ `RpcPlayFireEffect()` - Synced correctly
- ⚠️ `RpcLaunchPlayer()` - Implemented but not functional (Critical)

### Network Issues:
- ⚠️ Springboard launch not networked properly (Critical)
- ⚠️ SlowEffect not networked (Critical - effect doesn't work)

---

## 📝 DEAD/DUPLICATE CODE

### Dead Code Found:
- None

### Duplicate Code:
- Debug.Log patterns repeated - should be utility method

---

## 🧪 MANUAL TEST PLAN

### Test Scenario 1: SpikeTrap
1. Place SpikeTrap in Build phase
2. Wait for arming (2 seconds)
3. Walk enemy player over trap
4. **Expected:** Player takes damage, trap triggers VFX, trap destroyed after 2s
5. **Verify:** Server logs show damage, client sees VFX

### Test Scenario 2: GlueTrap
1. Place GlueTrap in Build phase
2. Wait for arming
3. Walk enemy player over trap
4. **Expected:** Player movement speed reduced to 40%
5. **Verify:** Player moves slower for 3 seconds, then speed restored
6. **CURRENT:** ❌ Player speed NOT reduced (Critical Bug)

### Test Scenario 3: Springboard
1. Place Springboard in Build phase
2. Wait for arming
3. Walk enemy player over trap
4. **Expected:** Player launched upward and forward
5. **Verify:** Player flies in air, trap resets after 2s
6. **CURRENT:** ❌ Player NOT launched (Critical Bug)

### Test Scenario 4: DartTurret
1. Place DartTurret in Build phase
2. Wait for arming
3. Enemy player enters detection range (8m)
4. **Expected:** Turret fires dart, player takes damage
5. **Verify:** Raycast hits player, damage applied, VFX plays

### Test Scenario 5: Trap Spam
1. Rapidly place multiple traps
2. **Expected:** Rate limiting prevents spam (4 per second)
3. **Verify:** Server rejects excess placements

### Test Scenario 6: Network Sync
1. Host places trap
2. Client joins
3. **Expected:** Client sees trap in correct state (armed/unarmed)
4. **Verify:** SyncVar values match on both clients

---

## ✅ FIX SUGGESTIONS SUMMARY

### Critical Fixes (Must Fix):
1. **SlowEffect broken** - Implement speed multiplier system in FPSController
2. **Springboard launch broken** - Implement ApplyImpulse in FPSController
3. **GetComponent calls** - Replace with TryGetComponent in Springboard, GlueTrap

### High Priority Fixes:
1. **Debug.Log allocations** - Wrap with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
2. **Invoke cleanup** - Use coroutines instead of Invoke, cancel on Destroy
3. **Visual feedback** - Add proper visual indicators for trap arming
4. **Gizmos conditional** - Wrap OnDrawGizmos with `#if UNITY_EDITOR`

### Medium Priority Fixes:
1. **Initialization guard** - Prevent double-initialization
2. **DartTurret validation** - Validate raycast hit is target
3. **Trap reset sync** - Add RPC for trap reset
4. **Trigger cooldown** - Add rate limiting to prevent spam

---

## 📋 TO-DO CHECKLIST

### Critical:
- [ ] Fix SlowEffect - implement speed multiplier in FPSController
- [ ] Fix Springboard launch - implement ApplyImpulse in FPSController
- [ ] Replace GetComponent with TryGetComponent in Springboard, GlueTrap

### High Priority:
- [ ] Wrap all Debug.Log calls with conditional compilation
- [ ] Replace Invoke with coroutines in SpikeTrap, GlueTrap
- [ ] Add visual feedback for trap arming (RpcOnArmed)
- [ ] Wrap OnDrawGizmos with `#if UNITY_EDITOR` in DartTurret

### Medium Priority:
- [ ] Add initialization guard to TrapBase.Initialize
- [ ] Validate DartTurret raycast hit is target
- [ ] Add RPC for Springboard reset
- [ ] Add trigger cooldown to TrapBase.OnTriggerEnter

---

**END OF AUDIT REPORT**

