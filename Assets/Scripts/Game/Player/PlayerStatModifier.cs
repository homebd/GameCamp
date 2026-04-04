using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Player
{
    public enum PlayerStatType
    {
        DamageMultiplier = 0,
        AttackSpeedMultiplier = 1,
        ProjectileScaleMultiplier = 2,
        ProjectileLifetimeMultiplier = 3,
        ProjectileCount = 4,
        ProjectilePierce = 5,
    }

    [System.Serializable]
    public struct PlayerStatModifier
    {
        public PlayerStatType StatType;
        public float Additive;
        public float Multiplicative;
        public float DurationSeconds;

        public bool IsTimed => DurationSeconds > 0f;

        public static PlayerStatModifier Create(PlayerStatType statType, float additive, float multiplicative = 1f, float durationSeconds = 0f)
        {
            return new PlayerStatModifier
            {
                StatType = statType,
                Additive = additive,
                Multiplicative = multiplicative,
                DurationSeconds = durationSeconds,
            };
        }
    }

    [System.Serializable]
    public struct WeaponStatModifier
    {
        public WeaponType WeaponType;
        public PlayerStatType StatType;
        public float Additive;
        public float Multiplicative;
        public float DurationSeconds;

        public bool IsTimed => DurationSeconds > 0f;

        public static WeaponStatModifier Create(WeaponType weaponType, PlayerStatType statType, float additive, float multiplicative = 1f, float durationSeconds = 0f)
        {
            return new WeaponStatModifier
            {
                WeaponType = weaponType,
                StatType = statType,
                Additive = additive,
                Multiplicative = multiplicative,
                DurationSeconds = durationSeconds,
            };
        }
    }

    public struct PlayerStatModifierRuntime
    {
        public int Id;
        public PlayerStatModifier Modifier;
        private float remainingDuration;

        public PlayerStatModifierRuntime(int id, PlayerStatModifier modifier)
        {
            Id = id;
            Modifier = modifier;
            remainingDuration = modifier.DurationSeconds;
        }

        public bool Tick(float deltaTime)
        {
            if (!Modifier.IsTimed)
            {
                return true;
            }

            remainingDuration -= deltaTime;
            return remainingDuration > 0f;
        }
    }

    public struct WeaponStatModifierRuntime
    {
        public int Id;
        public WeaponStatModifier Modifier;
        private float remainingDuration;

        public WeaponStatModifierRuntime(int id, WeaponStatModifier modifier)
        {
            Id = id;
            Modifier = modifier;
            remainingDuration = modifier.DurationSeconds;
        }

        public bool Tick(float deltaTime)
        {
            if (!Modifier.IsTimed)
            {
                return true;
            }

            remainingDuration -= deltaTime;
            return remainingDuration > 0f;
        }
    }
}
