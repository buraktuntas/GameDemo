# 🏗️ BUILDING SYSTEM COMPREHENSIVE AUDIT REPORT

**Date:** 2024  
**Scope:** Building/Base Construction/Trap Placement System  
**Game Type:** Competitive 4v4 PvP Survival-Arena (Valheim-style building + real-time combat)

---

## 📋 EXECUTIVE SUMMARY

**Critical Issues Found:** 8  
**High Priority Issues:** 12  
**Medium Priority Issues:** 9  
**Performance Risks:** 11  
**Security/Exploit Vectors:** 7

**Overall Status:** ⚠️ **REQUIRES IMMEDIATE FIXES** - Multiple critical server authority bypasses, missing validations, and performance issues.

---

## 1. 🚨 CRITICAL GAMEPLAY BUGS

### 1.1 **DUAL BUILDING PATHS - INCONSISTENT VALIDATION**
**Location:** `SimpleBuildMode.cs:780-784`, `BuildValidator.cs:44-83`  
**Severity:** CRITICAL  
**Issue:** Two separate building systems exist:
- `SimpleBuildMode.CmdPlaceStructure()` - Direct spawn, **NO budget validation**
- `BuildPlacementController.CmdRequestPlace()` → `BuildValidator.ValidateAndPlace()` - Proper validation

**Impact:** Players can bypass budget system by using `SimpleBuildMode` directly.  
**Evidence:**
```csharp
// SimpleBuildMode.cs:780-784 - NO budget check!
GameObject structure = Instantiate(selectedStructure, position, rotation);
NetworkServer.Spawn(structure);
```

**Fix:**
```csharp
// SimpleBuildMode.cs:780 - Replace with BuildValidator call
if (BuildValidator.Instance != null)
{
    BuildRequest request = new BuildRequest(position, rotation, GetStructureType(structureIndex), netId);
    if (!BuildValidator.Instance.ValidateAndPlace(request, GetComponent<PlayerController>().team))
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"🚨 [SimpleBuildMode] Placement rejected by validator");
        #endif
        return;
    }
}
// Remove direct Instantiate + Spawn
```

---

### 1.2 **BUDGET SPENT BEFORE VALIDATION PASSES**
**Location:** `BuildValidator.cs:66-78`  
**Severity:** CRITICAL  
**Issue:** Overlap check happens BEFORE budget check, but budget is spent AFTER all validations. However, if budget check fails, budget is already spent (wrong order).

**Current Flow:**
1. Overlap check (line 66)
2. Budget spend (line 74) ← **WRONG: Should be last**
3. Spawn (line 81)

**Impact:** If spawn fails after budget spend, budget is lost.  
**Fix:** Move budget spend to AFTER all validation passes, immediately before `SpawnStructure()`.

```csharp
// BuildValidator.cs - Correct order:
// 1. Phase check
// 2. Player state check
// 3. Overlap check
// 4. Distance check (if needed)
// 5. Budget check (don't spend yet)
// 6. Spawn structure
// 7. Spend budget (LAST - only if spawn succeeds)
```

---

### 1.3 **NO TERRAIN ANCHOR VALIDATION**
**Location:** `BuildValidator.cs:44-83`, `SimpleBuildMode.cs:747-752`  
**Severity:** CRITICAL  
**Issue:** Server validates ground with raycast, but:
- No minimum ground distance check
- No maximum height limit (floating bases exploit)
- No terrain slope validation
- No "anchor point" requirement

**Impact:** Players can build floating platforms, infinite-height towers, on steep slopes.

**Fix:**
```csharp
// BuildValidator.cs - Add to ValidateAndPlace()
// Height limit check
if (request.position.y > MAX_BUILD_HEIGHT)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Build height limit exceeded: {request.position.y}m");
    #endif
    return false;
}

// Ground anchor validation
RaycastHit groundHit;
if (!Physics.Raycast(request.position + Vector3.up * 0.5f, Vector3.down, out groundHit, 5f, groundLayer))
{
    return false; // No ground below
}

// Slope validation
float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
if (slopeAngle > MAX_SLOPE_ANGLE) // e.g., 45°
{
    return false;
}

// Ground distance check (prevent floating)
float groundDistance = Vector3.Distance(request.position, groundHit.point);
if (groundDistance > MAX_GROUND_DISTANCE) // e.g., 0.5m
{
    return false;
}
```

---

### 1.4 **STRUCTURE INITIALIZE NOT CALLED ON SERVER**
**Location:** `BuildValidator.cs:108-112`, `SimpleBuildMode.cs:781-782`  
**Severity:** CRITICAL  
**Issue:** `SimpleBuildMode.CmdPlaceStructure()` spawns structure but **never calls `Initialize()`**.  
**Impact:** Structure has no team, ownerId, or category. SyncVar defaults may cause desync.

**Evidence:**
```csharp
// SimpleBuildMode.cs:781-782
GameObject structure = Instantiate(selectedStructure, position, rotation);
NetworkServer.Spawn(structure);
// ❌ NO Initialize() call!
```

