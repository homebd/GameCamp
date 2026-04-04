using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Data;
using GameCamp.Game.Player;
using UnityEngine;

namespace GameCamp.Game.Weapons
{
    public readonly struct PlayerWeaponContext
    {
        public readonly PlayerController Player;
        public readonly PlayerStatRuntime Stats;
        public readonly ProjectilePool ProjectilePool;
        public readonly Transform DefaultMuzzle;

        public PlayerWeaponContext(
            PlayerController player,
            PlayerStatRuntime stats,
            ProjectilePool projectilePool,
            Transform defaultMuzzle)
        {
            Player = player;
            Stats = stats;
            ProjectilePool = projectilePool;
            DefaultMuzzle = defaultMuzzle;
        }
    }

    public abstract class WeaponModuleBase
    {
        protected PlayerWeaponContext Context;

        public WeaponDataSO WeaponData { get; }

        protected WeaponModuleBase(WeaponDataSO weaponData)
        {
            WeaponData = weaponData;
        }

        public void Initialize(in PlayerWeaponContext context)
        {
            Context = context;
            OnInitialized();
        }

        public void Tick(float deltaTime)
        {
            OnTick(deltaTime);
        }

        protected virtual void OnInitialized()
        {
        }

        protected abstract void OnTick(float deltaTime);
    }
}
