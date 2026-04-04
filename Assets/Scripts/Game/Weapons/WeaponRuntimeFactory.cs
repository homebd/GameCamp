using GameCamp.Game.Data;

namespace GameCamp.Game.Weapons
{
    public static class WeaponRuntimeFactory
    {
        public static WeaponModuleBase Create(WeaponDataSO weaponData)
        {
            if (weaponData == null)
            {
                return null;
            }

            return weaponData.WeaponKind switch
            {
                WeaponType.Rifle => new WeaponModuleRifle(weaponData),
                WeaponType.Laser => new WeaponModuleLaser(weaponData),
                WeaponType.Missile => new WeaponModuleMissile(weaponData),
                _ => null,
            };
        }
    }
}
