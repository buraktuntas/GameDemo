using UnityEngine;
using System.Collections.Generic;

namespace TacticalCombat.Core
{
    [System.Serializable]
    public struct BuildRequest
    {
        public Vector3 position;
        public Quaternion rotation;
        public StructureType type;
        public ulong playerId;

        public BuildRequest(Vector3 pos, Quaternion rot, StructureType structureType, ulong id)
        {
            position = pos;
            rotation = rot;
            type = structureType;
            playerId = id;
        }
    }

    [System.Serializable]
    public struct BuildBudget
    {
        public int wallPoints;
        public int elevationPoints;
        public int trapPoints;
        public int utilityPoints;

        public BuildBudget(int wall, int elevation, int trap, int utility)
        {
            wallPoints = wall;
            elevationPoints = elevation;
            trapPoints = trap;
            utilityPoints = utility;
        }

        public static BuildBudget GetRoleBudget(RoleId role)
        {
            return role switch
            {
                RoleId.Builder => new BuildBudget(60, 40, 30, 20),
                RoleId.Guardian => new BuildBudget(20, 10, 10, 5),
                RoleId.Ranger => new BuildBudget(10, 10, 5, 5),
                RoleId.Saboteur => new BuildBudget(5, 5, 5, 5),
                _ => new BuildBudget(0, 0, 0, 0)
            };
        }
    }

    [System.Serializable]
    public class PlayerState
    {
        public ulong playerId;
        public Team team;
        public RoleId role;
        public bool isAlive;
        public BuildBudget budget;

        public PlayerState(ulong id, Team playerTeam, RoleId playerRole)
        {
            playerId = id;
            team = playerTeam;
            role = playerRole;
            isAlive = true;
            budget = BuildBudget.GetRoleBudget(playerRole);
        }
    }

    [System.Serializable]
    public class RoundState
    {
        public Phase phase;
        public float remainingTime;
        public int teamAWins;
        public int teamBWins;
        public int currentRound;

        public RoundState()
        {
            phase = Phase.Lobby;
            remainingTime = 0f;
            teamAWins = 0;
            teamBWins = 0;
            currentRound = 0;
        }
    }

    [System.Serializable]
    public struct StructureCost
    {
        public StructureType type;
        public StructureCategory category;
        public int cost;

        public StructureCost(StructureType t, StructureCategory c, int costValue)
        {
            type = t;
            category = c;
            cost = costValue;
        }
    }

    /// <summary>
    /// Core Object data - can be stolen and returned
    /// </summary>
    [System.Serializable]
    public class CoreObjectData
    {
        public ulong ownerId;          // Player/Team who owns this core
        public ulong carrierId;         // Player currently carrying (0 if not carried)
        public Vector3 spawnPosition;   // Original spawn position
        public bool isCarried;          // Is someone carrying it?
        public bool isReturned;         // Has it been returned to base?

        // ✅ CRITICAL FIX: Track who returned the core to determine winner correctly
        public ulong returnerId;        // Player who returned this core (0 if not returned)
        public Team returnerTeam;       // Team of the player who returned this core

        public CoreObjectData(ulong owner)
        {
            ownerId = owner;
            carrierId = 0;
            spawnPosition = Vector3.zero;
            isCarried = false;
            isReturned = false;
            returnerId = 0;
            returnerTeam = Team.None;
        }
    }

    /// <summary>
    /// Match statistics for a single player
    /// </summary>
    [System.Serializable]
    public class MatchStats
    {
        public ulong playerId;
        public int kills;
        public int deaths;
        public int assists;
        public int structuresBuilt;
        public int trapKills;
        public int captures;           // Core captures
        public float defenseTime;      // Time spent defending base (seconds)
        public int totalScore;         // Calculated total score

        public MatchStats(ulong id)
        {
            playerId = id;
            kills = 0;
            deaths = 0;
            assists = 0;
            structuresBuilt = 0;
            trapKills = 0;
            captures = 0;
            defenseTime = 0f;
            totalScore = 0;
        }

        public void CalculateTotalScore()
        {
            totalScore = kills * GameConstants.SCORE_KILL +
                        assists * GameConstants.SCORE_ASSIST +
                        structuresBuilt * GameConstants.SCORE_STRUCTURE_BUILT +
                        trapKills * GameConstants.SCORE_TRAP_KILL +
                        captures * GameConstants.SCORE_CAPTURE +
                        Mathf.RoundToInt(defenseTime * GameConstants.SCORE_DEFENSE_TIME_PER_SECOND);
        }
    }

    /// <summary>
    /// Blueprint data - saved build configuration
    /// </summary>
    [System.Serializable]
    public class Blueprint
    {
        public string blueprintName;
        public ulong playerId;
        public List<BlueprintStructure> structures;

        public Blueprint(string name, ulong id)
        {
            blueprintName = name;
            playerId = id;
            structures = new List<BlueprintStructure>();
        }
    }

    [System.Serializable]
    public struct BlueprintStructure
    {
        public StructureType type;
        public Vector3 localPosition;  // Relative to spawn/base
        public Quaternion rotation;
        public StructureCategory category;
        public int cost;

        public BlueprintStructure(StructureType t, Vector3 pos, Quaternion rot, StructureCategory cat, int c)
        {
            type = t;
            localPosition = pos;
            rotation = rot;
            category = cat;
            cost = c;
        }
    }

    /// <summary>
    /// Throwable item data
    /// </summary>
    [System.Serializable]
    public class ThrowableData
    {
        public ThrowableType type;
        public Vector3 position;
        public ulong throwerId;
        public float throwTime;
        public float duration;
        public float radius;

        public ThrowableData(ThrowableType t, Vector3 pos, ulong thrower, float dur, float rad)
        {
            type = t;
            position = pos;
            throwerId = thrower;
            throwTime = Time.time;
            duration = dur;
            radius = rad;
        }
    }

    /// <summary>
    /// Trap link data - chains traps together
    /// </summary>
    [System.Serializable]
    public class TrapLinkData
    {
        public uint trapId;            // Network ID of this trap
        public List<uint> linkedTrapIds; // Network IDs of linked traps
        public int chainIndex;          // Position in chain (0 = first)

        public TrapLinkData(uint id)
        {
            trapId = id;
            linkedTrapIds = new List<uint>();
            chainIndex = 0;
        }
    }

    /// <summary>
    /// Updated RoundState - now MatchState (single match, no BO3)
    /// </summary>
    [System.Serializable]
    public class MatchState
    {
        public Phase phase;
        public float remainingTime;
        public GameMode gameMode;
        public Dictionary<ulong, MatchStats> playerStats;
        public Dictionary<ulong, CoreObjectData> coreObjects; // Team/Player ID -> Core Data
        public bool suddenDeathActive;

        public MatchState()
        {
            phase = Phase.Lobby;
            remainingTime = 0f;
            gameMode = GameMode.Team4v4;
            playerStats = new Dictionary<ulong, MatchStats>();
            coreObjects = new Dictionary<ulong, CoreObjectData>();
            suddenDeathActive = false;
        }
    }
}


