namespace TacticalCombat.Core
{
    /// <summary>
    /// Game phases - Updated for new game structure
    /// </summary>
    public enum Phase
    {
        Lobby,
        Build,          // 3 minutes - players build personal defense bases
        Combat,         // 15 minutes - steal enemy Core Object
        SuddenDeath,    // Final 2 minutes - secret tunnel opens
        End             // Match end - show scoreboard and awards
    }

    /// <summary>
    /// Game mode types
    /// </summary>
    public enum GameMode
    {
        FFA,            // Free-for-all (up to 8 players)
        Team4v4         // 4v4 team mode
    }

    public enum Team
    {
        None,
        TeamA,
        TeamB
    }

    public enum RoleId
    {
        Builder,
        Guardian,
        Ranger,
        Saboteur
    }

    public enum StructureType
    {
        Wall,
        Platform,
        Ramp,
        CoreStructure,
        TrapSpike,
        TrapGlue,
        TrapSpringboard,
        TrapDartTurret,
        UtilityGate,
        InfoTower       // New: Hackable tower for minimap reveals
    }

    public enum StructureCategory
    {
        Wall,
        Elevation,
        Trap,
        Utility,
        Core
    }

    public enum TrapType
    {
        Static,
        Mechanical
    }

    /// <summary>
    /// Throwable item types
    /// </summary>
    public enum ThrowableType
    {
        Smoke,          // Creates smoke screen
        EMP,            // Disables traps/structures temporarily
        StickyBomb,     // Explosive that sticks to surfaces
        RevealDart      // Reveals enemy positions on minimap
    }

    /// <summary>
    /// End-game awards
    /// </summary>
    public enum AwardType
    {
        Slayer,         // Most kills
        Architect,      // Most structures built
        Guardian,       // Most defense time
        Carrier,        // Most core captures
        Saboteur        // Most sabotages/trap kills
    }

    /// <summary>
    /// ✅ CLAN SYSTEM: Clan member ranks
    /// </summary>
    public enum ClanRank
    {
        Member,     // Regular member
        Officer,    // Can invite/kick members
        Leader      // Full control (create/delete clan, assign ranks)
    }
}


