using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.Core
{
    /// <summary>
    /// ✅ NEW: Ranking System - MMR, Rank Tiers, and Leaderboard
    /// </summary>
    public class RankingSystem : MonoBehaviour
    {
        public static RankingSystem Instance { get; private set; }

        [Header("MMR Settings")]
        [SerializeField] private int startingMMR = 1000;
        [SerializeField] private int minMMR = 0;
        [SerializeField] private int maxMMR = 5000;
        [SerializeField] private int mmrGainPerWin = 25;
        [SerializeField] private int mmrLossPerLoss = 25;

        [Header("Rank Tiers")]
        [SerializeField] private RankTierConfig[] rankTiers;

        // Player MMR storage (in production, this would be saved to database)
        private Dictionary<ulong, PlayerRankData> playerRanks = new Dictionary<ulong, PlayerRankData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize default rank tiers if not set
            if (rankTiers == null || rankTiers.Length == 0)
            {
                InitializeDefaultRankTiers();
            }
        }

        private void InitializeDefaultRankTiers()
        {
            rankTiers = new RankTierConfig[]
            {
                new RankTierConfig { tier = RankTier.Bronze, minMMR = 0, maxMMR = 999 },
                new RankTierConfig { tier = RankTier.Silver, minMMR = 1000, maxMMR = 1499 },
                new RankTierConfig { tier = RankTier.Gold, minMMR = 1500, maxMMR = 1999 },
                new RankTierConfig { tier = RankTier.Platinum, minMMR = 2000, maxMMR = 2499 },
                new RankTierConfig { tier = RankTier.Diamond, minMMR = 2500, maxMMR = 2999 },
                new RankTierConfig { tier = RankTier.Master, minMMR = 3000, maxMMR = 3499 },
                new RankTierConfig { tier = RankTier.Grandmaster, minMMR = 3500, maxMMR = 5000 }
            };
        }

        /// <summary>
        /// Get or create player rank data
        /// </summary>
        public PlayerRankData GetPlayerRank(ulong playerId)
        {
            if (!playerRanks.ContainsKey(playerId))
            {
                playerRanks[playerId] = new PlayerRankData
                {
                    playerId = playerId,
                    mmr = startingMMR,
                    rankTier = GetRankTier(startingMMR),
                    wins = 0,
                    losses = 0
                };
            }
            return playerRanks[playerId];
        }

        /// <summary>
        /// Update MMR after match result
        /// </summary>
        public void UpdateMMR(ulong playerId, bool won, int performanceScore = 0)
        {
            var rankData = GetPlayerRank(playerId);
            
            // Calculate MMR change based on win/loss and performance
            int mmrChange = won ? mmrGainPerWin : -mmrLossPerLoss;
            
            // Performance bonus/penalty (max ±10 MMR)
            int performanceBonus = Mathf.Clamp(performanceScore / 100, -10, 10);
            mmrChange += performanceBonus;
            
            rankData.mmr += mmrChange;
            rankData.mmr = Mathf.Clamp(rankData.mmr, minMMR, maxMMR);
            
            // Update rank tier
            rankData.rankTier = GetRankTier(rankData.mmr);
            
            // Update win/loss count
            if (won)
            {
                rankData.wins++;
            }
            else
            {
                rankData.losses++;
            }
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[RankingSystem] Player {playerId}: MMR {rankData.mmr} ({rankData.rankTier}), W/L: {rankData.wins}/{rankData.losses}");
            #endif
        }

        /// <summary>
        /// Get rank tier from MMR
        /// </summary>
        public RankTier GetRankTier(int mmr)
        {
            if (rankTiers == null || rankTiers.Length == 0)
            {
                InitializeDefaultRankTiers();
            }

            for (int i = rankTiers.Length - 1; i >= 0; i--)
            {
                if (mmr >= rankTiers[i].minMMR)
                {
                    return rankTiers[i].tier;
                }
            }
            return RankTier.Bronze;
        }

        /// <summary>
        /// Get leaderboard (top N players)
        /// </summary>
        public List<PlayerRankData> GetLeaderboard(int topN = 10)
        {
            return playerRanks.Values
                .OrderByDescending(p => p.mmr)
                .ThenByDescending(p => p.wins)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// Get player's leaderboard position
        /// </summary>
        public int GetLeaderboardPosition(ulong playerId)
        {
            var sorted = playerRanks.Values
                .OrderByDescending(p => p.mmr)
                .ThenByDescending(p => p.wins)
                .ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].playerId == playerId)
                {
                    return i + 1; // 1-indexed
                }
            }
            return -1; // Not found
        }

        /// <summary>
        /// Reset player rank (for testing or season reset)
        /// </summary>
        public void ResetPlayerRank(ulong playerId)
        {
            if (playerRanks.ContainsKey(playerId))
            {
                playerRanks[playerId] = new PlayerRankData
                {
                    playerId = playerId,
                    mmr = startingMMR,
                    rankTier = GetRankTier(startingMMR),
                    wins = 0,
                    losses = 0
                };
            }
        }
    }

    /// <summary>
    /// Player rank data
    /// </summary>
    [System.Serializable]
    public class PlayerRankData
    {
        public ulong playerId;
        public int mmr;
        public RankTier rankTier;
        public int wins;
        public int losses;
        public float winRate => (wins + losses) > 0 ? (float)wins / (wins + losses) : 0f;
    }

    /// <summary>
    /// Rank tier configuration
    /// </summary>
    [System.Serializable]
    public class RankTierConfig
    {
        public RankTier tier;
        public int minMMR;
        public int maxMMR;
    }

    /// <summary>
    /// Rank tier enum
    /// </summary>
    public enum RankTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Diamond = 4,
        Master = 5,
        Grandmaster = 6
    }
}







