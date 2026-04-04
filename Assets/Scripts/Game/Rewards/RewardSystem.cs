using System;
using System.Collections.Generic;
using GameCamp.Game.Audio;
using GameCamp.Game.Core;
using GameCamp.Game.Data;
using GameCamp.Game.Player;
using GameCamp.Game.Snake;
using GameCamp.Game.Stage;
using UnityEngine;

namespace GameCamp.Game.Rewards
{
    public class RewardSystem : MonoBehaviour
    {
        private const int OptionCount = 3;

        [Header("References")]
        [SerializeField] private StageRuntimeController stageRuntimeController;
        [SerializeField] private GameFlowController gameFlowController;
        [SerializeField] private RewardUI rewardUiPrefab;

        [Header("Data")]
        [SerializeField] private TextAsset rewardCsv;
        [SerializeField] private RewardLevelRarityWeights[] rewardLevelRarityWeights = Array.Empty<RewardLevelRarityWeights>();
        [SerializeField] private List<WeaponDataSO> weaponCatalog = new();
        [SerializeField] private int[] startRewardIds = Array.Empty<int>();

        private readonly Dictionary<int, RewardDefinition> rewardById = new();
        private readonly List<RewardDefinition> rewardPool = new();
        private readonly Dictionary<int, int> acquiredCounts = new();
        private readonly Queue<int> pendingRewardLevels = new();
        private readonly Dictionary<WeaponType, WeaponDataSO> weaponByKind = new();

        private PlayerController player;
        private SnakeController snake;
        private RewardUI rewardUiInstance;

        private bool isLoaded;
        private bool initialRewardsApplied;
        private bool isRewardOpen;
        private RewardOptionData[] currentOptions = Array.Empty<RewardOptionData>();

        public IReadOnlyList<RewardOptionData> CurrentOptions => currentOptions;
        public event Action<IReadOnlyList<RewardOptionData>> OnRewardOptionsOpened;
        public event Action OnRewardOptionsClosed;

        public bool TryGetWeaponData(WeaponType weaponType, out WeaponDataSO weaponData)
        {
            return weaponByKind.TryGetValue(weaponType, out weaponData) && weaponData != null;
        }

        private void Awake()
        {
            LoadRewardData();
            BuildWeaponCatalog();
            EnsureRewardUiInstance();
        }

        private void OnEnable()
        {
            EnsureRewardUiInstance();
            TryBindRuntimeReferences();
        }

        private void OnDisable()
        {
            UnbindSnake();

            if (isRewardOpen)
            {
                CloseCurrentReward();
            }
        }

        private void Update()
        {
            if (player == null || snake == null)
            {
                TryBindRuntimeReferences();
            }

            if (player != null && !initialRewardsApplied)
            {
                ApplyStartRewards();
                initialRewardsApplied = true;
            }
        }

        public void SelectReward(int rewardId)
        {
            if (!isRewardOpen)
            {
                Debug.LogError($"{nameof(RewardSystem)} received SelectReward while reward UI is closed.", this);
                return;
            }

            RewardDefinition selected = null;
            for (int i = 0; i < currentOptions.Length; i++)
            {
                if (currentOptions[i].RewardId != rewardId)
                {
                    continue;
                }

                if (!rewardById.TryGetValue(rewardId, out selected))
                {
                    Debug.LogError($"{nameof(RewardSystem)} cannot resolve reward id {rewardId} from catalog.", this);
                    return;
                }

                break;
            }

            if (selected == null)
            {
                Debug.LogError($"{nameof(RewardSystem)} selected reward id {rewardId} is not in current options.", this);
                return;
            }

            AudioSystem.Instance?.PlaySfx(GameAudioCueId.RewardSelect);
            ApplyReward(selected);
            CloseCurrentReward();
            TryOpenNextReward();
        }

