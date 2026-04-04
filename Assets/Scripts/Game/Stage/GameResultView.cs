using GameCamp.Game.Core;
using GameCamp.Game.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCamp.Game.Stage
{
    public class GameResultView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StageRuntimeController stageRuntimeController;
        [SerializeField] private GameFlowController gameFlowController;

        [Header("Scene")]
        [SerializeField] private string outGameSceneName = "OutGame";

        [Header("UI")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button returnButton;
        private CanvasGroup selfCanvasGroup;

        private void Awake()
        {
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(OnClickReturnToOutGame);
            }
        }

        private void OnEnable()
        {
            SubscribeStageEvents();
            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            UnsubscribeStageEvents();
        }

        private void OnDestroy()
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(OnClickReturnToOutGame);
            }
        }

        public void OnClickReturnToOutGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(outGameSceneName);
        }

        public void SetReferences(StageRuntimeController stageController, GameFlowController flowController)
        {
            if (stageRuntimeController == stageController && gameFlowController == flowController)
            {
                return;
            }

            UnsubscribeStageEvents();
            stageRuntimeController = stageController;
            gameFlowController = flowController;
            SubscribeStageEvents();
        }

        private void HandleStageEnded(bool isClear)
        {
            if (resultText != null)
            {
                resultText.text = isClear ? "클리어" : "실패";
            }

            AudioSystem.Instance?.PlaySfx(isClear ? GameAudioCueId.GameClear : GameAudioCueId.GameFailed);

            SetPanelVisible(true);

            if (returnButton != null)
            {
                returnButton.interactable = true;
            }

            if (gameFlowController != null && gameFlowController.State == GameFlowController.FlowState.RewardPaused)
            {
                gameFlowController.ResumeFromRewardPause();
            }
        }

        private void SubscribeStageEvents()
        {
            if (!isActiveAndEnabled || stageRuntimeController == null)
            {
                return;
            }

            stageRuntimeController.OnStageEnded -= HandleStageEnded;
            stageRuntimeController.OnStageEnded += HandleStageEnded;
        }

        private void UnsubscribeStageEvents()
        {
            if (stageRuntimeController != null)
            {
                stageRuntimeController.OnStageEnded -= HandleStageEnded;
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (selfCanvasGroup == null)
            {
                selfCanvasGroup = GetComponent<CanvasGroup>();
                if (selfCanvasGroup == null)
                {
                    selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            selfCanvasGroup.alpha = visible ? 1f : 0f;
            selfCanvasGroup.interactable = visible;
            selfCanvasGroup.blocksRaycasts = visible;
        }
    }
}
