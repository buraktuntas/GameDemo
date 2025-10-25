using UnityEngine;

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
}


