using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Data;
using GameCamp.Game.Audio;
using UnityEngine;

namespace GameCamp.Game.Weapons
{
    public sealed class WeaponModuleLaser : WeaponModuleBase
    {
        private const float LaserTickInterval = 0.2f;
        private float fireCooldown;

        public WeaponModuleLaser(WeaponDataSO weaponData) : base(weaponData)
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
            float castInterval = 1f / attackRate;

            while (fireCooldown <= 0f)
            {
                FireOnce();
                fireCooldown += castInterval;
            }
        }

        private void FireOnce()
        {
            Transform muzzle = Context.DefaultMuzzle;
            if (muzzle == null)
            {
                return;
            }

            float damageMultiplier = Context.Stats != null ? Context.Stats.GetDamageMultiplier(WeaponData.WeaponKind) : 1f;
            float projectileScaleMul = Context.Stats != null ? Context.Stats.GetProjectileScaleMultiplier(WeaponData.WeaponKind) : 1f;
            float lifetimeMul = Context.Stats != null ? Context.Stats.GetProjectileLifetimeMultiplier(WeaponData.WeaponKind) : 1f;

            float finalDamage = Mathf.Max(0f, WeaponData.BaseDamage * damageMultiplier);
            float lifetime = Mathf.Max(0.05f, WeaponData.ProjectileLifetime * lifetimeMul);
            float tickInterval = LaserTickInterval;

            LaserPulse laser = Context.ProjectilePool.Spawn<LaserPulse>(WeaponType.Laser, muzzle.position, Quaternion.identity, lifetime);
            if (laser == null)
            {
                return;
            }

            laser.Initialize(muzzle, finalDamage, tickInterval, projectileScaleMul);
            AudioSystem.Instance?.PlaySfxAt(GameAudioCueId.ShootLaser, muzzle.position);
        }
    }
}