**Fix:**
```csharp
// SimpleBuildMode.cs:781 - After spawn
GameObject structure = Instantiate(selectedStructure, position, rotation);
NetworkServer.Spawn(structure);

Structure structureComp = structure.GetComponent<Structure>();
if (structureComp != null)
{
    StructureType type = GetStructureTypeFromIndex(structureIndex);
    structureComp.Initialize(
        GetComponent<PlayerController>().team,
        type,
        Structure.GetStructureCategory(type),
        netId
    );
}
```

---

### 1.5 **NO COMBAT LOCKOUT CHECK**
**Location:** `SimpleBuildMode.cs:700`, `BuildPlacementController.cs:198`  
**Severity:** CRITICAL  
**Issue:** Players can build while being shot/damaged. No validation to prevent building during combat.

**Impact:** Exploit where players spam build during combat to block bullets.

**Fix:**
```csharp
// SimpleBuildMode.cs:700 - Add to CmdPlaceStructure()
var health = GetComponent<Combat.Health>();
if (health != null && health.IsInCombat()) // Need to add IsInCombat() method
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 [SimpleBuildMode] Cannot build during combat");
    #endif
    return;
}

// OR: Check if took damage in last X seconds
if (health != null && Time.time - health.lastDamageTime < COMBAT_LOCKOUT_DURATION)
{
    return;
}
```

---

### 1.6 **NO ENEMY BASE PLACEMENT CHECK**
**Location:** `BuildValidator.cs:44-83`  
**Severity:** CRITICAL  
**Issue:** No validation to prevent building inside enemy base/core area.

**Impact:** Players can build traps/structures inside enemy spawn/core, breaking game balance.

**Fix:**
```csharp
// BuildValidator.cs - Add to ValidateAndPlace()
// Check distance to enemy core
var coreStructures = FindObjectsByType<CoreStructure>(FindObjectsSortMode.None);
foreach (var core in coreStructures)
{
    if (core.team != team) // Enemy core
    {
        float distanceToEnemyCore = Vector3.Distance(request.position, core.transform.position);
        if (distanceToEnemyCore < ENEMY_CORE_BUILD_RADIUS) // e.g., 15m
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"🚨 Cannot build within {ENEMY_CORE_BUILD_RADIUS}m of enemy core");
            #endif
            return false;
        }
    }
}
```

---

### 1.7 **TRAP INITIALIZE NOT CALLED ON SPAWN**
**Location:** `BuildValidator.cs:86-113`, `TrapBase.cs:24-31`  
**Severity:** CRITICAL  
**Issue:** Traps are spawned but `Initialize()` is never called. Traps won't arm.

**Impact:** Traps don't work - they never arm because `Initialize()` sets `armingTime` and calls `Invoke(nameof(Arm), armingDelay)`.

**Fix:**
```csharp
// BuildValidator.cs:108-112 - After structure spawn
Structure structure = structureObj.GetComponent<Structure>();
if (structure != null)
{
    structure.Initialize(team, request.type, Structure.GetStructureCategory(request.type), request.playerId);
}

// ✅ ADD: Initialize traps
TrapBase trap = structureObj.GetComponent<TrapBase>();
if (trap != null)
{
    trap.Initialize(team);
}
```

---

### 1.8 **SPIKE TRAP GETCOMPONENT GC ALLOCATION**
**Location:** `SpikeTrap.cs:21`  
**Severity:** CRITICAL (Performance)  
**Issue:** `GetComponent<Combat.Health>()` allocates GC on every trigger.

**Fix:**
```csharp
// SpikeTrap.cs:21 - Replace with TryGetComponent
if (target.TryGetComponent<Combat.Health>(out var health) && !health.IsDead())
{
    health.TakeDamage(damage);
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"Spike trap dealt {damage} damage to {target.name}");
    #endif
}
```

---

## 2. ⚠️ DESYNC RISKS

### 2.1 **GHOST PREVIEW NOT SYNCED (CORRECT)**
**Location:** `SimpleBuildMode.cs:467-519`, `BuildPlacementController.cs:229-255`  
**Status:** ✅ **CORRECT** - Ghost is local-only, not networked.  
**Note:** This is correct behavior. Ghost should not be synced.

---

### 2.2 **STRUCTURE SYNCVAR HOOKS MISSING**
**Location:** `Structure.cs:13-23`  
**Severity:** MEDIUM  
**Issue:** `SyncVar` fields (`team`, `structureType`, `category`, `ownerId`) have no hooks. Clients won't update visuals when these change.

**Impact:** Visual desync - team colors may not update on clients.

**Fix:**
```csharp
// Structure.cs - Add hooks
[SyncVar(hook = nameof(OnTeamChanged))]
public Team team;

private void OnTeamChanged(Team oldTeam, Team newTeam)
{
    UpdateVisuals();
}
```

---

### 2.3 **STRUCTURAL INTEGRITY SYNCVAR HOOK DOUBLE-FIRE RISK**
**Location:** `StructuralIntegrity.cs:23-24`  
**Severity:** LOW  
**Status:** ✅ **FIXED** - Hook exists, but `material` usage is wrong (see Performance section).

