# 🎯 CLAN SYSTEM INTEGRATION PROMPT - OPTIMIZED FOR CURRENT CODEBASE

**Mevcut Kod Yapısına Göre Optimize Edilmiş Prompt**

---

## 📋 CONTEXT (Mevcut Sistem)

You are analyzing a **Unity + Mirror networking** multiplayer PvP survival-arena game.

**Current Architecture:**
- **Namespace:** `TacticalCombat.*` (Core, Network, Player, Combat, Building, Traps, UI)
- **MatchManager:** Phase management (Lobby → Build → Combat → RoundEnd), Team tracking (TeamA/TeamB), PlayerState dictionary
- **NetworkGameManager:** Custom Mirror NetworkManager, player spawning, team auto-balance
- **Team System:** `Team` enum (TeamA, TeamB, None) - currently used for match teams
- **PlayerState:** Contains `playerId`, `team`, `role`, `budget`, `isAlive`
- **RoleId:** Enum (Builder, Guardian, Ranger, Saboteur)
- **Build System:** BuildValidator, SimpleBuildMode, Structure placement
- **Combat System:** WeaponSystem, Health, Hitbox, server-authoritative damage
- **Trap System:** TrapBase, SpikeTrap, GlueTrap, Springboard, DartTurret

**Current Game Flow:**
1. Players connect → NetworkGameManager spawns → Auto-assigns TeamA/TeamB
2. MatchManager.RegisterPlayer() → Stores PlayerState
3. Match starts → Build phase → Combat phase → RoundEnd
4. BO3 system (best of 3 rounds)
5. Win condition: All players dead OR core destroyed

---

## 🎯 TASK: CLAN SYSTEM INTEGRATION

Extend the current system to support **Clan-based progression** while maintaining compatibility with existing Team/PlayerState architecture.

**Key Requirements:**
1. **Clan System** - New layer above Team system
2. **Lobby/Room System** - Custom matchmaking with room browser
3. **XP/Progression** - Clan-centric rewards
4. **Persistent Data** - Save clan/player stats between sessions
5. **Backward Compatibility** - Existing Team system must still work

---

## 🏗️ ARCHITECTURE EXTENSION PLAN

### 1️⃣ **Clan System Integration**

**New Classes to Add:**
```
TacticalCombat.Core.ClanManager (NetworkBehaviour, Singleton)
TacticalCombat.Core.ClanData (ScriptableObject or Serializable)
TacticalCombat.Core.PlayerProfile (extends PlayerState)
TacticalCombat.Network.LobbyManager (extends NetworkManager)
TacticalCombat.Network.RoomData (Serializable)
TacticalCombat.Persistence.PersistentDataService (abstract base)
TacticalCombat.Persistence.FirebaseDataService (implementation)
TacticalCombat.UI.ClanLobbyUI (UI for room browser)
TacticalCombat.UI.ClanScoreboard (extends existing Scoreboard)
```

**Integration Points:**
- **MatchManager:** Add `clanA` and `clanB` fields, map TeamA → ClanA, TeamB → ClanB
- **PlayerState:** Add `clanId` field, keep `team` for backward compatibility
- **NetworkGameManager:** Add room creation/joining logic
- **Existing Team System:** Keep as-is, add Clan layer on top

**Design Decision:**
- **Option A:** TeamA/TeamB = ClanA/ClanB (1:1 mapping, simpler)
- **Option B:** Multiple players per clan, clans assigned to teams (more complex, supports 8v8)
- **Recommendation:** Start with Option A, design for Option B later

---

### 2️⃣ **Lobby/Room System**

**Current State:**
- No lobby system exists
- Players connect directly to server
- Auto-assigns teams on connect

**New System:**
```
LobbyManager (NetworkBehaviour)
├── RoomList (SyncList<RoomData>)
├── CreateRoom(string roomName, int maxPlayers)
├── JoinRoom(uint roomId)
├── LeaveRoom()
└── StartMatch() (when min players reached)

RoomData (Serializable)
├── roomId (uint)
├── roomName (string)
├── hostPlayerId (ulong)
├── clanA (ClanData)
├── clanB (ClanData)
├── players (List<PlayerProfile>)
├── maxPlayers (int)
└── isMatchStarted (bool)
```

