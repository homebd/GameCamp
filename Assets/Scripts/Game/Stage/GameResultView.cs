using GameCamp.Game.Core;
using GameCamp.Game.Audio;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCamp.Game.Stage
{
    public class GameResultView : MonoBehaviour
    {
        [Header("References")]
        private StageRuntimeController stageRuntimeController;
        private GameFlowController gameFlowController;

        [Header("Scene")]
        [SerializeField] private string outGameSceneName = "OutGame";

        [Header("UI")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button returnButton;
        [SerializeField] private float showAnimDuration = 0.24f;
        [SerializeField] private float showAnimScaleFrom = 0.9f;
        private CanvasGroup selfCanvasGroup;
        private Tween showTween;

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
            SetPanelVisible(false, false);
        }

        private void OnDisable()
        {
            showTween?.Kill();
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

            SetPanelVisible(true, true);

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

        private void SetPanelVisible(bool visible, bool animateShow)
        {
            showTween?.Kill();

            if (selfCanvasGroup == null)
            {
                selfCanvasGroup = GetComponent<CanvasGroup>();
                if (selfCanvasGroup == null)
                {
                    selfCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (!visible)
            {
                selfCanvasGroup.alpha = 0f;
                selfCanvasGroup.interactable = false;
                selfCanvasGroup.blocksRaycasts = false;
                return;
            }

            selfCanvasGroup.interactable = true;
            selfCanvasGroup.blocksRaycasts = true;

            if (!animateShow)
            {
                selfCanvasGroup.alpha = 1f;
                transform.localScale = Vector3.one;
                return;
            }

            selfCanvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, showAnimScaleFrom);
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(DOTween.To(
                () => selfCanvasGroup.alpha,
                value => selfCanvasGroup.alpha = value,
                1f,
                Mathf.Max(0.01f, showAnimDuration)));
            seq.Join(transform.DOScale(Vector3.one, Mathf.Max(0.01f, showAnimDuration)).SetEase(Ease.OutCubic));
            showTween = seq;
        }
    }
}