---

### 2.4 **NO CLIENT-SIDE PLACEMENT FEEDBACK RPC**
**Location:** `SimpleBuildMode.cs:700`, `BuildValidator.cs:86`  
**Severity:** MEDIUM  
**Issue:** When server rejects placement, client receives no feedback. Ghost stays red but player doesn't know why.

**Fix:**
```csharp
// SimpleBuildMode.cs:700 - Add RPC rejection
[ClientRpc]
private void RpcPlacementRejected(string reason)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Placement rejected: {reason}");
    #endif
    // Could show UI message
}

// Call in CmdPlaceStructure when validation fails
RpcPlacementRejected("Invalid placement distance");
```

---

## 3. 🔒 SECURITY / EXPLOIT CONCERNS

### 3.1 **PLACEMENT DISTANCE BYPASS**
**Location:** `SimpleBuildMode.cs:726-731`  
**Severity:** HIGH  
**Issue:** Client sends position, server validates distance. But if client hacks position calculation, they can place structures far away.

**Current:** `distance > placementDistance + 0.5f` check exists, but 0.5m buffer may be too lenient.

**Fix:** Add server-side raycast to verify player actually has LOS to placement position.

```csharp
// SimpleBuildMode.cs:726 - After distance check
// Verify LOS (prevent teleport placement exploit)
Vector3 playerEye = transform.position + Vector3.up * 1.6f;
if (Physics.Linecast(playerEye, position, out RaycastHit losHit, obstacleLayer))
{
    float losDistance = Vector3.Distance(losHit.point, position);
    if (losDistance > 0.3f) // Allow small tolerance
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning($"🚨 [SimpleBuildMode] LOS violation: {losDistance}m");
        #endif
        return;
    }
}
```

---

### 3.2 **RAPID PLACEMENT SPAM (PARTIALLY FIXED)**
**Location:** `SimpleBuildMode.cs:702-723`  
**Status:** ✅ **FIXED** - Rate limiting exists (4/second, 250ms cooldown).  
**Note:** Good implementation.

---

### 3.3 **FREE BUILD EXPLOIT (BUDGET BYPASS)**
**Location:** `SimpleBuildMode.cs:780-784`  
**Severity:** CRITICAL  
**Issue:** `SimpleBuildMode.CmdPlaceStructure()` doesn't use `BuildValidator`, so budget is never checked.

**Impact:** Players can build unlimited structures for free.

**Fix:** See section 1.1 - Use `BuildValidator` for all placements.

---

### 3.4 **FLOATING BASE EXPLOIT**
**Location:** `BuildValidator.cs:44-83`  
**Severity:** HIGH  
**Issue:** No height limit or ground anchor validation.

**Impact:** Players build floating platforms in sky.

**Fix:** See section 1.3 - Add terrain anchor validation.

---

### 3.5 **PHASE-THROUGH WALLS EXPLOIT**
**Location:** `SimpleBuildMode.cs:647-674`  
**Severity:** MEDIUM  
**Issue:** Client-side `IsValidPlacement()` checks overlap, but server validation may use different box size.

**Impact:** Client shows valid, server rejects (or vice versa) = desync.

**Fix:** Ensure server and client use **identical** validation logic and box sizes.

```csharp
// SimpleBuildMode.cs:755 - Match client box size exactly
Vector3 boxSize = new Vector3(1.8f, 0.9f, 0.18f); // Must match line 653
```

---

### 3.6 **TRAP PLACEMENT UNDER PLAYERS**
**Location:** `BuildValidator.cs:66`  
**Severity:** MEDIUM  
**Issue:** Overlap check uses `OverlapSphere`, but doesn't check for players inside placement area.

**Impact:** Players can place traps directly under enemies, causing instant damage.

**Fix:**
```csharp
// BuildValidator.cs:66 - Add player overlap check
Collider[] overlaps = Physics.OverlapSphere(request.position, minDistanceBetweenStructures, obstacleMask);
if (overlaps.Length > 0)
{
    // Check if overlap is a player (prevent trap-under-player exploit)
    foreach (var overlap in overlaps)
    {
        if (overlap.TryGetComponent<PlayerController>(out var player))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"🚨 Cannot place structure on player");
            #endif
            return false;
        }
    }
    return false;
}
```

---

### 3.7 **INSTANT BUILD MACRO SPAM**
**Location:** `SimpleBuildMode.cs:700-723`  
**Status:** ✅ **FIXED** - Rate limiting prevents this.

---

## 4. ❌ INCORRECT SERVER/CLIENT RESPONSIBILITIES

### 4.1 **CLIENT-SIDE PLACEMENT VALIDATION DUPLICATE**
**Location:** `SimpleBuildMode.cs:647-674`, `BuildPlacementController.cs:185-196`  
**Severity:** MEDIUM  
**Issue:** Client validates placement for ghost preview, but server also validates. This is **correct**, but they must match exactly.

**Recommendation:** Extract validation logic to shared static method to ensure consistency.

