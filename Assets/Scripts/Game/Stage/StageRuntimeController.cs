using System;
using System.Collections.Generic;
using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Core;
using GameCamp.Game.Data;
using GameCamp.Game.Player;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Stage
{
    public class StageRuntimeController : MonoBehaviour
    {
        [Header("Stage Data")]
        [SerializeField] private List<StageConfigSO> stageConfigs = new();
        [SerializeField] private int startStageIndex;

        [Header("Runtime Prefabs")]
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private SnakeController snakePrefab;

        [Header("Flow")]
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private bool autoStartOnEnable = true;

        private int currentStageIndex = -1;
        private SnakeController snakeController;
        private PlayerController playerInstance;

        public int CurrentStageIndex => currentStageIndex;
        public PlayerController PlayerInstance => playerInstance;
        public SnakeController SnakeInstance => snakeController;

        public event Action<bool> OnStageEnded;

        private void OnEnable()
        {
            StageFlowRuntime.Initialize(stageConfigs != null ? stageConfigs.Count : 1);

            if (autoStartOnEnable)
            {
                int fallbackIndex = Mathf.Clamp(startStageIndex, 0, Mathf.Max(0, stageConfigs.Count - 1));
                int selected = Mathf.Clamp(StageFlowRuntime.SelectedStageIndex, 0, Mathf.Max(0, stageConfigs.Count - 1));
                int index = StageFlowRuntime.CanSelect(selected) ? selected : fallbackIndex;
                StartStageAtIndex(index);
            }
        }

        private void OnDisable()
        {
            if (snakeController != null)
            {
                snakeController.OnSnakeFinished -= HandleSnakeFinished;
            }
        }

        public void StartStage()
        {
            int index = currentStageIndex >= 0 ? currentStageIndex : Mathf.Clamp(startStageIndex, 0, Mathf.Max(0, stageConfigs.Count - 1));
            StartStageAtIndex(index);
        }

        public void StartStageAtIndex(int stageIndex)
        {
            if (!EnsureRuntimeObjects())
            {
                return;
            }

            if (!TryGetStage(stageIndex, out StageConfigSO stage))
            {
                Debug.LogWarning($"{nameof(StageRuntimeController)} has no valid stage at index {stageIndex}.", this);
                return;
            }

            currentStageIndex = stageIndex;
            ApplyStageConfig(stage);
            snakeController.Begin();
        }

        public bool TryAdvanceToNextStage(bool autoStart = true)
        {
            int nextIndex = currentStageIndex + 1;
            if (!TryGetStage(nextIndex, out _))
            {
                return false;
            }

            currentStageIndex = nextIndex;
            if (autoStart)
            {
                StartStageAtIndex(currentStageIndex);
            }

            return true;
        }

        public void SetStage(StageConfigSO stage, bool restartStage = true)
        {
            int index = stageConfigs.IndexOf(stage);
            if (index < 0)
            {
                Debug.LogWarning($"{nameof(StageRuntimeController)} cannot find stage in list.", this);
                return;
            }

            currentStageIndex = index;
            if (restartStage && isActiveAndEnabled)
            {
                StartStageAtIndex(currentStageIndex);
            }
        }

        private bool EnsureRuntimeObjects()
        {
            if (gameFlowController == null)
            {
                Debug.LogError($"{nameof(StageRuntimeController)} requires {nameof(gameFlowController)}.", this);
                return false;
            }

            if (playerPrefab == null)
            {
                Debug.LogError($"{nameof(StageRuntimeController)} requires {nameof(playerPrefab)}.", this);
                return false;
            }

            if (snakePrefab == null)
            {
                Debug.LogError($"{nameof(StageRuntimeController)} requires {nameof(snakePrefab)}.", this);
                return false;
            }

            if (projectilePool == null)
            {
                Debug.LogError($"{nameof(StageRuntimeController)} requires {nameof(projectilePool)}.", this);
                return false;
            }

            if (playerInstance == null)
            {
                playerInstance = Instantiate(playerPrefab);
            }

            playerInstance.SetGameFlowController(gameFlowController);
            playerInstance.SetProjectilePool(projectilePool);

            if (snakeController == null)
            {
                snakeController = Instantiate(snakePrefab);
                snakeController.OnSnakeFinished += HandleSnakeFinished;
            }

            return true;
        }

        private bool TryGetStage(int stageIndex, out StageConfigSO stage)
        {
            stage = null;
            if (stageConfigs == null || stageConfigs.Count == 0)
            {
                return false;
            }

            if (stageIndex < 0 || stageIndex >= stageConfigs.Count)
            {
                return false;
            }

            stage = stageConfigs[stageIndex];
            return stage != null;
        }

        private void ApplyStageConfig(StageConfigSO stage)
        {
            snakeController.ApplyStageSettings(
                stage.EnemyMoveSpeed,
                stage.EnemyBaseHp,
                stage.MaxSegmentCount,
                stage.ScaleHpTowardTail,
                stage.HpIncreasePerSegment,
                stage.MaxHpMultiplier,
                ConvertRewardChances(stage.RewardLevelChances),
                stage.PathPoints,
                stage.UseBezierPath,
                stage.EnrageSpeedMultiplier,
                stage.EnrageDurationSeconds,
                stage.EnrageTriggerProgresses01,
                stage.EnrageHeadSprite);
        }

        private void HandleSnakeFinished(bool isClear)
        {
            OnStageEnded?.Invoke(isClear);
            Debug.Log(isClear ? "Stage Clear" : "Stage Failed");
        }

        private static RewardLevelChance[] ConvertRewardChances(IReadOnlyList<RewardLevelChanceData> src)
        {
            if (src == null || src.Count == 0)
            {
                return Array.Empty<RewardLevelChance>();
            }

            RewardLevelChance[] result = new RewardLevelChance[src.Count];
            for (int i = 0; i < src.Count; i++)
            {
                result[i] = new RewardLevelChance
                {
                    RewardLevel = Mathf.Max(0, src[i].RewardLevel),
                    Chance = Mathf.Max(0f, src[i].Chance),
                };
            }

            return result;
        }
    }
}
