using System;

namespace TacticalCombat.Core
{
    /// <summary>
    /// Serializable award data for Mirror RPC
    /// </summary>
    [Serializable]
    public struct AwardData
    {
        public ulong playerId;
        public AwardType awardType;

        public AwardData(ulong id, AwardType type)
        {
            playerId = id;
            awardType = type;
        }
    }
}