```csharp
// Shared validation utility
public static class BuildValidation
{
    public static bool IsValidPlacement(Vector3 position, Quaternion rotation, LayerMask obstacleLayer, float placementDistance, Transform player)
    {
        // Shared logic used by both client and server
    }
}
```

---

### 4.2 **GHOST PREVIEW ON NON-LOCAL PLAYERS**
**Location:** `SimpleBuildMode.cs:258-279`  
**Status:** ✅ **CORRECT** - `if (!isLocalPlayer) return;` prevents ghost on remote players.

---

### 4.3 **SERVER SPAWNS STRUCTURE (CORRECT)**
**Location:** `BuildValidator.cs:86-113`, `SimpleBuildMode.cs:781-782`  
**Status:** ✅ **CORRECT** - `NetworkServer.Spawn()` called on server only.

---

## 5. 🐛 PLACEMENT / COLLISION BUGS

### 5.1 **OVERLAP CHECK BOX SIZE MISMATCH**
**Location:** `SimpleBuildMode.cs:653 vs 757`  
**Severity:** MEDIUM  
**Issue:** Client uses `new Vector3(1.8f, 0.9f, 0.18f)`, server uses `new Vector3(0.9f, 0.45f, 0.09f)` (half size).

**Impact:** Client shows valid, server rejects (or vice versa).

**Fix:**
```csharp
// SimpleBuildMode.cs:755 - Match client size
Vector3 boxSize = new Vector3(1.8f, 0.9f, 0.18f); // Must match line 653
int overlapCount = Physics.OverlapBoxNonAlloc(
    position,
    boxSize / 2f, // Half extents
    overlapBoxBuffer,
    rotation,
    obstacleLayer,
    QueryTriggerInteraction.Ignore
);
```

---

### 5.2 **NO SNAP POINT VALIDATION**
**Location:** `SimpleBuildMode.cs:640-645`  
**Severity:** LOW  
**Issue:** Client snaps to grid, but server doesn't verify snap alignment.

**Impact:** Minor desync if client sends non-snapped position.

**Fix:**
```csharp
// SimpleBuildMode.cs:700 - Add snap validation
Vector3 snappedPosition = SnapToGrid(position);
if (Vector3.Distance(position, snappedPosition) > 0.1f)
{
    // Client sent non-snapped position, use server's snap
    position = snappedPosition;
}
```

---

### 5.3 **ROTATION NOT VALIDATED**
**Location:** `SimpleBuildMode.cs:700`  
**Severity:** LOW  
**Issue:** Server accepts any rotation. No validation for valid rotation angles (e.g., 90° increments).

**Fix:** (Optional - only if game requires grid-aligned rotations)

---

### 5.4 **GROUND CHECK TOO LENIENT**
**Location:** `SimpleBuildMode.cs:748`  
**Severity:** MEDIUM  
**Issue:** `Physics.Raycast(position, Vector3.down, 2f, groundLayer)` - 2m is too large. Allows floating structures.

**Fix:** Reduce to 0.5m max distance.

```csharp
// SimpleBuildMode.cs:748
if (!Physics.Raycast(position, Vector3.down, 0.5f, groundLayer))
{
    Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Not on ground (distance > 0.5m)");
    return;
}
```

---

## 6. 💥 DESTRUCTION SYNC ISSUES

### 6.1 **DESTRUCTION EFFECTS NOT POOLED**
**Location:** `Structure.cs:143`  
**Severity:** MEDIUM  
**Issue:** `Instantiate(destructionEffect)` creates new GameObject every time. No pooling.

**Impact:** GC spikes on structure destruction.

**Fix:** Use object pool for destruction effects.

```csharp
// Structure.cs:143
if (destructionEffect != null)
{
    GameObject effect = EffectPool.Instance?.Get(destructionEffect) ?? Instantiate(destructionEffect);
    effect.transform.position = transform.position;
    effect.transform.rotation = Quaternion.identity;
    // Return to pool after delay
    StartCoroutine(ReturnEffectToPool(effect, 3f));
}
```

---

### 6.2 **RPC PLAYDESTRUCTIONEFFECTS CALLED BEFORE DESTROY**
**Location:** `Structure.cs:111`  
**Status:** ✅ **CORRECT** - RPC called, then delayed destroy. Good.

---

### 6.3 **STRUCTURAL INTEGRITY COLLAPSE NOT SYNCED**
**Location:** `StructuralIntegrity.cs:180-192`  
**Severity:** MEDIUM  
**Issue:** `CollapseStructure()` destroys structure but doesn't call `RpcPlayDestructionEffects()`.

**Impact:** Structure disappears without VFX/SFX on clients.

**Fix:**
```csharp
// StructuralIntegrity.cs:180
[Server]
private void CollapseStructure()
{
    // Get Structure component to play destruction effects
    Structure structure = GetComponent<Structure>();
    if (structure != null)
    {
        structure.RpcPlayDestructionEffects(); // Need to make public or add public method
    }
    
    // Then destroy
    NetworkServer.Destroy(gameObject);
}
```

---

