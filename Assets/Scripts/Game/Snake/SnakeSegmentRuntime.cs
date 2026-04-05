using System;
using System.Collections.Generic;
using GameCamp.Game.Data;
using GameCamp.Game.Path;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCamp.Game.Snake
{
    public class SnakeSegmentRuntime : MonoBehaviour
    {
        [Serializable]
        private struct RewardChestVisualEntry
        {
            public int RewardLevel;
            public Sprite ChestSprite;
        }

        private static readonly List<SnakeSegmentRuntime> ActiveSegments = new();

        public int SegmentId { get; private set; }
        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public int RewardLevel { get; private set; }
        public bool HasReward => RewardLevel > 0;

        [SerializeField] private SnakeSegmentVisual visual;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image chestVisual;
        [SerializeField] private RewardChestVisualEntry[] chestVisualByRewardLevel = Array.Empty<RewardChestVisualEntry>();
        [SerializeField] private float positionLerpSpeed = 20f;

        private SnakeController owner;
        private bool isDead;
        private bool hasPoseInitialized;

        public event Action<SnakeSegmentRuntime, float, Vector3, WeaponType, float> OnDamaged;
        public event Action<SnakeSegmentRuntime, Vector3> OnDestroyed;

        public float RecommendedNextSegmentSpacing
        {
            get
            {
                EnsureVisual();
                return visual != null ? visual.RecommendedNextSegmentSpacing : 0f;
            }
        }

        private void OnEnable()
        {
            ActiveSegments.Add(this);
        }

        private void OnDisable()
        {
            ActiveSegments.Remove(this);
        }

        public static bool TryGetLowestHpTarget(out SnakeSegmentRuntime target)
        {
            target = null;
            float lowestHp = float.MaxValue;

            for (int i = ActiveSegments.Count - 1; i >= 0; i--)
            {
                SnakeSegmentRuntime segment = ActiveSegments[i];
                if (segment == null)
                {
                    ActiveSegments.RemoveAt(i);
                    continue;
                }

                if (segment.isDead || segment.CurrentHp <= 0f)
                {
                    continue;
                }

                if (segment.CurrentHp >= lowestHp)
                {
                    continue;
                }

                lowestHp = segment.CurrentHp;
                target = segment;
            }

            return target != null;
        }

        public void Initialize(int segmentId, float hp, int rewardLevel, SnakeController snakeOwner)
        {
            SegmentId = segmentId;
            MaxHp = Mathf.Max(0.1f, hp);
            CurrentHp = MaxHp;
            RewardLevel = Mathf.Max(0, rewardLevel);
            owner = snakeOwner;
            isDead = false;
            hasPoseInitialized = false;

            EnsureVisual();
            visual?.ResetSmoothing();
            visual?.SetSpawnOrderIndex(Mathf.Max(0, segmentId - 1));
            visual?.PlaySpawnFadeIn();
            UpdateHpText();
            UpdateChestVisual();
        }

        public void ApplyDamage(float amount)
        {
            ApplyDamage(amount, transform.position, WeaponType.Common, 1f);
        }

        public void ApplyDamage(float amount, Vector3 hitWorldPosition)
        {
            ApplyDamage(amount, hitWorldPosition, WeaponType.Common, 1f);
        }

        public void ApplyDamage(float amount, Vector3 hitWorldPosition, WeaponType sourceWeaponType, float vfxScaleMultiplier)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            visual?.PlayHitFeedback();

            float applied = Mathf.Min(CurrentHp, amount);
            CurrentHp -= amount;

            if (applied > 0f)
            {
                OnDamaged?.Invoke(this, applied, hitWorldPosition, sourceWeaponType, Mathf.Max(0.05f, vfxScaleMultiplier));
            }

            if (CurrentHp > 0f)
            {
                UpdateHpText();
                return;
            }

            CurrentHp = 0f;
            UpdateHpText();
            isDead = true;

            OnDestroyed?.Invoke(this, hitWorldPosition);
            owner?.HandleSegmentDestroyed(this, RewardLevel);
        }

        public void SetPathPose(PathCurveEvaluator pathCurve, float centerDistance)
        {
            if (pathCurve == null || !pathCurve.Evaluate(centerDistance, out Vector2 position, out _))
            {
                return;
            }

            bool snapNow = !hasPoseInitialized;
            float posT = snapNow ? 1f : ComputeLerpFactor(positionLerpSpeed);

            Vector3 targetPos = new Vector3(position.x, position.y, transform.position.z);
            transform.position = posT >= 1f ? targetPos : Vector3.Lerp(transform.position, targetPos, posT);

            visual?.ApplyPath(pathCurve, centerDistance, snapNow);
            hasPoseInitialized = true;
        }

        public void SetEnrageSprite(Sprite sprite)
        {
            EnsureVisual();
            visual?.SetEnrageSprite(sprite);
        }

        private void EnsureVisual()
        {
            if (visual == null)
            {
                visual = GetComponentInChildren<SnakeSegmentVisual>();
            }
        }

        private void UpdateHpText()
        {
            if (hpText == null)
            {
                return;
            }

            hpText.text = Mathf.CeilToInt(CurrentHp).ToString();
        }

        private void UpdateChestVisual()
        {
            if (chestVisual != null)
            {
                chestVisual.gameObject.SetActive(HasReward);
            }

            if (!HasReward || chestVisual == null)
            {
                return;
            }

            chestVisual.sprite = ResolveChestSpriteForLevel(RewardLevel);
        }

        private Sprite ResolveChestSpriteForLevel(int level)
        {
            Sprite fallback = chestVisual.sprite;

            if (chestVisualByRewardLevel == null || chestVisualByRewardLevel.Length == 0)
            {
                return fallback;
            }

            for (int i = 0; i < chestVisualByRewardLevel.Length; i++)
            {
                RewardChestVisualEntry entry = chestVisualByRewardLevel[i];
                if (entry.RewardLevel == level && entry.ChestSprite != null)
                {
                    return entry.ChestSprite;
                }
            }

            int bestLevel = int.MinValue;
            for (int i = 0; i < chestVisualByRewardLevel.Length; i++)
            {
                RewardChestVisualEntry entry = chestVisualByRewardLevel[i];
                if (entry.ChestSprite == null)
                {
                    continue;
                }

                if (entry.RewardLevel <= level && entry.RewardLevel >= bestLevel)
                {
                    bestLevel = entry.RewardLevel;
                    fallback = entry.ChestSprite;
                }
            }

            return fallback;
        }

        private static float ComputeLerpFactor(float speed)
        {
            if (speed <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }
    }
}
