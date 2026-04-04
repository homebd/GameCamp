using System.Collections.Generic;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Player
{
    [System.Serializable]
    public class PlayerStatRuntime
    {
        [SerializeField] private float baseCommonDamageMultiplier = 1f;
        [SerializeField] private float baseCommonAttackSpeedMultiplier = 1f;
        [SerializeField] private float baseProjectileScaleMultiplier = 1f;
        [SerializeField] private float baseProjectileLifetimeMultiplier = 1f;
        [SerializeField] private float baseProjectileCount = 0f;
        [SerializeField] private float baseProjectilePierce = 0f;

        private readonly List<PlayerStatModifierRuntime> commonModifiers = new();
        private readonly List<WeaponStatModifierRuntime> weaponModifiers = new();
        private int nextModifierId = 1;

        public void Tick(float deltaTime)
        {
            TickCommon(deltaTime);
            TickWeapon(deltaTime);
        }

        public int AddCommonModifier(PlayerStatModifier modifier)
        {
            int id = nextModifierId++;
            commonModifiers.Add(new PlayerStatModifierRuntime(id, modifier));
            return id;
        }

        public int AddWeaponModifier(WeaponStatModifier modifier)
        {
            int id = nextModifierId++;
            weaponModifiers.Add(new WeaponStatModifierRuntime(id, modifier));
            return id;
        }

        public int AddModifier(PlayerStatModifier modifier)
        {
            return AddCommonModifier(modifier);
        }

        public bool RemoveModifier(int modifierId)
        {
            int commonIndex = commonModifiers.FindIndex(m => m.Id == modifierId);
            if (commonIndex >= 0)
            {
                commonModifiers.RemoveAt(commonIndex);
                return true;
            }

            int weaponIndex = weaponModifiers.FindIndex(m => m.Id == modifierId);
            if (weaponIndex >= 0)
            {
                weaponModifiers.RemoveAt(weaponIndex);
                return true;
            }

            return false;
        }

        public void ClearAllModifiers()
        {
            commonModifiers.Clear();
            weaponModifiers.Clear();
        }

        public float GetDamageMultiplier(WeaponType weaponType)
        {
            return Mathf.Max(0f, EvaluateCombined(PlayerStatType.DamageMultiplier, weaponType, baseCommonDamageMultiplier));
        }

        public float GetAttackSpeedMultiplier(WeaponType weaponType)
        {
            return Mathf.Max(0.01f, EvaluateCombined(PlayerStatType.AttackSpeedMultiplier, weaponType, baseCommonAttackSpeedMultiplier));
        }

        public float GetProjectileScaleMultiplier(WeaponType weaponType)
        {
            return Mathf.Max(0.05f, EvaluateCombined(PlayerStatType.ProjectileScaleMultiplier, weaponType, baseProjectileScaleMultiplier));
        }

        public float GetProjectileLifetimeMultiplier(WeaponType weaponType)
        {
            return Mathf.Max(0.05f, EvaluateCombined(PlayerStatType.ProjectileLifetimeMultiplier, weaponType, baseProjectileLifetimeMultiplier));
        }

        public int GetProjectileCount(WeaponType weaponType)
        {
            float value = EvaluateCombined(PlayerStatType.ProjectileCount, weaponType, baseProjectileCount);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }

        public int GetProjectilePierce(WeaponType weaponType)
        {
            float value = EvaluateCombined(PlayerStatType.ProjectilePierce, weaponType, baseProjectilePierce);
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }

        private void TickCommon(float deltaTime)
        {
            if (commonModifiers.Count == 0)
            {
                return;
            }

            for (int i = commonModifiers.Count - 1; i >= 0; i--)
            {
                PlayerStatModifierRuntime runtime = commonModifiers[i];
                if (!runtime.Tick(deltaTime))
                {
                    commonModifiers.RemoveAt(i);
                    continue;
                }

                commonModifiers[i] = runtime;
            }
        }

        private void TickWeapon(float deltaTime)
        {
            if (weaponModifiers.Count == 0)
            {
                return;
            }

            for (int i = weaponModifiers.Count - 1; i >= 0; i--)
            {
                WeaponStatModifierRuntime runtime = weaponModifiers[i];
                if (!runtime.Tick(deltaTime))
                {
                    weaponModifiers.RemoveAt(i);
                    continue;
                }

                weaponModifiers[i] = runtime;
            }
        }

        private float EvaluateCombined(PlayerStatType statType, WeaponType weaponType, float baseValue)
        {
            float additive = 0f;
            float multiplier = 1f;

            for (int i = 0; i < commonModifiers.Count; i++)
            {
                if (commonModifiers[i].Modifier.StatType != statType)
                {
                    continue;
                }

                additive += commonModifiers[i].Modifier.Additive;
                multiplier *= commonModifiers[i].Modifier.Multiplicative;
            }

            for (int i = 0; i < weaponModifiers.Count; i++)
            {
                WeaponStatModifier mod = weaponModifiers[i].Modifier;
                if (mod.StatType != statType || (mod.WeaponType != weaponType && mod.WeaponType != WeaponType.Common))
                {
                    continue;
                }

                additive += mod.Additive;
                multiplier *= mod.Multiplicative;
            }

            return (baseValue + additive) * multiplier;
        }
    }
}
