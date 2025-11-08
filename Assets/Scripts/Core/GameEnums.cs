namespace TacticalCombat.Core
{
    public enum Phase
    {
        Lobby,
        Build,
        Combat,
        RoundEnd
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
        UtilityGate
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
    /// ✅ CLAN SYSTEM: Clan member ranks
    /// </summary>
    public enum ClanRank
    {
        Member,     // Regular member
        Officer,    // Can invite/kick members
        Leader      // Full control (create/delete clan, assign ranks)
    }
}


