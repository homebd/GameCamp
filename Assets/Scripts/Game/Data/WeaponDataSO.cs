using UnityEngine;

namespace GameCamp.Game.Data
{
    public enum WeaponType
    {
        Common = 0,
        Rifle = 1,
        Laser = 2,
        Missile = 3,
    }

    [CreateAssetMenu(fileName = "WeaponDataSO", menuName = "GameCamp/Data/WeaponData")]
    public class WeaponDataSO : ScriptableObject
    {
        [field: Header("Identity")]
        [field: SerializeField] public int WeaponId { get; private set; }
        [field: SerializeField] public WeaponType WeaponKind { get; private set; }

        [field: Header("Presentation")]
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public Color SignatureColor { get; private set; } = Color.white;
        [field: SerializeField] public Sprite WeaponSprite { get; private set; }

        [field: Header("Combat")]
        [field: SerializeField] public float BaseDamage { get; private set; } = 1f;
        [field: SerializeField] public float BaseAttackRate { get; private set; } = 4f;
        [field: SerializeField] public float ProjectileSpeed { get; private set; } = 12f;
        [field: SerializeField] public float ProjectileLifetime { get; private set; } = 3f;
    }
}
