using System;
using System.Collections.Generic;
using GameCamp.Game.Path;
using UnityEngine;

namespace GameCamp.Game.Snake
{
    [Serializable]
    public struct RewardLevelChance
    {
        public int RewardLevel;
        [Range(0f, 1f)] public float Chance;
    }

    public class SnakeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeSegmentRuntime segmentPrefab;
        [SerializeField] private Transform segmentRoot;
        [SerializeField] private Transform headVisual;
        [SerializeField] private SpriteRenderer headSpriteRenderer;

        [Header("Path Sampling")]
        [SerializeField] private int linearSubdivisions = 4;
        [SerializeField] private int bezierSubdivisions = 8;

        [Header("Snake Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float snakeSpeed = 2f;
        [SerializeField] private bool autoSpacingFromVisual = true;
        [SerializeField] private float segmentSpacing = 0.8f;
        [SerializeField] private int maxSegmentCount = 20;
        [SerializeField] private float segmentHp = 3f;

        [Header("Segment HP Scaling")]
        [SerializeField] private bool scaleHpTowardTail = true;
        [SerializeField] private float hpIncreasePerSegment = 0.02f;
        [SerializeField] private float maxHpMultiplier = 2f;

        [Header("Head Visual")]
        [SerializeField] private bool autoHeadOffset = true;
        [SerializeField] private float headDistanceOffset = 0.3f;
        [SerializeField] private float headPositionLerpSpeed = 20f;

        private readonly List<SnakeSegmentRuntime> segments = new();
        private readonly List<float> enrageTriggerProgresses = new();
        private readonly PathCurveEvaluator pathCurve = new();
        private RewardLevelChance[] rewardLevelChances = Array.Empty<RewardLevelChance>();

        private bool isRunning;
        private float headDistance;
        private float nextSpawnHeadDistance;
        private int spawnedCount;
        private int nextSegmentId = 1;
        private float resolvedSegmentSpacing;
        private bool hasHeadPoseInitialized;
        private float baseSnakeSpeed;
        private float enrageSpeedMultiplier = 1f;
        private float enrageDurationSeconds;
        private float enrageRemainingSeconds;
        private int nextEnrageTriggerIndex;
        private Sprite enrageHeadSprite;
        private Sprite baseHeadSprite;

        public float PathProgress01 => pathCurve.TotalLength > 0.0001f
            ? Mathf.Clamp01(headDistance / pathCurve.TotalLength)
            : 0f;
        public float SegmentProgress01 => maxSegmentCount > 0
            ? Mathf.Clamp01((float)spawnedCount / maxSegmentCount)
            : 0f;

        public event Action<bool> OnSnakeFinished;
        public event Action<SnakeSegmentRuntime, int> OnSegmentDestroyed;

        private void Awake()
        {
            pathCurve.SetSamplingQuality(linearSubdivisions, bezierSubdivisions);
        }

        private void OnValidate()
        {
            if (linearSubdivisions < 1)
            {
                linearSubdivisions = 1;
            }

            if (bezierSubdivisions < 2)
            {
                bezierSubdivisions = 2;
            }

            hpIncreasePerSegment = Mathf.Max(0f, hpIncreasePerSegment);
            maxHpMultiplier = Mathf.Max(1f, maxHpMultiplier);

            pathCurve.SetSamplingQuality(linearSubdivisions, bezierSubdivisions);
        }

        private void Start()
        {
            if (autoStart)
            {
                Begin();
            }
        }

        private void Update()
        {
            if (!isRunning || !pathCurve.IsValid)
            {
                return;
            }

            float dt = Time.deltaTime;
            UpdateEnrageState(dt);
            float moveSpeed = baseSnakeSpeed * (IsEnraged() ? enrageSpeedMultiplier : 1f);
            headDistance += moveSpeed * dt;

            while (spawnedCount < maxSegmentCount && headDistance >= nextSpawnHeadDistance)
            {
                SpawnNextSegment();
                nextSpawnHeadDistance += resolvedSegmentSpacing;
            }

            UpdateSegmentPoses();
            UpdateHeadVisualPose();

            if (headDistance >= pathCurve.TotalLength)
            {
                isRunning = false;
                OnSnakeFinished?.Invoke(false);
            }
            else if (spawnedCount >= maxSegmentCount && segments.Count == 0)
            {
                isRunning = false;
                DestroyHeadVisual();
                OnSnakeFinished?.Invoke(true);
            }
        }

        public void ApplyStageSettings(
            float moveSpeed,
            float baseHp,
            int stageMaxSegmentCount,
            bool stageScaleHpTowardTail,
            float stageHpIncreasePerSegment,
            float stageMaxHpMultiplier,
            RewardLevelChance[] stageRewardLevelChances,
            IReadOnlyList<Vector2> stagePathPoints,
            bool useBezierPath,
            float stageEnrageSpeedMultiplier,
            float stageEnrageDurationSeconds,
            IReadOnlyList<float> stageEnrageTriggerProgresses01,
            Sprite stageEnrageHeadSprite)
        {
            baseSnakeSpeed = Mathf.Max(0.01f, moveSpeed);
            snakeSpeed = baseSnakeSpeed;
            segmentHp = Mathf.Max(0.1f, baseHp);
            maxSegmentCount = Mathf.Max(1, stageMaxSegmentCount);

            scaleHpTowardTail = stageScaleHpTowardTail;
            hpIncreasePerSegment = Mathf.Max(0f, stageHpIncreasePerSegment);
            maxHpMultiplier = Mathf.Max(1f, stageMaxHpMultiplier);

            pathCurve.SetSamplingQuality(linearSubdivisions, bezierSubdivisions);
            pathCurve.SetUseBezierSmoothing(useBezierPath);
            pathCurve.SetWorldPath(stagePathPoints);
            SetupEnrage(stageEnrageSpeedMultiplier, stageEnrageDurationSeconds, stageEnrageTriggerProgresses01, stageEnrageHeadSprite);

            if (stageRewardLevelChances == null || stageRewardLevelChances.Length == 0)
            {
                rewardLevelChances = Array.Empty<RewardLevelChance>();
                return;
            }

            rewardLevelChances = new RewardLevelChance[stageRewardLevelChances.Length];
            Array.Copy(stageRewardLevelChances, rewardLevelChances, stageRewardLevelChances.Length);
        }

        public void Begin()
        {
            if (segmentPrefab == null || !pathCurve.IsValid)
            {
                Debug.LogWarning($"{nameof(SnakeController)} setup incomplete on {name}.");
                return;
            }

            ClearSegments();

            resolvedSegmentSpacing = ResolveSegmentSpacing();
            isRunning = true;
            headDistance = 0f;
            nextSpawnHeadDistance = 0f;
            spawnedCount = 0;
            nextSegmentId = 1;
            hasHeadPoseInitialized = false;
            enrageRemainingSeconds = 0f;
            nextEnrageTriggerIndex = 0;

            if (headSpriteRenderer != null)
            {
                baseHeadSprite = headSpriteRenderer.sprite;
                headSpriteRenderer.flipX = false;
            }

            ApplyEnrageVisual(false);
        }

        public void HandleSegmentDestroyed(SnakeSegmentRuntime segment, int rewardLevel)
        {
            if (segment == null)
            {
                return;
            }

            int removedIndex = segments.IndexOf(segment);
            if (removedIndex >= 0)
            {
                segments.RemoveAt(removedIndex);
                headDistance = Mathf.Max(0f, headDistance - resolvedSegmentSpacing);
                nextSpawnHeadDistance = Mathf.Max(0f, nextSpawnHeadDistance - resolvedSegmentSpacing);
            }

            OnSegmentDestroyed?.Invoke(segment, rewardLevel);
            Destroy(segment.gameObject);
        }

        private void SpawnNextSegment()
        {
            Transform parent = segmentRoot != null ? segmentRoot : transform;
            SnakeSegmentRuntime segment = Instantiate(segmentPrefab, parent);

            int rewardLevel = ResolveRewardLevelForSpawn();
            float hpForThisSegment = ResolveSegmentHpForSpawn();
            segment.Initialize(nextSegmentId++, hpForThisSegment, rewardLevel, this);

            segments.Add(segment);
            spawnedCount++;
        }

        private float ResolveSegmentHpForSpawn()
        {
            float baseHp = Mathf.Max(0.1f, segmentHp);
            if (!scaleHpTowardTail)
            {
                return baseHp;
            }

            float multiplier = 1f + (spawnedCount * hpIncreasePerSegment);
            multiplier = Mathf.Min(multiplier, Mathf.Max(1f, maxHpMultiplier));
            return baseHp * multiplier;
        }

        private int ResolveRewardLevelForSpawn()
        {
            if (rewardLevelChances == null || rewardLevelChances.Length == 0)
            {
                return 0;
            }

            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            for (int i = 0; i < rewardLevelChances.Length; i++)
            {
                RewardLevelChance entry = rewardLevelChances[i];
                if (entry.RewardLevel <= 0)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, entry.Chance);
                if (roll <= cumulative)
                {
                    return entry.RewardLevel;
                }

                if (cumulative >= 1f)
                {
                    break;
                }
            }

            return 0;
        }

        private void UpdateSegmentPoses()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                float segmentDistance = headDistance - (i * resolvedSegmentSpacing);
                segments[i].SetPathPose(pathCurve, segmentDistance);
            }
        }

        private void UpdateHeadVisualPose()
        {
            if (headVisual == null || segments.Count == 0)
            {
                return;
            }

            float offset = autoHeadOffset ? resolvedSegmentSpacing * 0.5f : headDistanceOffset;
            float d = headDistance + Mathf.Max(0f, offset);

            if (!pathCurve.Evaluate(d, out Vector2 pos, out Vector2 tangent))
            {
                return;
            }

            bool snapNow = !hasHeadPoseInitialized;
            float posT = snapNow ? 1f : ComputeLerpFactor(headPositionLerpSpeed);

            Vector3 targetPos = new Vector3(pos.x, pos.y, headVisual.position.z);
            headVisual.position = posT >= 1f ? targetPos : Vector3.Lerp(headVisual.position, targetPos, posT);
            if (headSpriteRenderer != null && Mathf.Abs(tangent.x) > 0.0001f)
            {
                headSpriteRenderer.flipX = tangent.x < 0f;
            }

            hasHeadPoseInitialized = true;
        }

        private void DestroyHeadVisual()
        {
            if (headVisual == null)
            {
                return;
            }

            Destroy(headVisual.gameObject);
            headVisual = null;
        }

        private float ResolveSegmentSpacing()
        {
            float fallback = Mathf.Max(0.05f, segmentSpacing);
            if (!autoSpacingFromVisual || segmentPrefab == null)
            {
                return fallback;
            }

            float fromVisual = segmentPrefab.RecommendedNextSegmentSpacing;
            return fromVisual > 0.01f ? fromVisual : fallback;
        }

        private void ClearSegments()
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i] != null)
                {
                    Destroy(segments[i].gameObject);
                }
            }

            segments.Clear();
        }

        private static float ComputeLerpFactor(float speed)
        {
            if (speed <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }

        private void SetupEnrage(float speedMultiplier, float durationSeconds, IReadOnlyList<float> triggerProgresses01, Sprite headSprite)
        {
            if (triggerProgresses01 == null)
            {
                throw new ArgumentNullException(nameof(triggerProgresses01));
            }

            enrageSpeedMultiplier = Mathf.Max(1f, speedMultiplier);
            enrageDurationSeconds = Mathf.Max(0f, durationSeconds);
            enrageHeadSprite = headSprite;
            enrageTriggerProgresses.Clear();

            for (int i = 0; i < triggerProgresses01.Count; i++)
            {
                enrageTriggerProgresses.Add(Mathf.Clamp01(triggerProgresses01[i]));
            }

            enrageTriggerProgresses.Sort();
            nextEnrageTriggerIndex = 0;
            enrageRemainingSeconds = 0f;
        }

        private void UpdateEnrageState(float deltaTime)
        {
            bool wasEnraged = IsEnraged();

            if (enrageRemainingSeconds > 0f)
            {
                enrageRemainingSeconds = Mathf.Max(0f, enrageRemainingSeconds - deltaTime);
            }

            float progress01 = SegmentProgress01;

            while (nextEnrageTriggerIndex < enrageTriggerProgresses.Count && progress01 >= enrageTriggerProgresses[nextEnrageTriggerIndex])
            {
                if (enrageDurationSeconds > 0f)
                {
                    enrageRemainingSeconds = enrageDurationSeconds;
                }

                nextEnrageTriggerIndex++;
            }

            bool isEnragedNow = IsEnraged();
            if (wasEnraged != isEnragedNow)
            {
                ApplyEnrageVisual(isEnragedNow);
            }
        }

        private bool IsEnraged()
        {
            return enrageRemainingSeconds > 0f;
        }

        private void ApplyEnrageVisual(bool enraged)
        {
            if (headSpriteRenderer == null)
            {
                return;
            }

            headSpriteRenderer.sprite = enraged ? enrageHeadSprite : baseHeadSprite;
        }
    }
}

