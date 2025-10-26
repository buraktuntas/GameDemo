using UnityEngine;

namespace TacticalCombat.Core
{
    [CreateAssetMenu(fileName = "LayerConfig", menuName = "Tactical Combat/Layer Config")]
    public class LayerConfig : ScriptableObject
    {
        public LayerMask placementSurface;
        public LayerMask obstacleMask;
        public LayerMask structureLayer;
        public LayerMask buildObstructionMask;
        public LayerMask playerMask;
        public LayerMask projectileHitMask;
    }

    public static class LayerConfigProvider
    {
        private static LayerConfig _instance;
        public static LayerConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LayerConfig>("LayerConfig");
                }
                return _instance;
            }
        }
    }
}

