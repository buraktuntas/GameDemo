namespace TacticalCombat.Core
{
    public static class GameConstants
    {
        // Phase Durations - Updated for new game structure
        public const float BUILD_DURATION = 180f; // 3:00 minutes
        public const float COMBAT_DURATION = 900f; // 15:00 minutes
        public const float SUDDEN_DEATH_DURATION = 120f; // 2:00 minutes (final 2 minutes of combat)
        public const float END_PHASE_DURATION = 10f; // 10 seconds to show scoreboard

        // Game Mode Settings
        public const int MAX_PLAYERS_FFA = 8;
        public const int MAX_PLAYERS_TEAM = 8; // 4v4
        public const int MIN_PLAYERS_TO_START = 2;

        // Removed BO3 - single match structure now

        // Structure Health
        public const int CORE_HP = 1200;
        public const int WALL_HP = 200;
        public const int PLATFORM_HP = 150;
        public const int RAMP_HP = 100;

        // Trap Damage
        public const int SPIKE_TRAP_DAMAGE = 50;
        public const int DART_TRAP_DAMAGE = 25;

        // Sabotage
        public const float SABOTAGE_INTERACT_TIME = 2.5f;
        public const float SABOTAGE_DISABLE_DURATION = 15f;
        public const float SABOTAGE_REVEAL_DURATION = 5f;

        // Vision Control
        public const float MID_CAPTURE_TIME = 5f;
        public const float VISION_PULSE_INTERVAL = 3f;
        public const float VISION_PULSE_RADIUS = 20f;
        public const float VISION_PULSE_DURATION = 2f;

        // Combat
        public const float BOW_PROJECTILE_SPEED = 30f;
        public const int BOW_DAMAGE = 50;
        public const float BOW_COOLDOWN = 1f;

        public const float SPEAR_RANGE = 2.5f;
        public const int SPEAR_DAMAGE = 75;
        public const float SPEAR_COOLDOWN = 1.2f;

        // Friendly Fire
        public const bool FRIENDLY_FIRE_ENABLED = true;
        public const float FRIENDLY_FIRE_DAMAGE_MULTIPLIER = 0.5f; // 50% damage to teammates

        // Player
        public const int PLAYER_MAX_HEALTH = 100;
        public const float PLAYER_MOVE_SPEED = 5f;
        public const float PLAYER_JUMP_FORCE = 7f;

        // Abilities
        public const float BUILDER_RAPID_DEPLOY_DURATION = 5f;
        public const float BUILDER_RAPID_DEPLOY_COOLDOWN = 60f;
        
        public const float GUARDIAN_BULWARK_DURATION = 3f;
        public const float GUARDIAN_BULWARK_COOLDOWN = 45f;
        
        public const float RANGER_SCOUT_ARROW_REVEAL_DURATION = 2f;
        public const float RANGER_SCOUT_ARROW_COOLDOWN = 30f;
        
        public const float SABOTEUR_SHADOW_STEP_DURATION = 4f;
        public const float SABOTEUR_SHADOW_STEP_COOLDOWN = 40f;

        // Build
        public const float BUILD_PLACEMENT_RANGE = 5f;
        public const float BUILD_SNAP_DISTANCE = 0.5f;
        public const float BUILD_MAX_DISTANCE_FROM_SPAWN = 50f; // Max distance from spawn for personal base

        // Core Object
        public const float CORE_CARRY_SPEED_MULTIPLIER = 0.7f; // 70% speed when carrying core
        public const float CORE_RETURN_DISTANCE = 3f; // Distance to return core to base
        public const int CORE_RETURN_SCORE = 100; // Points for returning core

        // Throwables
        public const float SMOKE_DURATION = 10f;
        public const float EMP_DURATION = 5f;
        public const float EMP_RADIUS = 10f;
        public const float STICKY_BOMB_DAMAGE = 100f;
        public const float REVEAL_DART_DURATION = 15f;
        public const float REVEAL_DART_RADIUS = 20f;

        // Info Tower
        public const float INFO_TOWER_HACK_TIME = 5f;
        public const float INFO_TOWER_REVEAL_DURATION = 30f;
        public const float INFO_TOWER_REVEAL_RADIUS = 50f;

        // Trap Linking
        public const int MAX_TRAP_CHAIN_LENGTH = 5; // Max traps in a chain
        public const float TRAP_CHAIN_DELAY = 0.2f; // Delay between chain triggers

        // Scoring
        public const int SCORE_KILL = 10;
        public const int SCORE_ASSIST = 5;
        public const int SCORE_STRUCTURE_BUILT = 2;
        public const int SCORE_TRAP_KILL = 15;
        public const int SCORE_CAPTURE = 100;
        public const int SCORE_DEFENSE_TIME_PER_SECOND = 1;

        // Unity 6 Performance Settings
        public const int TARGET_FRAME_RATE = 60;
        public const bool ENABLE_GPU_INSTANCING = true;
        public const bool ENABLE_SRP_BATCHER = true;
        public const bool USE_GPU_RESIDENT_DRAWER = true; // Unity 6 özelliği
        
        // Network Performance (Unity 6)
        public const int MAX_STRUCTURES_PER_TEAM = 150; // GPU Resident Drawer ile daha fazla
        public const float NETWORK_SEND_RATE = 30f; // Hz
    }
}