**Integration:**
- Replace direct connection with room-based connection
- NetworkGameManager.OnServerAddPlayer() → Check if joining room
- MatchManager.StartMatch() → Only if room has min players (2v2)

---

### 3️⃣ **Clan System**

**ClanManager Responsibilities:**
- Create/Delete/Join/Leave clans
- Clan XP aggregation from match results
- Clan level calculation
- Clan member management (ranks, permissions)
- Server-authoritative validation

**ClanData Structure:**
```csharp
[System.Serializable]
public class ClanData
{
    public string clanId;           // Unique ID (GUID or database ID)
    public string clanName;         // Display name
    public string clanTag;          // Short tag (e.g., "CLAN")
    public Color clanColor;         // Team color
    public Texture2D clanLogo;      // Custom logo
    
    public int clanXP;              // Total XP
    public int clanLevel;           // Calculated from XP
    public int wins;                // Match wins
    public int losses;              // Match losses
    
    public List<ClanMember> members; // Member list
    public ulong ownerId;           // Clan leader
    public DateTime createdAt;
}

[System.Serializable]
public class ClanMember
{
    public ulong playerId;
    public string username;
    public ClanRank rank;           // Leader, Officer, Member
    public int contributionXP;      // Individual contribution
    public DateTime joinedAt;
}
```

**XP Calculation:**
- Match win: +100 XP per player
- Match loss: +25 XP per player
- Kills: +10 XP each
- Assists: +5 XP each
- Structure durability: +1 XP per 100 HP
- Win streak bonus: +10% per streak

---

### 4️⃣ **Player Progression**

**Extend PlayerState:**
```csharp
public class PlayerProfile : PlayerState
{
    // Existing fields (playerId, team, role, budget, isAlive)
    
    // New fields
    public string clanId;           // Current clan
    public int playerXP;            // Individual XP (for personal unlocks)
    public int playerLevel;         // Individual level
    public PlayerStats stats;       // Kills, deaths, wins, etc.
    public List<string> unlockedItems; // Weapon skins, abilities, etc.
}

[System.Serializable]
public class PlayerStats
{
    public int totalKills;
    public int totalDeaths;
    public int totalAssists;
    public int matchesWon;
    public int matchesLost;
    public float avgSurvivalTime;
    public int structuresBuilt;
    public int trapsPlaced;
}
```

**Progression Rewards:**
- Level 5: Unlock new weapon skin
- Level 10: Unlock new trap type
- Level 15: Unlock new build structure
- Level 20: Unlock clan banner customization

---

### 5️⃣ **Database Schema**

**Recommended Schema:**
```sql
-- Clans Table
CREATE TABLE clans (
    id VARCHAR(36) PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    tag VARCHAR(10) UNIQUE NOT NULL,
    xp INT DEFAULT 0,
    level INT DEFAULT 1,
    wins INT DEFAULT 0,
    losses INT DEFAULT 0,
    owner_id BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    logo_url VARCHAR(255),
    color_r INT,
    color_g INT,
    color_b INT
);

-- Players Table
CREATE TABLE players (
    id BIGINT PRIMARY KEY,  -- NetworkIdentity.netId
    username VARCHAR(50) NOT NULL,
    clan_id VARCHAR(36) NULL,
    xp INT DEFAULT 0,
    level INT DEFAULT 1,
    kills INT DEFAULT 0,
    deaths INT DEFAULT 0,
    assists INT DEFAULT 0,
    matches_won INT DEFAULT 0,
    matches_lost INT DEFAULT 0,
    last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (clan_id) REFERENCES clans(id) ON DELETE SET NULL
);

-- Clan Members Table (Many-to-Many)
CREATE TABLE clan_members (
    clan_id VARCHAR(36),
    player_id BIGINT,
    rank ENUM('Leader', 'Officer', 'Member') DEFAULT 'Member',
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    contribution_xp INT DEFAULT 0,
    PRIMARY KEY (clan_id, player_id),
    FOREIGN KEY (clan_id) REFERENCES clans(id) ON DELETE CASCADE,
    FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE
);

-- Matches Table
CREATE TABLE matches (
    id VARCHAR(36) PRIMARY KEY,
    clan_a_id VARCHAR(36),
    clan_b_id VARCHAR(36),
    winner_id VARCHAR(36) NULL,
    duration INT,  -- seconds
    started_at TIMESTAMP,
    ended_at TIMESTAMP,
    score_json JSON,  -- Detailed stats
    FOREIGN KEY (clan_a_id) REFERENCES clans(id),
    FOREIGN KEY (clan_b_id) REFERENCES clans(id),
    FOREIGN KEY (winner_id) REFERENCES clans(id)
);
```

