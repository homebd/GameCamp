using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCamp.Game.Data
{
    [Serializable]
    public struct RewardLevelChanceData
    {
        [field: SerializeField] public int RewardLevel { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float Chance { get; private set; }
    }

    [CreateAssetMenu(fileName = "StageConfigSO", menuName = "GameCamp/Data/StageConfigSO")]
    public class StageConfigSO : ScriptableObject
    {
        [Header("Enemy")]
        [field: SerializeField] public float EnemyBaseHp { get; private set; } = 3f;
        [field: SerializeField] public float EnemyMoveSpeed { get; private set; } = 2f;
        [field: SerializeField] public int MaxSegmentCount { get; private set; } = 40;

        [Header("Enemy HP Scaling")]
        [field: SerializeField] public bool ScaleHpTowardTail { get; private set; } = true;
        [field: SerializeField] public float HpIncreasePerSegment { get; private set; } = 0.02f;
        [field: SerializeField] public float MaxHpMultiplier { get; private set; } = 2f;

        [Header("Path (World Coordinates)")]
        [field: SerializeField] public bool UseBezierPath { get; private set; } = true;
        [field: SerializeField] public List<Vector2> PathPoints { get; private set; } = new();

        [Header("Enrage")]
        [field: SerializeField] public float EnrageSpeedMultiplier { get; private set; } = 1.5f;
        [field: SerializeField] public float EnrageDurationSeconds { get; private set; } = 3f;
        [field: SerializeField] public List<float> EnrageTriggerProgresses01 { get; private set; } = new();
        [field: FormerlySerializedAs("EnrageSegmentSprite")]
        [field: SerializeField]
        public Sprite EnrageHeadSprite { get; private set; }

        [Header("Reward")]
        [field: SerializeField] public RewardLevelChanceData[] RewardLevelChances { get; private set; } = Array.Empty<RewardLevelChanceData>();

        private void OnValidate()
        {
            EnemyBaseHp = Mathf.Max(0.1f, EnemyBaseHp);
            EnemyMoveSpeed = Mathf.Max(0.01f, EnemyMoveSpeed);
            MaxSegmentCount = Mathf.Max(1, MaxSegmentCount);

            HpIncreasePerSegment = Mathf.Max(0f, HpIncreasePerSegment);
            MaxHpMultiplier = Mathf.Max(1f, MaxHpMultiplier);
            EnrageSpeedMultiplier = Mathf.Max(1f, EnrageSpeedMultiplier);
            EnrageDurationSeconds = Mathf.Max(0f, EnrageDurationSeconds);

            if (EnrageTriggerProgresses01 == null)
            {
                EnrageTriggerProgresses01 = new List<float>();
            }

            for (int i = 0; i < EnrageTriggerProgresses01.Count; i++)
            {
                EnrageTriggerProgresses01[i] = Mathf.Clamp01(EnrageTriggerProgresses01[i]);
            }
        }
    }
}