## 7. ⚡ PERFORMANCE RISKS

### 7.1 **BUILDVALIDATOR OVERLAPSPHERE GC ALLOCATION**
**Location:** `BuildValidator.cs:66`  
**Severity:** HIGH  
**Issue:** `Physics.OverlapSphere()` allocates GC array.

**Fix:**
```csharp
// BuildValidator.cs:66 - Use NonAlloc
private static readonly Collider[] overlapBuffer = new Collider[64];

Collider[] overlaps = Physics.OverlapSphereNonAlloc(
    request.position,
    minDistanceBetweenStructures,
    overlapBuffer,
    obstacleMask
);
if (overlaps.Length > 0) // Check first element, not buffer length
{
    // Check if any overlap is significant
    for (int i = 0; i < overlaps.Length && i < overlapBuffer.Length; i++)
    {
        if (overlaps[i] != null)
        {
            return false;
        }
    }
}
```

---

### 7.2 **GETCOMPONENT CALLS IN HOT PATHS**
**Location:** Multiple files  
**Severity:** MEDIUM  
**Issues:**
- `SimpleBuildMode.cs:663` - `GetComponent<Structure>()` in overlap check
- `SimpleBuildMode.cs:666` - `GetComponent<NetworkIdentity>()` in overlap check
- `SimpleBuildMode.cs:668` - `GetComponent<PlayerController>()` in overlap check
- `SpikeTrap.cs:21` - `GetComponent<Combat.Health>()`

**Fix:** Replace all with `TryGetComponent`.

---

### 7.3 **STRUCTURAL INTEGRITY MATERIAL LEAK**
**Location:** `StructuralIntegrity.cs:208`  
**Severity:** HIGH  
**Issue:** `structureRenderer.material` creates new material instance every frame.

**Fix:**
```csharp
// StructuralIntegrity.cs:208 - Use sharedMaterial
if (structureRenderer.sharedMaterial != null)
{
    // Create material instance ONCE, cache it
    if (stabilityMaterialInstance == null)
    {
        stabilityMaterialInstance = new Material(structureRenderer.sharedMaterial);
        structureRenderer.material = stabilityMaterialInstance; // Set once
    }
    stabilityMaterialInstance.color = stabilityColor; // Update color only
}
```

---

### 7.4 **BUILDGHOST MATERIAL LEAK**
**Location:** `BuildGhost.cs:32`  
**Severity:** HIGH  
**Issue:** `rend.material = mat` creates new material instance.

**Fix:**
```csharp
// BuildGhost.cs:32 - Use sharedMaterial or cache instance
if (rend.sharedMaterial != mat)
{
    rend.sharedMaterial = mat; // Or create instance once and reuse
}
```

---

### 7.5 **DEBUG.LOG IN HOT PATHS**
**Location:** Multiple files (57 instances)  
**Severity:** MEDIUM  
**Issue:** `Debug.Log` calls in Update loops and validation paths.

**Fix:** Wrap with `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

---

### 7.6 **STRUCTURAL INTEGRITY UPDATE NEIGHBORS RECURSIVE**
**Location:** `StructuralIntegrity.cs:162-177`  
**Severity:** MEDIUM  
**Issue:** `UpdateNeighborStabilities()` calls `Invoke(nameof(CalculateStability), 0.1f)` on neighbors, which can cause chain reaction and performance spike.

**Fix:** Add cooldown or limit recursion depth.

```csharp
// StructuralIntegrity.cs:162
private float lastNeighborUpdateTime = 0f;
private const float NEIGHBOR_UPDATE_COOLDOWN = 0.5f;

