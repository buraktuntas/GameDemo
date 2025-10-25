using UnityEngine;
using TacticalCombat.Core;

namespace TacticalCombat.Core
{
    [CreateAssetMenu(fileName = "New Role", menuName = "Tactical Combat/Role Definition")]
    public class RoleDefinition : ScriptableObject
    {
        [Header("Role Info")]
        public RoleId roleId;
        public string roleName;
        [TextArea(3, 5)]
        public string description;

        [Header("Passive Modifiers")]
        public float buildSpeedMultiplier = 1f;
        public float damageResistance = 0f; // 0 to 1
        public float movementSpeedMultiplier = 1f;

        [Header("Active Ability")]
        public string abilityName;
        [TextArea(2, 4)]
        public string abilityDescription;
        public float abilityCooldown = 30f;
        public float abilityDuration = 5f;
        public Sprite abilityIcon;
    }
}



