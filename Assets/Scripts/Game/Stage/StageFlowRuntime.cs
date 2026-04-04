using UnityEngine;

namespace GameCamp.Game.Stage
{
    public readonly struct StageResultRuntimeData
    {
        public readonly int StageIndex;
        public readonly bool IsSuccess;

        public StageResultRuntimeData(int stageIndex, bool isSuccess)
        {
            StageIndex = stageIndex;
            IsSuccess = isSuccess;
        }
    }

    public static class StageFlowRuntime
    {
        private static bool initialized;
        private static int totalStageCount = 1;
        private static int unlockedMaxStageIndex;
        private static int selectedStageIndex;
        private static bool hasPendingResult;
        private static StageResultRuntimeData pendingResult;

        public static int TotalStageCount => totalStageCount;
        public static int UnlockedMaxStageIndex => unlockedMaxStageIndex;
        public static int SelectedStageIndex => selectedStageIndex;

        public static void Initialize(int stageCount)
        {
            int safeCount = Mathf.Max(1, stageCount);

            if (!initialized)
            {
                initialized = true;
                totalStageCount = safeCount;
                unlockedMaxStageIndex = 0;
                selectedStageIndex = 0;
                return;
            }

            // Never shrink by a smaller caller value (e.g. OutGame inspector mismatch).
            totalStageCount = Mathf.Max(totalStageCount, safeCount);
            unlockedMaxStageIndex = Mathf.Clamp(unlockedMaxStageIndex, 0, totalStageCount - 1);
            selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, unlockedMaxStageIndex);
        }

        public static void SetSelectedStageIndex(int stageIndex)
        {
            selectedStageIndex = Mathf.Clamp(stageIndex, 0, unlockedMaxStageIndex);
        }

        public static bool CanSelect(int stageIndex)
        {
            return stageIndex >= 0 && stageIndex <= unlockedMaxStageIndex && stageIndex < totalStageCount;
        }

        public static void ReportStageFinished(int stageIndex, bool isSuccess)
        {
            hasPendingResult = true;
            pendingResult = new StageResultRuntimeData(stageIndex, isSuccess);

            if (!isSuccess)
            {
                return;
            }

            int nextIndex = stageIndex + 1;
            if (nextIndex <= unlockedMaxStageIndex)
            {
                return;
            }

            unlockedMaxStageIndex = Mathf.Clamp(nextIndex, 0, totalStageCount - 1);
        }

        public static bool TryConsumePendingResult(out StageResultRuntimeData result)
        {
            if (!hasPendingResult)
            {
                result = default;
                return false;
            }

            hasPendingResult = false;
            result = pendingResult;
            return true;
        }
    }
}