[Server]
private void UpdateNeighborStabilities()
{
    if (Time.time - lastNeighborUpdateTime < NEIGHBOR_UPDATE_COOLDOWN) return;
    lastNeighborUpdateTime = Time.time;
    
    // Limit neighbor updates to prevent cascade
    int updateCount = 0;
    foreach (var other in allStructures)
    {
        if (updateCount >= MAX_NEIGHBOR_UPDATES) break; // Limit
        // ... rest of code
    }
}
```

---

### 7.7 **FINDNEARESTSUPPORT O(N²) COMPLEXITY**
**Location:** `StructuralIntegrity.cs:122-159`  
**Severity:** MEDIUM  
**Issue:** `FindNearestSupport()` iterates through all structures. Called on every stability calculation.

**Fix:** Use spatial partitioning (e.g., `Physics.OverlapSphereNonAlloc` with cached results).

---

### 7.8 **GHOST PREVIEW UPDATE EVERY FRAME**
**Location:** `SimpleBuildMode.cs:548-591`  
**Status:** ✅ **FIXED** - Throttled to 0.05s interval (20 FPS max).

---

### 7.9 **STRUCTURE VISUAL UPDATE ON SYNCVAR CHANGE**
**Location:** `Structure.cs:70-87`  
**Status:** ✅ **CORRECT** - `UpdateVisuals()` called on `Initialize()` and should be called on SyncVar hooks (missing - see section 2.2).

---

### 7.10 **NO OBJECT POOLING FOR STRUCTURES**
**Location:** `BuildValidator.cs:103`, `SimpleBuildMode.cs:781`  
**Severity:** MEDIUM  
**Issue:** `Instantiate()` used directly. `NetworkObjectPool` exists but not used in `SimpleBuildMode`.

**Fix:** Use `NetworkObjectPool` consistently.

```csharp
// SimpleBuildMode.cs:781
GameObject structure;
if (NetworkObjectPool.Instance != null)
{
    structure = NetworkObjectPool.Instance.Get(selectedStructure, position, rotation);
}
else
{
    structure = Instantiate(selectedStructure, position, rotation);
    NetworkServer.Spawn(structure);
}
```

---

### 7.11 **ONTRIGGERENTER IN TRAPBASE (PERFORMANCE)**
**Location:** `TrapBase.cs:47`  
**Status:** ✅ **FIXED** - Uses `TryGetComponent`.

---

## 8. 🗑️ MEMORY / GC ISSUES

### 8.1 **BUILDVALIDATOR OVERLAPSPHERE GC**
**Location:** `BuildValidator.cs:66`  
**Fix:** See section 7.1.

---

### 8.2 **STRUCTURAL INTEGRITY MATERIAL LEAK**
**Location:** `StructuralIntegrity.cs:208`  
**Fix:** See section 7.3.

---

### 8.3 **BUILDGHOST MATERIAL LEAK**
**Location:** `BuildGhost.cs:32`  
**Fix:** See section 7.4.

---

### 8.4 **GETCOMPONENT GC ALLOCATIONS**
**Location:** Multiple files  
**Fix:** See section 7.2.

---

### 8.5 **DEBUG.LOG STRING ALLOCATIONS**
**Location:** Multiple files (57 instances)  
**Fix:** See section 7.5.

---

### 8.6 **STRUCTURE DESTRUCTION EFFECT INSTANTIATE**
**Location:** `Structure.cs:143`  
**Fix:** See section 6.1.

---

## 9. 💀 DEAD / DUPLICATE CODE

### 9.1 **DUPLICATE BUILDING SYSTEMS**
**Location:** `SimpleBuildMode.cs` vs `BuildPlacementController.cs`  
**Severity:** HIGH  
**Issue:** Two separate building systems exist. Should be consolidated.

**Recommendation:** Deprecate `SimpleBuildMode` or merge into `BuildPlacementController`.

---

### 9.2 **UNUSED STRUCTURE COST FIELDS**
**Location:** `SimpleBuildMode.cs:48-54`  
**Severity:** LOW  
**Issue:** Structure costs defined in `SimpleBuildMode` but not used (costs come from `Structure.GetStructureCost()`).

**Recommendation:** Remove or use for client-side preview.

---

### 9.3 **UNUSED CANAFFORDSTRUCTURE METHOD**
**Location:** `SimpleBuildMode.cs:942-947`  
**Severity:** LOW  
**Issue:** Method always returns `true`. TODO comment says "Get player budget from MatchManager".

**Recommendation:** Implement or remove.

---

## 10. 🔧 FIX SUGGESTIONS (CODE DIFFS)

### Fix 1: Consolidate Building Systems
```csharp
// SimpleBuildMode.cs:700 - Replace CmdPlaceStructure with BuildValidator call
[Command]
private void CmdPlaceStructure(Vector3 position, Quaternion rotation, int structureIndex)
{
    // ... existing rate limiting ...
    
    // Convert to BuildRequest
    StructureType type = GetStructureTypeFromIndex(structureIndex);
    BuildRequest request = new BuildRequest(position, rotation, type, netId);
    
    // Use BuildValidator (centralized validation)
    if (BuildValidator.Instance == null)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError("❌ BuildValidator not found!");
        #endif
        return;
    }
    
    PlayerController pc = GetComponent<PlayerController>();
    if (pc == null) return;
    
    if (!BuildValidator.Instance.ValidateAndPlace(request, pc.team))
    {
        RpcPlacementRejected("Validation failed");
        return;
    }
    
    // Success - structure spawned by BuildValidator
}

private StructureType GetStructureTypeFromIndex(int index)
{
    // Map index to StructureType
    // TODO: Implement based on availableStructures array
    return StructureType.Wall; // Placeholder
}

[ClientRpc]
private void RpcPlacementRejected(string reason)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Placement rejected: {reason}");
    #endif
}
```

---

### Fix 2: Add Terrain Anchor Validation
```csharp
// BuildValidator.cs:44 - Add to ValidateAndPlace()
private const float MAX_BUILD_HEIGHT = 50f;
private const float MAX_SLOPE_ANGLE = 45f;
private const float MAX_GROUND_DISTANCE = 0.5f;

// After overlap check, before budget spend:
// Height limit
if (request.position.y > MAX_BUILD_HEIGHT)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Build height limit: {request.position.y}m > {MAX_BUILD_HEIGHT}m");
    #endif
    return false;
}

// Ground anchor validation
RaycastHit groundHit;
if (!Physics.Raycast(request.position + Vector3.up * 0.5f, Vector3.down, out groundHit, 5f, groundLayer))
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning("🚨 No ground below placement position");
    #endif
    return false;
}

