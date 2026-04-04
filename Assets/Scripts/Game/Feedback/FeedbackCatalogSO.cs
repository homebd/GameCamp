using UnityEngine;
using GameCamp.Game.Data;

namespace GameCamp.Game.Feedback
{
    [System.Serializable]
    public struct WeaponVfxEntry
    {
        public WeaponType WeaponType;
        public ParticleSystem VfxPrefab;
    }

    [CreateAssetMenu(fileName = "FeedbackCatalogSO", menuName = "GameCamp/Data/FeedbackCatalogSO")]
    public class FeedbackCatalogSO : ScriptableObject
    {
        [field: Header("Damage Text")]
        [field: SerializeField] public DamageTextPopup DamageTextPrefab { get; private set; }
        [field: SerializeField] public int DamageTextPrewarm { get; private set; } = 24;
        [field: SerializeField] public float DamageTextLifetime { get; private set; } = 0.65f;
        [field: SerializeField] public float DamageTextRiseSpeed { get; private set; } = 0.8f;
        [field: SerializeField] public float DamageTextRandomX { get; private set; } = 0.14f;
        [field: SerializeField] public Color DamageTextColor { get; private set; } = Color.white;

        [field: Header("Weapon Vfx")]
        [field: SerializeField] public WeaponVfxEntry[] WeaponVfxEntries { get; private set; }
    }
}