        private void LoadRewardData()
        {
            rewardById.Clear();
            rewardPool.Clear();

            try
            {
                List<RewardDefinition> rewards = RewardCsvParsing.Parse(rewardCsv);
                for (int i = 0; i < rewards.Count; i++)
                {
                    RewardDefinition entry = rewards[i];
                    rewardById.Add(entry.RewardId, entry);
                    rewardPool.Add(entry);
                }

                isLoaded = true;
            }
            catch (Exception ex)
            {
                isLoaded = false;
                Debug.LogError($"{nameof(RewardSystem)} failed to parse reward CSV: {ex.Message}", this);
            }
        }

        private void BuildWeaponCatalog()
        {
            weaponByKind.Clear();
            for (int i = 0; i < weaponCatalog.Count; i++)
            {
                WeaponDataSO weapon = weaponCatalog[i];
                if (weapon == null)
                {
                    continue;
                }

                if (weaponByKind.ContainsKey(weapon.WeaponKind))
                {
                    Debug.LogError($"{nameof(RewardSystem)} weaponCatalog has duplicated kind: {weapon.WeaponKind}", this);
                    continue;
                }

                weaponByKind.Add(weapon.WeaponKind, weapon);
            }
        }

        private void TryBindRuntimeReferences()
        {
            if (stageRuntimeController == null)
            {
                Debug.LogError($"{nameof(RewardSystem)} requires {nameof(stageRuntimeController)} reference.", this);
                return;
            }

            PlayerController runtimePlayer = stageRuntimeController.PlayerInstance;
            SnakeController runtimeSnake = stageRuntimeController.SnakeInstance;

            if (runtimePlayer != null)
            {
                player = runtimePlayer;
            }

            if (runtimeSnake != snake)
            {
                UnbindSnake();
                snake = runtimeSnake;

                if (snake != null)
                {
                    snake.OnSegmentDestroyed += HandleSegmentDestroyed;
                }
            }
        }

        private void EnsureRewardUiInstance()
        {
            if (rewardUiInstance != null)
            {
                return;
            }

            if (rewardUiPrefab == null)
            {
                Debug.LogError($"{nameof(RewardSystem)} requires {nameof(rewardUiPrefab)}.", this);
                return;
            }

            rewardUiInstance = Instantiate(rewardUiPrefab);

            rewardUiInstance.SetRewardSystem(this);
            rewardUiInstance.gameObject.SetActive(true);
        }

        private void UnbindSnake()
        {
            if (snake != null)
            {
                snake.OnSegmentDestroyed -= HandleSegmentDestroyed;
            }
        }

        private void ApplyStartRewards()
        {
            if (startRewardIds == null)
            {
                return;
            }

            for (int i = 0; i < startRewardIds.Length; i++)
            {
                int rewardId = startRewardIds[i];
                if (!rewardById.TryGetValue(rewardId, out RewardDefinition reward))
                {
                    Debug.LogError($"{nameof(RewardSystem)} start reward id {rewardId} not found in CSV.", this);
                    continue;
                }

                ApplyReward(reward);
            }
        }

        private void HandleSegmentDestroyed(SnakeSegmentRuntime _, int rewardLevel)
        {
            if (!isLoaded || rewardLevel <= 0)
            {
                return;
            }

            pendingRewardLevels.Enqueue(rewardLevel);
            TryOpenNextReward();
        }

        private void TryOpenNextReward()
        {
            if (isRewardOpen || pendingRewardLevels.Count == 0)
            {
                return;
            }

            if (player == null)
            {
                Debug.LogError($"{nameof(RewardSystem)} cannot open reward because player reference is missing.", this);
                return;
            }

            int rewardLevel = pendingRewardLevels.Dequeue();
            RewardOptionData[] options = BuildOptions(rewardLevel);
            if (options.Length != OptionCount)
            {
                Debug.LogError($"{nameof(RewardSystem)} failed to build {OptionCount} options for reward level {rewardLevel}.", this);
                return;
            }

            currentOptions = options;
            isRewardOpen = true;

            gameFlowController?.RequestRewardPause();
            AudioSystem.Instance?.PlaySfx(GameAudioCueId.RewardPopup);
            OnRewardOptionsOpened?.Invoke(currentOptions);
        }