// Slope validation
float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
if (slopeAngle > MAX_SLOPE_ANGLE)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Slope too steep: {slopeAngle}° > {MAX_SLOPE_ANGLE}°");
    #endif
    return false;
}

// Ground distance check
float groundDistance = Vector3.Distance(request.position, groundHit.point);
if (groundDistance > MAX_GROUND_DISTANCE)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.LogWarning($"🚨 Too far from ground: {groundDistance}m > {MAX_GROUND_DISTANCE}m");
    #endif
    return false;
}
```

---

### Fix 3: Fix OverlapSphere GC Allocation
```csharp
// BuildValidator.cs - Add static buffer
private static readonly Collider[] overlapBuffer = new Collider[64];

// BuildValidator.cs:66 - Replace
int overlapCount = Physics.OverlapSphereNonAlloc(
    request.position,
    minDistanceBetweenStructures,
    overlapBuffer,
    obstacleMask
);

if (overlapCount > 0)
{
    // Check if any overlap is a player (prevent trap-under-player)
    for (int i = 0; i < overlapCount && i < overlapBuffer.Length; i++)
    {
        if (overlapBuffer[i] == null) continue;
        
        // Prevent placing on players
        if (overlapBuffer[i].TryGetComponent<PlayerController>(out var player))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("🚨 Cannot place structure on player");
            #endif
            return false;
        }
        
        // Check for structures
        if (overlapBuffer[i].GetComponent<Structure>() != null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("Placement overlaps with existing structure");
            #endif
            return false;
        }
    }
}
```

---

### Fix 4: Fix Structure Initialize Call
```csharp
// BuildValidator.cs:108-112 - After spawn
Structure structure = structureObj.GetComponent<Structure>();
if (structure != null)
{
    structure.Initialize(team, request.type, Structure.GetStructureCategory(request.type), request.playerId);
}

// ✅ ADD: Initialize traps
TrapBase trap = structureObj.GetComponent<TrapBase>();
if (trap != null)
{
    trap.Initialize(team);
}
```

---

### Fix 5: Fix Material Leaks
```csharp
// StructuralIntegrity.cs - Add cached material
private Material stabilityMaterialInstance;

private void UpdateVisualFeedback(float stability)
{
    Color stabilityColor = GetStabilityColor(stability);
    
    if (structureRenderer == null) return;
    
    // Create material instance ONCE
    if (stabilityMaterialInstance == null)
    {
        stabilityMaterialInstance = new Material(structureRenderer.sharedMaterial);
        structureRenderer.material = stabilityMaterialInstance;
    }
    
    // Update color only (no new instance)
    stabilityMaterialInstance.color = stabilityColor;
}

