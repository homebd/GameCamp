using System;
using System.Collections.Generic;
using System.Globalization;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Rewards
{
    public enum RewardRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
    }

    public enum RewardEffectType
    {
        DamageMultiplier = 0,
        AttackSpeedMultiplier = 1,
        ProjectileScaleMultiplier = 2,
        ProjectileCount = 3,
        ProjectileLifetimeMultiplier = 4,
        ProjectilePierce = 5,
        UnlockWeapon = 100,
    }

    [Serializable]
    public struct RewardEffectSpec
    {
        public RewardEffectType EffectType;
        public float Value;
        public WeaponType WeaponType;
        public float DurationSeconds;
    }

    [Serializable]
    public sealed class RewardDefinition
    {
        [field: SerializeField] public int RewardId { get; private set; }
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public RewardRarity Rarity { get; private set; }
        [field: SerializeField] public WeaponType Scope { get; private set; }
        [field: SerializeField] public int MaxAcquireCount { get; private set; }
        [field: SerializeField] public RewardEffectSpec Effect { get; private set; }

        public bool IsUnlimited => MaxAcquireCount == 0;

        public static RewardDefinition Create(int rewardId, string name, string description, RewardRarity rarity, WeaponType scope, int maxAcquireCount, RewardEffectSpec effect)
        {
            return new RewardDefinition
            {
                RewardId = rewardId,
                Name = name,
                Description = description,
                Rarity = rarity,
                Scope = scope,
                MaxAcquireCount = Mathf.Max(0, maxAcquireCount),
                Effect = effect,
            };
        }

        public string BuildDescription()
        {
            if (string.IsNullOrEmpty(Description))
            {
                return string.Empty;
            }

            string result = Description;
            result = result.Replace("{value_0}", Effect.Value.ToString("0.###", CultureInfo.InvariantCulture));
            result = result.Replace("{value_0_pct}", (Effect.Value * 100f).ToString("0.#", CultureInfo.InvariantCulture));
            result = result.Replace("{value_0_int}", Mathf.RoundToInt(Effect.Value).ToString(CultureInfo.InvariantCulture));
            return result;
        }
    }

    [Serializable]
    public struct RewardRarityWeight
    {
        [field: SerializeField] public RewardRarity Rarity { get; private set; }
        [field: SerializeField] public float Weight { get; private set; }
    }

    [Serializable]
    public struct RewardLevelRarityWeights
    {
        [field: SerializeField] public int RewardLevel { get; private set; }
        [field: SerializeField] public RewardRarityWeight[] Weights { get; private set; }
    }

    public readonly struct RewardOptionData
    {
        public readonly int RewardId;
        public readonly string Name;
        public readonly string Description;
        public readonly RewardRarity Rarity;
        public readonly WeaponType Scope;
        public readonly WeaponType EffectWeapon;
        public readonly RewardEffectType EffectType;

        public RewardOptionData(int rewardId, string name, string description, RewardRarity rarity, WeaponType scope, WeaponType effectWeapon, RewardEffectType effectType)
        {
            RewardId = rewardId;
            Name = name;
            Description = description;
            Rarity = rarity;
            Scope = scope;
            EffectWeapon = effectWeapon;
            EffectType = effectType;
        }
    }
}