        private RewardOptionData[] BuildOptions(int rewardLevel)
        {
            if (!TryGetRarityWeights(rewardLevel, out Dictionary<RewardRarity, float> rarityWeights))
            {
                return Array.Empty<RewardOptionData>();
            }

            List<RewardDefinition> candidates = BuildCandidatePool();
            if (candidates.Count < OptionCount)
            {
                Debug.LogError($"{nameof(RewardSystem)} candidate pool has only {candidates.Count} items. Need at least {OptionCount}.", this);
                return Array.Empty<RewardOptionData>();
            }

            List<RewardDefinition> working = new(candidates);
            RewardOptionData[] result = new RewardOptionData[OptionCount];

            for (int i = 0; i < OptionCount; i++)
            {
                RewardDefinition picked = PickOne(working, rarityWeights);
                if (picked == null)
                {
                    return Array.Empty<RewardOptionData>();
                }

                working.Remove(picked);
                result[i] = new RewardOptionData(
                    picked.RewardId,
                    picked.Name,
                    picked.BuildDescription(),
                    picked.Rarity,
                    picked.Scope,
                    picked.Effect.WeaponType,
                    picked.Effect.EffectType);
            }

            return result;
        }

        private List<RewardDefinition> BuildCandidatePool()
        {
            List<RewardDefinition> candidates = new();
            for (int i = 0; i < rewardPool.Count; i++)
            {
                RewardDefinition reward = rewardPool[i];

                if (!IsScopeAvailable(reward.Scope))
                {
                    continue;
                }

                int acquired = acquiredCounts.TryGetValue(reward.RewardId, out int count) ? count : 0;
                if (!reward.IsUnlimited && acquired >= reward.MaxAcquireCount)
                {
                    continue;
                }

                candidates.Add(reward);
            }

            return candidates;
        }

        private bool IsScopeAvailable(WeaponType scope)
        {
            if (scope == WeaponType.Common)
            {
                return true;
            }

            return player != null && player.HasWeaponType(scope);
        }

        private static RewardDefinition PickOne(IReadOnlyList<RewardDefinition> pool, IReadOnlyDictionary<RewardRarity, float> rarityWeights)
        {
            Dictionary<RewardRarity, float> localWeights = new();
            for (int i = 0; i < pool.Count; i++)
            {
                RewardRarity rarity = pool[i].Rarity;
                if (!rarityWeights.TryGetValue(rarity, out float w) || w <= 0f)
                {
                    continue;
                }

                if (localWeights.TryGetValue(rarity, out float prev))
                {
                    localWeights[rarity] = prev + w;
                }
                else
                {
                    localWeights.Add(rarity, w);
                }
            }

            if (localWeights.Count == 0)
            {
                return null;
            }

            RewardRarity chosenRarity = RollRarity(localWeights);

            List<RewardDefinition> sameRarity = new();
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Rarity == chosenRarity)
                {
                    sameRarity.Add(pool[i]);
                }
            }

            if (sameRarity.Count == 0)
            {
                return null;
            }

