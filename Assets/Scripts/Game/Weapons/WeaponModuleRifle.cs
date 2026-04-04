using GameCamp.Game.Audio;
using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Weapons
{
    public sealed class WeaponModuleRifle : WeaponModuleBase
    {
        private const float MultiShotSideSpacing = 0.22f;

        private float fireCooldown;

        public WeaponModuleRifle(WeaponDataSO weaponData) : base(weaponData)
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
            Transform muzzle = Context.DefaultMuzzle;
            if (muzzle == null || WeaponData == null)
            {
                return;
            }

            float damageMultiplier = Context.Stats != null ? Context.Stats.GetDamageMultiplier(WeaponData.WeaponKind) : 1f;
            int shotCount = 1 + (Context.Stats != null ? Context.Stats.GetProjectileCount(WeaponData.WeaponKind) : 0);
            int extraPierce = Context.Stats != null ? Context.Stats.GetProjectilePierce(WeaponData.WeaponKind) : 0;
            float lifetimeMul = Context.Stats != null ? Context.Stats.GetProjectileLifetimeMultiplier(WeaponData.WeaponKind) : 1f;
            float projectileScaleMul = Context.Stats != null ? Context.Stats.GetProjectileScaleMultiplier(WeaponData.WeaponKind) : 1f;

            float finalDamage = Mathf.Max(0f, WeaponData.BaseDamage * damageMultiplier);
            float lifetime = Mathf.Max(0.05f, WeaponData.ProjectileLifetime * lifetimeMul);

            float center = (shotCount - 1) * 0.5f;
            Vector2 shootDir = Vector2.up;
            Vector2 sideDir = Vector2.right;

            bool spawnedAnyProjectile = false;
            for (int i = 0; i < shotCount; i++)
            {
                float sideOffset = (i - center) * MultiShotSideSpacing;
                Vector3 spawnPos = muzzle.position + (Vector3)(sideDir * sideOffset);

                Bullet bullet = Context.ProjectilePool.Spawn<Bullet>(WeaponType.Rifle, spawnPos, Quaternion.identity, lifetime);
                if (bullet == null)
                {
                    continue;
                }

                bullet.Initialize(shootDir, WeaponData.ProjectileSpeed, finalDamage, extraPierce, projectileScaleMul);
                spawnedAnyProjectile = true;
            }

            if (spawnedAnyProjectile)
            {
                AudioSystem.Instance?.PlaySfxAt(GameAudioCueId.ShootRifle, muzzle.position);
            }
        }
    }
}
