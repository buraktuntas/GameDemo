namespace TacticalCombat.Core
{
    public static class GameConstants
    {
        // Phase Durations
        public const float BUILD_DURATION = 150f; // 2:30
        public const float COMBAT_DURATION = 480f; // 8:00
        public const float ROUND_END_DURATION = 5f;

        // BO3
        public const int ROUNDS_TO_WIN = 2;
        public const int MAX_ROUNDS = 3;

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

