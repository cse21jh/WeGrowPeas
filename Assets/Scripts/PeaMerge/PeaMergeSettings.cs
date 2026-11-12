using System;
using UnityEngine;

namespace WeGrowPeas.PeaMerge
{
    [CreateAssetMenu(menuName = "WeGrowPeas/Pea Merge Settings")]
    public sealed class PeaMergeSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Tier
        {
            public string label;
            [Min(0)] public int appearanceIndex;
            [Min(0.1f)] public float radius = 0.35f;
            [Min(0)] public int mergeScore = 2;
            public Sprite bodyIcon;
            public Sprite accessoryIcon;
            public Color accessoryTint = Color.white;
        }

        public GameObject peaPrefab;
        public Tier[] tiers;
        [Header("Board (world units; 100 UI units = 1 world unit)")]
        [Min(2f)] public float halfWidth = 4.45f;
        public float floorY = -6f;
        public float wallTopY = 4.15f;
        public float dangerY = 3f;
        public float dropY = 4.95f;
        [Header("Rules")]
        [Range(1, 4)] public int randomTierCount = 4;
        [Min(0.1f)] public float dropCooldown = 0.55f;
        [Min(0.5f)] public float overflowSeconds = 2f;
        [Min(0.1f)] public float spawnGraceSeconds = 1f;
        public string bestScoreKey = "WeGrowPeas.PeaMerge.BestScore.v1";

        public int RandomTier() => UnityEngine.Random.Range(0, Mathf.Min(randomTierCount, tiers.Length));
    }
}
