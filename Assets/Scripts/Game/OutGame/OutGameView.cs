using GameCamp.Game.Audio;
using GameCamp.Game.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCamp.Game.OutGame
{
    public class OutGameView : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Stage")]
        [SerializeField] private int stageCount = 1;

        [Header("UI")]
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text stageText;

        private int selectedStageIndex;

        private void Awake()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.onClick.AddListener(OnClickLeft);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.onClick.AddListener(OnClickRight);
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(OnClickStart);
            }
        }

        private void OnEnable()
        {
            AudioSystem.Instance?.StopBgm();
            StageFlowRuntime.Initialize(stageCount);
            selectedStageIndex = Mathf.Clamp(StageFlowRuntime.SelectedStageIndex, 0, StageFlowRuntime.UnlockedMaxStageIndex);
            RefreshView();
        }

        private void OnDestroy()
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.onClick.RemoveListener(OnClickLeft);
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.onClick.RemoveListener(OnClickRight);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnClickStart);
            }
        }

        public void OnClickLeft()
        {
            if (selectedStageIndex <= 0)
            {
                return;
            }

            selectedStageIndex--;
            RefreshView();
        }

        public void OnClickRight()
        {
            if (selectedStageIndex >= StageFlowRuntime.UnlockedMaxStageIndex)
            {
                return;
            }

            selectedStageIndex++;
            RefreshView();
        }

        public void OnClickStart()
        {
            if (!StageFlowRuntime.CanSelect(selectedStageIndex))
            {
                return;
            }

            StageFlowRuntime.SetSelectedStageIndex(selectedStageIndex);
            SceneManager.LoadScene(gameSceneName);
        }

        private void RefreshView()
        {
            selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, StageFlowRuntime.UnlockedMaxStageIndex);

            if (stageText != null)
            {
                stageText.text = (selectedStageIndex + 1).ToString();
            }

            if (leftArrowButton != null)
            {
                leftArrowButton.interactable = selectedStageIndex > 0;
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.interactable = selectedStageIndex < StageFlowRuntime.UnlockedMaxStageIndex;
            }

            if (startButton != null)
            {
                startButton.interactable = StageFlowRuntime.CanSelect(selectedStageIndex);
            }
        }
    }
}
