# 🗑️ SCRIPTS TO DELETE / CLEANUP

**Date:** 2025-01-26  
**Purpose:** List of scripts that are no longer needed after transformation

---

## ⚠️ SCRIPTS TO REVIEW FOR DELETION

### Old Round System Code (No longer needed):
- Check `MatchManager.cs` for old BO3/round code that can be removed
- `RoundState` class in `DataModels.cs` - Can be removed (replaced by MatchState)
- Any UI code referencing "Round" instead of "Match"

### Unused Events:
- `OnRoundWonEvent` in MatchManager - No longer used (replaced by OnMatchWonEvent)

### Deprecated Methods:
- `GetCurrentRound()` - No longer exists (removed)
- Any methods referencing `currentRound` variable

---

## 📝 CLEANUP TASKS

### MatchManager.cs:
- [ ] Remove `teamAWins` and `teamBWins` SyncVars (if not needed for display)
- [ ] Remove `OnRoundWonEvent` (replaced by OnMatchWonEvent)
- [ ] Remove old BO3 logic (if any remains)

### DataModels.cs:
- [ ] Remove `RoundState` class (replaced by MatchState)
- [ ] Keep `MatchState` class

### UI Files:
- [ ] Remove round-related UI elements (if any)
- [ ] Update any "Round" text to "Match"

---

## ✅ KEEP THESE (Still Used):

- All Player systems (FPSController, PlayerController, etc.)
- All Combat systems (WeaponSystem, Health, etc.)
- All Building systems (BuildValidator, Structure, etc.)
- All Trap systems (TrapBase, SpikeTrap, etc.)
- Network systems (NetworkGameManager, etc.)

---

**Note:** Be careful when deleting - some code might still be referenced. Always test after cleanup.

