using GameCamp.Game.Audio;
using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Data;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Weapons
{
    public sealed class WeaponModuleMissile : WeaponModuleBase
    {
        private const float MultiShotSideSpacing = 0.3f;
        private const float ExplosionRadius = 0.9f;

        private float fireCooldown;

        public WeaponModuleMissile(WeaponDataSO weaponData) : base(weaponData)
        {
        }

        protected override void OnInitialized()
        {
            fireCooldown = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            if (Context.ProjectilePool == null || WeaponData == null)
            {
                return;
            }

            float attackSpeedMultiplier = Context.Stats != null ? Context.Stats.GetAttackSpeedMultiplier(WeaponData.WeaponKind) : 1f;
            float attackRate = Mathf.Max(0.01f, WeaponData.BaseAttackRate * attackSpeedMultiplier);

            fireCooldown -= deltaTime;
            float interval = 1f / attackRate;

            while (fireCooldown <= 0f)
            {
                FireOnce();
                fireCooldown += interval;
            }
        }

        private void FireOnce()
        {
            if (!SnakeSegmentRuntime.TryGetLowestHpTarget(out SnakeSegmentRuntime target))
            {
                return;
            }

            Transform muzzle = Context.DefaultMuzzle;
            if (muzzle == null)
            {
                return;
            }

            float damageMultiplier = Context.Stats != null ? Context.Stats.GetDamageMultiplier(WeaponData.WeaponKind) : 1f;
            int shotCount = 1 + (Context.Stats != null ? Context.Stats.GetProjectileCount(WeaponData.WeaponKind) : 0);
            float lifetimeMul = Context.Stats != null ? Context.Stats.GetProjectileLifetimeMultiplier(WeaponData.WeaponKind) : 1f;
            float projectileScaleMul = Context.Stats != null ? Context.Stats.GetProjectileScaleMultiplier(WeaponData.WeaponKind) : 1f;
            float finalDamage = Mathf.Max(0f, WeaponData.BaseDamage * damageMultiplier);
            float lifetime = Mathf.Max(0.05f, WeaponData.ProjectileLifetime * lifetimeMul);

            float center = (shotCount - 1) * 0.5f;
            Vector2 sideDir = Vector2.right;
            bool spawnedAny = false;

            for (int i = 0; i < shotCount; i++)
            {
                float sideOffset = (i - center) * MultiShotSideSpacing;
                Vector3 spawnPos = muzzle.position + (Vector3)(sideDir * sideOffset);

                Missile missile = Context.ProjectilePool.Spawn<Missile>(WeaponType.Missile, spawnPos, Quaternion.identity, lifetime);
                if (missile == null)
                {
                    continue;
                }

                missile.Initialize(target, WeaponData.ProjectileSpeed, finalDamage, ExplosionRadius, projectileScaleMul);
                spawnedAny = true;
            }

            if (spawnedAny)
            {
                AudioSystem.Instance?.PlaySfxAt(GameAudioCueId.ShootMissile, muzzle.position);
            }
        }
    }
}