            int idx = UnityEngine.Random.Range(0, sameRarity.Count);
            return sameRarity[idx];
        }

        private static RewardRarity RollRarity(IReadOnlyDictionary<RewardRarity, float> weights)
        {
            float total = 0f;
            foreach (KeyValuePair<RewardRarity, float> pair in weights)
            {
                total += Mathf.Max(0f, pair.Value);
            }

            if (total <= 0f)
            {
                throw new InvalidOperationException("All rarity weights are zero.");
            }

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            foreach (KeyValuePair<RewardRarity, float> pair in weights)
            {
                cumulative += Mathf.Max(0f, pair.Value);
                if (roll <= cumulative)
                {
                    return pair.Key;
                }
            }

            foreach (KeyValuePair<RewardRarity, float> pair in weights)
            {
                return pair.Key;
            }

            throw new InvalidOperationException("Rarity roll failed unexpectedly.");
        }

        private bool TryGetRarityWeights(int rewardLevel, out Dictionary<RewardRarity, float> weights)
        {
            weights = null;
            for (int i = 0; i < rewardLevelRarityWeights.Length; i++)
            {
                RewardLevelRarityWeights entry = rewardLevelRarityWeights[i];
                if (entry.RewardLevel != rewardLevel)
                {
                    continue;
                }

                weights = new Dictionary<RewardRarity, float>();
                if (entry.Weights == null)
                {
                    Debug.LogError($"{nameof(RewardSystem)} reward level {rewardLevel} has null rarity weights.", this);
                    return false;
                }

                for (int j = 0; j < entry.Weights.Length; j++)
                {
                    RewardRarityWeight w = entry.Weights[j];
                    if (weights.ContainsKey(w.Rarity))
                    {
                        Debug.LogError($"{nameof(RewardSystem)} reward level {rewardLevel} has duplicate rarity {w.Rarity}.", this);
                        return false;
                    }

                    weights.Add(w.Rarity, Mathf.Max(0f, w.Weight));
                }

                return true;
            }

            Debug.LogError($"{nameof(RewardSystem)} missing exact rarity weight table for reward level {rewardLevel}.", this);
            return false;
        }

        private void ApplyReward(RewardDefinition reward)
        {
            if (player == null)
            {
                Debug.LogError($"{nameof(RewardSystem)} cannot apply reward because player is null.", this);
                return;
            }

            ApplyEffect(reward.Scope, reward.Effect);

            int count = acquiredCounts.TryGetValue(reward.RewardId, out int prev) ? prev : 0;
            acquiredCounts[reward.RewardId] = count + 1;
        }

        private void ApplyEffect(WeaponType scope, RewardEffectSpec effect)
        {
            switch (effect.EffectType)
            {
                case RewardEffectType.DamageMultiplier:
                {
                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.DamageMultiplier, effect.Value, 1f, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.AttackSpeedMultiplier:
                {
                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.AttackSpeedMultiplier, effect.Value, 1f, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.ProjectileScaleMultiplier:
                {
                    float multiplier = ToPositiveMultiplier(effect.Value);
                    if (multiplier <= 0f)
                    {
                        Debug.LogError($"{nameof(RewardSystem)} ProjectileScaleMultiplier requires value > 0.", this);
                        return;
                    }

                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.ProjectileScaleMultiplier, 0f, multiplier, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.ProjectileCount:
                {
                    int amount = Mathf.RoundToInt(effect.Value);
                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.ProjectileCount, amount, 1f, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.ProjectileLifetimeMultiplier:
                {
                    float multiplier = ToPositiveMultiplier(effect.Value);
                    if (multiplier <= 0f)
                    {
                        Debug.LogError($"{nameof(RewardSystem)} ProjectileLifetimeMultiplier requires value > 0.", this);
                        return;
                    }

                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.ProjectileLifetimeMultiplier, 0f, multiplier, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.ProjectilePierce:
                {
                    int amount = Mathf.RoundToInt(effect.Value);
                    var modifier = WeaponStatModifier.Create(scope, PlayerStatType.ProjectilePierce, amount, 1f, effect.DurationSeconds);
                    player.AddWeaponStatModifier(modifier);
                    break;
                }
                case RewardEffectType.UnlockWeapon:
                {
                    WeaponType target = effect.WeaponType;
                    if (target == WeaponType.Common)
                    {
                        Debug.LogError($"{nameof(RewardSystem)} UnlockWeapon requires non-common weapon type.", this);
                        return;
                    }

                    if (!weaponByKind.TryGetValue(target, out WeaponDataSO weaponData) || weaponData == null)
                    {
                        Debug.LogError($"{nameof(RewardSystem)} weapon catalog missing data for {target}.", this);
                        return;
                    }

                    player.GrantWeapon(weaponData);
                    break;
                }
                default:
                    Debug.LogError($"{nameof(RewardSystem)} unsupported effect type: {effect.EffectType}", this);
                    break;
            }
        }

        private void CloseCurrentReward()
        {
            isRewardOpen = false;
            currentOptions = Array.Empty<RewardOptionData>();
            OnRewardOptionsClosed?.Invoke();
            gameFlowController?.ResumeFromRewardPause();
        }

        private static float ToPositiveMultiplier(float value)
        {
            return 1f + value;
        }
    }
}