// BuildGhost.cs:32 - Use sharedMaterial
private void UpdateVisual()
{
    if (renderers == null || renderers.Length == 0) return;

    Material mat = isValid ? validMaterial : invalidMaterial;
    if (mat != null)
    {
        foreach (var rend in renderers)
        {
            if (rend != null)
            {
                rend.sharedMaterial = mat; // Or cache instance once
            }
        }
    }
}
```

---

## 11. 🧪 TWO-PLAYER MANUAL TEST PLAN

### Test 1: Basic Placement
**Action:** Player 1 places wall  
**Expected:** 
- ✅ Wall appears instantly for Player 2
- ✅ Ghost preview only visible to Player 1 (local)
- ✅ Wall has correct team color on both clients

---

### Test 2: Invalid Placement
**Action:** Player 1 tries to place wall overlapping existing structure  
**Expected:**
- ✅ Ghost shows red on Player 1
- ✅ Server rejects placement
- ✅ No structure spawned
- ✅ Player 1 receives rejection feedback (if RPC added)

---

### Test 3: Destroy Structure
**Action:** Player 1 destroys wall (via combat or manual destroy)  
**Expected:**
- ✅ Destruction VFX plays on both clients
- ✅ Destruction sound plays on both clients
- ✅ Structure disappears on both clients simultaneously

---

### Test 4: Trap Placement
**Action:** Player 1 places spike trap  
**Expected:**
- ✅ Trap appears on both clients
- ✅ Trap arms after 2 seconds (visible feedback)
- ✅ Trap triggers when Player 2 steps on it
- ✅ Damage applies to Player 2
- ✅ Trigger VFX plays on both clients

---

### Test 5: Reconnect
**Action:** Player 2 disconnects, structures placed, Player 2 reconnects  
**Expected:**
- ✅ All existing structures visible to Player 2
- ✅ Structures have correct team colors
- ✅ Structures have correct health values

---

### Test 6: Spam Build
**Action:** Player 1 rapidly clicks build button (macro spam)  
**Expected:**
- ✅ Rate limit blocks spam (max 4/second)
- ✅ Server rejects excess requests
- ✅ No freeze or lag spike

---

### Test 7: Place on Slope
**Action:** Player 1 tries to place wall on steep slope  
**Expected:**
- ✅ Server rejects placement (if slope validation added)
- ✅ Ghost shows invalid on client

---

### Test 8: Place Near Enemy Base
**Action:** Player 1 tries to place wall near enemy core  
**Expected:**
- ✅ Server rejects if within 15m of enemy core (if validation added)
- ✅ Ghost shows invalid on client

---

### Test 9: Build During Combat
**Action:** Player 1 takes damage, immediately tries to build  
**Expected:**
- ✅ Server rejects if in combat lockout (if validation added)
- ✅ Ghost shows invalid on client

---

### Test 10: Floating Base Exploit
**Action:** Player 1 tries to place platform in sky (no ground below)  
**Expected:**
- ✅ Server rejects (if ground validation added)
- ✅ Ghost shows invalid on client

---

## 12. 🔍 EDGE CASE TESTS

### Edge Case 1: Jump-Place Exploit
**Test:** Player jumps and places structure mid-air  
**Expected:** Server validates ground anchor, rejects if no ground below.

---

### Edge Case 2: Trap Spam
**Test:** Player places 10 traps in same location rapidly  
**Expected:** Rate limit prevents spam, overlap check prevents stacking.

---

### Edge Case 3: Anti-Camp Logic
**Test:** Player builds wall to block enemy spawn  
**Expected:** Enemy base placement check prevents this (if implemented).

---

### Edge Case 4: Structure Stacking
**Test:** Player places wall on top of wall repeatedly (infinite tower)  
**Expected:** Height limit prevents infinite stacking (if implemented).

---

### Edge Case 5: Build Across Map
**Test:** Player hacks position to place structure 100m away  
**Expected:** Distance check + LOS validation prevents this.

---

### Edge Case 6: Trap Under Player
**Test:** Player places trap directly under enemy player  
**Expected:** Player overlap check prevents this (if implemented).

---

### Edge Case 7: Build During Fire
**Test:** Player builds while being shot  
**Expected:** Combat lockout prevents this (if implemented).

---

### Edge Case 8: Ghost Leftover
**Test:** Player disconnects while in build mode  
**Expected:** `OnDisable()` destroys ghost preview.

---

### Edge Case 9: Structural Integrity Collapse
**Test:** Remove support structure, causing chain collapse  
**Expected:** Neighbor stability updates trigger, structures collapse in order.

---

### Edge Case 10: Reconnect During Build
**Test:** Player 1 disconnects mid-placement, reconnects  
**Expected:** Ghost resets, no duplicate structures.

---

## 📊 PRIORITY FIX CHECKLIST

### 🔴 CRITICAL (Fix Immediately)
- [ ] 1.1 - Consolidate building systems (use BuildValidator for all placements)
- [ ] 1.2 - Fix budget spend order (spend after all validations pass)
- [ ] 1.3 - Add terrain anchor validation (height limit, ground distance, slope)
- [ ] 1.4 - Call Structure.Initialize() on spawn
- [ ] 1.6 - Add enemy base placement check
- [ ] 1.7 - Call TrapBase.Initialize() on spawn
- [ ] 3.1 - Add LOS validation for placement distance
- [ ] 3.3 - Fix free build exploit (budget bypass)

### 🟠 HIGH PRIORITY (Fix This Week)
- [ ] 1.5 - Add combat lockout check
- [ ] 2.2 - Add SyncVar hooks for Structure visuals
- [ ] 3.6 - Add player overlap check (prevent trap-under-player)
- [ ] 5.1 - Fix overlap box size mismatch
- [ ] 5.4 - Fix ground check distance (reduce to 0.5m)
- [ ] 6.1 - Pool destruction effects
- [ ] 6.3 - Sync structural integrity collapse VFX
- [ ] 7.1 - Fix OverlapSphere GC allocation
- [ ] 7.2 - Replace GetComponent with TryGetComponent
- [ ] 7.3 - Fix StructuralIntegrity material leak
- [ ] 7.4 - Fix BuildGhost material leak

### 🟡 MEDIUM PRIORITY (Fix This Month)
- [ ] 2.4 - Add client-side placement rejection RPC
- [ ] 3.5 - Ensure server/client validation logic matches
- [ ] 5.2 - Add snap point validation
- [ ] 7.5 - Wrap Debug.Log with conditional compilation
- [ ] 7.6 - Add cooldown to neighbor stability updates
- [ ] 7.10 - Use NetworkObjectPool consistently
- [ ] 9.1 - Consolidate duplicate building systems

---

## ✅ VERIFIED CORRECT IMPLEMENTATIONS

1. ✅ Ghost preview is local-only (not synced) - CORRECT
2. ✅ Structures spawned via NetworkServer.Spawn - CORRECT
3. ✅ Rate limiting prevents spam - CORRECT
4. ✅ Server-side validation exists - CORRECT (but incomplete)
5. ✅ Client-side prediction for ghost preview - CORRECT
6. ✅ Material cleanup in SimpleBuildMode - CORRECT
7. ✅ TryGetComponent usage in TrapBase - CORRECT

---

**END OF AUDIT REPORT**

