using GameCamp.Game.Data;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Feedback
{
    [RequireComponent(typeof(SnakeSegmentRuntime))]
    public class SegmentCombatFeedbackBridge : MonoBehaviour
    {
        [SerializeField] private SnakeSegmentRuntime segment;
        [SerializeField] private bool showDamageText = true;
        [SerializeField] private bool showHitVfx = true;

        private void Awake()
        {
            if (segment == null)
            {
                segment = GetComponent<SnakeSegmentRuntime>();
            }
        }

        private void OnEnable()
        {
            if (segment == null)
            {
                return;
            }

            segment.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (segment == null)
            {
                return;
            }

            segment.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(SnakeSegmentRuntime _, float damage, Vector3 worldPos, WeaponType sourceWeaponType, float vfxScaleMultiplier)
        {
            if (showDamageText)
            {
                FeedbackSystem.Instance?.SpawnDamageText(Mathf.CeilToInt(damage), worldPos);
            }

            if (showHitVfx)
            {
                if (sourceWeaponType != WeaponType.Rifle)
                {
                    return;
                }

                FeedbackSystem.Instance?.SpawnWeaponVfx(sourceWeaponType, worldPos, vfxScaleMultiplier);
            }
        }
    }
}