---

### 6️⃣ **Networking Flow**

**Current Flow:**
```
Client → NetworkGameManager.OnServerAddPlayer() → Spawn Player → MatchManager.RegisterPlayer()
```

**New Flow:**
```
Client → LobbyManager.ConnectToLobby() 
      → Show Room List
      → CreateRoom() OR JoinRoom()
      → NetworkGameManager.OnServerAddPlayer() (in room context)
      → MatchManager.RegisterPlayer() (with clanId)
      → StartMatch() (when room ready)
```

**Room Sync:**
- Use `SyncList<RoomData>` for room list
- Use `SyncVar` for room state (isMatchStarted, players)
- Use `[Command]` for room actions (Create, Join, Leave)
- Use `[ClientRpc]` for room updates

---

### 7️⃣ **Security & Anti-Exploit**

**Server-Only Validation:**
- Clan creation: Validate unique name/tag
- Clan XP: Only award after valid match end
- Match results: Server calculates, client receives
- XP farming: Rate limit matches per hour
- Disconnect exploit: Award XP only if match completed (>50% duration)

**Validation Points:**
```csharp
[Server]
public void AwardClanXP(string clanId, int xp)
{
    // ✅ Validate: Only call from MatchManager after match end
    // ✅ Validate: XP amount is reasonable (< 1000 per match)
    // ✅ Validate: Match was valid (> 2v2, > 5 minutes)
    // ✅ Validate: No duplicate awards (check matchId)
}
```

---

### 8️⃣ **UI Integration**

**New UI Components:**
- `ClanLobbyUI.cs` - Room browser, create/join buttons
- `ClanProfileUI.cs` - Clan info, members, stats
- `ClanScoreboard.cs` - Extends existing Scoreboard, shows clan XP
- `PlayerProfileUI.cs` - Player stats, progression, unlocks

**UI Flow:**
```
MainMenu → ClanLobbyUI (Room List)
        → Click Room → Show RoomDetailsUI (Team A vs Team B)
        → Join Room → Wait for Start Match
        → Match Starts → GameScene
        → Match Ends → Show ClanScoreboard (XP gained)
        → Return to Lobby
```

---

### 9️⃣ **Backward Compatibility**

**Migration Strategy:**
- Keep existing `Team` enum (TeamA, TeamB, None)
- Add `clanId` to PlayerState (nullable, defaults to null)
- If `clanId == null`, use existing team system
- If `clanId != null`, map ClanA → TeamA, ClanB → TeamB
- Existing code continues to work

**Code Changes:**
```csharp
// MatchManager.cs - Minimal changes
public void RegisterPlayer(ulong playerId, Team team, RoleId role, string clanId = null)
{
    // Existing code...
    if (clanId != null)
    {
        // Map clan to team
        team = GetTeamForClan(clanId);
    }
    // Rest of existing code...
}
```

---

### 🔟 **Testing Checklist**

**1v1 Test:**
- [ ] Create room with 1 player
- [ ] Cannot start match (min 2v2)
- [ ] Second player joins → Can start match
- [ ] Match ends → XP awarded to both clans

**2v2 Test:**
- [ ] Create room, 2 players join ClanA, 2 join ClanB
- [ ] Start match → All players spawn correctly
- [ ] Match plays → Stats tracked
- [ ] Match ends → XP calculated and saved

