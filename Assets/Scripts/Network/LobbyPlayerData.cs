using System;
using Mirror;
using UnityEngine;

namespace TacticalCombat.Network
{
    /// <summary>
    /// Serializable player data for lobby synchronization
    /// </summary>
    [Serializable]
    public struct LobbyPlayerData : IEquatable<LobbyPlayerData>
    {
        public uint connectionId;
        public string playerName;
        public int teamId; // 0 = Team A, 1 = Team B, -1 = Not assigned
        public bool isReady;
        public bool isHost;

        public LobbyPlayerData(uint connId, string name, int team = -1, bool ready = false, bool host = false)
        {
            connectionId = connId;
            playerName = name;
            teamId = team;
            isReady = ready;
            isHost = host;
        }

        public bool Equals(LobbyPlayerData other)
        {
            return connectionId == other.connectionId;
        }

        public override bool Equals(object obj)
        {
            return obj is LobbyPlayerData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return connectionId.GetHashCode();
        }
    }

    /// <summary>
    /// Custom reader/writer for Mirror networking
    /// </summary>
    public static class LobbyPlayerDataReaderWriter
    {
        public static void WriteLobbyPlayerData(this NetworkWriter writer, LobbyPlayerData data)
        {
            writer.WriteUInt(data.connectionId);
            writer.WriteString(data.playerName);
            writer.WriteInt(data.teamId);
            writer.WriteBool(data.isReady);
            writer.WriteBool(data.isHost);
        }

        public static LobbyPlayerData ReadLobbyPlayerData(this NetworkReader reader)
        {
            return new LobbyPlayerData
            {
                connectionId = reader.ReadUInt(),
                playerName = reader.ReadString(),
                teamId = reader.ReadInt(),
                isReady = reader.ReadBool(),
                isHost = reader.ReadBool()
            };
        }
    }
}







