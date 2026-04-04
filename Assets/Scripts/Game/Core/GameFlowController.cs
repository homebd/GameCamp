using System;
using GameCamp.Game.Audio;
using GameCamp.Game.Stage;
using UnityEngine;

namespace GameCamp.Game.Core
{
    public class GameFlowController : MonoBehaviour
    {
        public enum FlowState
        {
            Playing = 0,
            RewardPaused = 1,
            StageEnded = 2,
        }

        [SerializeField] private StageRuntimeController stageRuntimeController;
        [SerializeField] private GameResultView gameResultViewPrefab;
        [SerializeField] private GameHudView gameHudViewPrefab;

        private GameResultView gameResultViewInstance;
        private GameHudView gameHudViewInstance;

        public FlowState State { get; private set; } = FlowState.Playing;
        public event Action<FlowState> OnStateChanged;

        private void OnEnable()
        {
            AudioSystem.Instance?.PlayBgm(GameAudioCueId.BgmMain);
            EnsureGameResultView();
            EnsureGameHudView();

            if (stageRuntimeController != null)
            {
                stageRuntimeController.OnStageEnded += HandleStageEnded;
            }
        }

        private void OnDisable()
        {
            if (stageRuntimeController != null)
            {
                stageRuntimeController.OnStageEnded -= HandleStageEnded;
            }
        }

        public void RequestRewardPause()
        {
            if (State == FlowState.StageEnded)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(FlowState.RewardPaused);
        }

        public void ResumeFromRewardPause()
        {
            if (State != FlowState.RewardPaused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(FlowState.Playing);
        }

        private void HandleStageEnded(bool isClear)
        {
            Time.timeScale = 1f;
            SetState(FlowState.StageEnded);

            if (stageRuntimeController != null)
            {
                StageFlowRuntime.ReportStageFinished(stageRuntimeController.CurrentStageIndex, isClear);
            }
        }

        private void SetState(FlowState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            OnStateChanged?.Invoke(State);
        }

        private void EnsureGameResultView()
        {
            if (gameResultViewInstance != null)
            {
                gameResultViewInstance.SetReferences(stageRuntimeController, this);
                return;
            }

            if (gameResultViewPrefab == null)
            {
                return;
            }

            gameResultViewInstance = Instantiate(gameResultViewPrefab);
            gameResultViewInstance.SetReferences(stageRuntimeController, this);
            gameResultViewInstance.gameObject.SetActive(true);
        }

        private void EnsureGameHudView()
        {
            if (gameHudViewInstance != null)
            {
                gameHudViewInstance.SetReferences(stageRuntimeController, this);
                return;
            }

            if (gameHudViewPrefab == null)
            {
                return;
            }

            gameHudViewInstance = Instantiate(gameHudViewPrefab);
            gameHudViewInstance.SetReferences(stageRuntimeController, this);
            gameHudViewInstance.gameObject.SetActive(true);
        }
    }
}
