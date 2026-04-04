using GameCamp.Game.Audio;
using GameCamp.Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCamp.Game.Stage
{
    public class GameHudView : MonoBehaviour
    {
        private StageRuntimeController stageRuntimeController;
        private GameFlowController gameFlowController;

        [Header("Scene")]
        [SerializeField] private string outGameSceneName = "OutGame";

        [Header("UI")]
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button homeButton;

        [Header("Speed")]
        [SerializeField] private float normalSpeed = 1f;
        [SerializeField] private float fastSpeed = 2f;

        private bool isFastMode;
        private bool isSubscribedToFlow;

        private void Awake()
        {
            if (speedButton != null)
            {
                speedButton.onClick.AddListener(OnClickSpeed);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnClickHome);
            }
        }

        private void OnEnable()
        {
            SubscribeFlow();
            RefreshStageText();
            RefreshProgressText();
            RefreshSpeedText();
            UpdateSpeedButtonInteractable();
        }

        private void OnDisable()
        {
            UnsubscribeFlow();
        }

        private void OnDestroy()
        {
            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(OnClickSpeed);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnClickHome);
            }
        }

        private void Update()
        {
            RefreshStageText();
            RefreshProgressText();
        }

        public void OnClickSpeed()
        {
            if (gameFlowController != null && gameFlowController.State != GameFlowController.FlowState.Playing)
            {
                return;
            }

            isFastMode = !isFastMode;
            ApplyCurrentSpeed();
            RefreshSpeedText();
            AudioSystem.Instance?.PlaySfx(GameAudioCueId.UiClick);
        }

        public void OnClickHome()
        {
            Time.timeScale = 1f;
            AudioSystem.Instance?.PlaySfx(GameAudioCueId.UiClick);
            SceneManager.LoadScene(outGameSceneName);
        }

        public void SetReferences(StageRuntimeController stageController, GameFlowController flowController)
        {
            if (stageRuntimeController == stageController && gameFlowController == flowController)
            {
                return;
            }

            UnsubscribeFlow();
            stageRuntimeController = stageController;
            gameFlowController = flowController;
            SubscribeFlow();

            RefreshStageText();
            RefreshProgressText();
            RefreshSpeedText();
            UpdateSpeedButtonInteractable();
        }

        private void HandleFlowStateChanged(GameFlowController.FlowState state)
        {
            if (state == GameFlowController.FlowState.Playing)
            {
                ApplyCurrentSpeed();
            }

            UpdateSpeedButtonInteractable();
        }

        private void ApplyCurrentSpeed()
        {
            Time.timeScale = isFastMode ? Mathf.Max(0.01f, fastSpeed) : Mathf.Max(0.01f, normalSpeed);
        }

        private void RefreshStageText()
        {
            if (stageText == null || stageRuntimeController == null)
            {
                return;
            }

            int index = Mathf.Max(0, stageRuntimeController.CurrentStageIndex);
            stageText.text = (index + 1).ToString();
        }

        private void RefreshProgressText()
        {
            if (progressText == null || stageRuntimeController == null || stageRuntimeController.SnakeInstance == null)
            {
                return;
            }

            float progress01 = stageRuntimeController.SnakeInstance.SegmentProgress01;
            int percent = Mathf.RoundToInt(progress01 * 100f);
            progressText.text = $"{percent}%";
        }

        private void RefreshSpeedText()
        {
            if (speedText == null)
            {
                return;
            }

            speedText.text = isFastMode ? $"{fastSpeed:0.#}x" : $"{normalSpeed:0.#}x";
        }

        private void UpdateSpeedButtonInteractable()
        {
            if (speedButton == null)
            {
                return;
            }

            bool interactable = gameFlowController == null || gameFlowController.State == GameFlowController.FlowState.Playing;
            speedButton.interactable = interactable;
        }

        private void SubscribeFlow()
        {
            if (!isActiveAndEnabled || isSubscribedToFlow || gameFlowController == null)
            {
                return;
            }

            gameFlowController.OnStateChanged += HandleFlowStateChanged;
            isSubscribedToFlow = true;
        }

        private void UnsubscribeFlow()
        {
            if (!isSubscribedToFlow || gameFlowController == null)
            {
                return;
            }

            gameFlowController.OnStateChanged -= HandleFlowStateChanged;
            isSubscribedToFlow = false;
        }
    }
}