**4v4 Stress Test:**
- [ ] 8 players in room
- [ ] Network sync (room list, player list)
- [ ] Match performance (60 FPS, < 100ms latency)
- [ ] Disconnect handling (mid-match)

**Clan XP Sync:**
- [ ] Match ends → XP calculated server-side
- [ ] XP saved to database
- [ ] Clan level updates correctly
- [ ] UI shows updated XP/level

---

## 📝 IMPLEMENTATION PRIORITY

### Phase 1: Foundation (Week 1)
1. ✅ ClanManager.cs (basic structure)
2. ✅ ClanData.cs (data model)
3. ✅ Extend PlayerState → PlayerProfile
4. ✅ Database schema setup

### Phase 2: Lobby System (Week 2)
1. ✅ LobbyManager.cs (room creation/joining)
2. ✅ RoomData.cs (room structure)
3. ✅ ClanLobbyUI.cs (UI for room browser)
4. ✅ Integration with NetworkGameManager

### Phase 3: XP System (Week 3)
1. ✅ XP calculation in MatchManager
2. ✅ PersistentDataService (Firebase/PlayFab)
3. ✅ Clan XP aggregation
4. ✅ Level calculation

### Phase 4: UI & Polish (Week 4)
1. ✅ ClanProfileUI
2. ✅ ClanScoreboard
3. ✅ Player progression UI
4. ✅ Testing & bug fixes

---

## 🎯 EXPECTED OUTPUT

When you run this prompt, generate:

1. **ClanManager.cs** - Full implementation with server authority
2. **LobbyManager.cs** - Room system with Mirror SyncList
3. **ClanData.cs** - Serializable data model
4. **PlayerProfile.cs** - Extended PlayerState
5. **PersistentDataService.cs** - Abstract base + Firebase implementation
6. **ClanLobbyUI.cs** - Room browser UI
7. **Integration Guide** - How to integrate with existing MatchManager
8. **Migration Script** - Convert existing PlayerState to PlayerProfile
9. **Test Scenarios** - Automated test cases
10. **Security Checklist** - Anti-exploit measures

---

## ⚠️ IMPORTANT NOTES

1. **Mirror Networking:** Use Mirror's SyncList, SyncVar, Command, ClientRpc (NOT NGO)
2. **Namespace:** All classes in `TacticalCombat.*` namespace
3. **Backward Compatibility:** Existing Team system must continue working
4. **Server Authority:** All XP/Clan operations server-only
5. **Performance:** Target 60 FPS, < 100ms latency for 4v4
6. **Scalability:** Design for 8v8 future expansion

---

## 🚀 USAGE

Paste this prompt into Cursor/Claude/Copilot Chat, then:

1. Let AI analyze current codebase structure
2. Generate ClanManager.cs scaffolding
3. Generate LobbyManager.cs with Mirror integration
4. Generate database schema
5. Generate UI components
6. Provide integration guide for existing systems

**Expected Time:** 2-3 hours for full implementation

---

## 📋 CODE GENERATION PROMPTS (Optional)

If you need specific implementations, use these sub-prompts:

**Prompt 1: ClanManager Implementation**
```
Generate ClanManager.cs for Unity + Mirror networking:
- Server-authoritative clan creation/deletion
- Clan member management (join/leave/kick)
- Clan XP aggregation from match results
- Clan level calculation (XP-based)
- Integration with existing MatchManager
- Namespace: TacticalCombat.Core
```

**Prompt 2: LobbyManager Implementation**
```
Generate LobbyManager.cs extending Mirror NetworkManager:
- Room creation with SyncList<RoomData>
- Room joining/leaving with [Command]/[ClientRpc]
- Room list sync to all clients
- Minimum player check before match start
- Integration with NetworkGameManager
- Namespace: TacticalCombat.Network
```

**Prompt 3: PersistentDataService**
```
Generate PersistentDataService.cs abstract base + Firebase implementation:
- Save/load clan data
- Save/load player profiles
- Save match results
- Anti-tamper validation
- Async/await pattern
- Error handling
- Namespace: TacticalCombat.Persistence
```

---

**END OF PROMPT**

