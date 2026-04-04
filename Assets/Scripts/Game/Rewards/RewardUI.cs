using System;
using System.Collections.Generic;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Rewards
{
    public class RewardUI : MonoBehaviour
    {
        [Serializable]
        private struct RarityPresentation
        {
            public RewardRarity Rarity;
            public string DisplayName;
            public Color Color;
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RewardOptionView optionPrefab;
        [SerializeField] private Transform optionParent;
        [SerializeField] private RarityPresentation[] rarityPresentations = Array.Empty<RarityPresentation>();

        private readonly List<RewardOptionView> optionViews = new();
        private readonly Dictionary<RewardRarity, RarityPresentation> rarityByType = new();
        private RewardSystem rewardSystem;
        private CanvasGroup selfCanvasGroup;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            RebuildRarityMap();
        }

        private void OnEnable()
        {
            SubscribeRewardSystem();
            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            UnsubscribeRewardSystem();
        }

        public void SetRewardSystem(RewardSystem system)
        {
            if (rewardSystem == system)
            {
                return;
            }

            UnsubscribeRewardSystem();
            rewardSystem = system;
            SubscribeRewardSystem();
        }

        private void SubscribeRewardSystem()
        {
            if (rewardSystem == null || !isActiveAndEnabled)
            {
                return;
            }

            rewardSystem.OnRewardOptionsOpened += HandleRewardOptionsOpened;
            rewardSystem.OnRewardOptionsClosed += HandleRewardOptionsClosed;
        }

        private void UnsubscribeRewardSystem()
        {
            if (rewardSystem != null)
            {
                rewardSystem.OnRewardOptionsOpened -= HandleRewardOptionsOpened;
                rewardSystem.OnRewardOptionsClosed -= HandleRewardOptionsClosed;
            }
        }

        private void HandleRewardOptionsOpened(IReadOnlyList<RewardOptionData> options)
        {
            if (optionPrefab == null)
            {
                Debug.LogError($"{nameof(RewardUI)} requires {nameof(optionPrefab)} reference.", this);
                return;
            }

            if (optionParent == null)
            {
                Debug.LogError($"{nameof(RewardUI)} requires {nameof(optionParent)} reference.", this);
                return;
            }

            if (panelRoot == null)
            {
                Debug.LogError($"{nameof(RewardUI)} requires {nameof(panelRoot)} reference.", this);
                return;
            }

            SetPanelVisible(true);
            EnsureOptionViewCount(options.Count);

            for (int i = 0; i < optionViews.Count; i++)
            {
                bool active = i < options.Count;
                optionViews[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                RewardOptionData option = options[i];
                bool hasRarity = TryGetRarityPresentation(option.Rarity, out string rarityName, out Color rarityColor);
                bool hasScope = TryGetWeaponPresentation(option.Scope, out string scopeName, out Color scopeColor, out Sprite scopeSprite);
                bool hasEffectWeapon = TryGetWeaponPresentation(option.EffectWeapon, out string effectWeaponName, out Color effectWeaponColor, out Sprite effectWeaponSprite);
                bool ok = hasRarity && hasScope && hasEffectWeapon;

                if (!ok)
                {
                    Debug.LogError($"{nameof(RewardUI)} missing presentation mapping for reward option id {option.RewardId}.", this);
                    optionViews[i].gameObject.SetActive(false);
                    continue;
                }

                optionViews[i].Bind(
                    option.RewardId,
                    option.Name,
                    option.Description,
                    rarityName,
                    rarityColor,
                    scopeName,
                    effectWeaponColor,
                    effectWeaponSprite,
                    option.EffectType,
                    HandleSelectReward);

                _ = scopeColor;
                _ = scopeSprite;
                _ = effectWeaponName;
            }
        }

        private void HandleRewardOptionsClosed()
        {
            SetPanelVisible(false);
        }

        private void HandleSelectReward(int rewardId)
        {
            rewardSystem?.SelectReward(rewardId);
        }

        private void EnsureOptionViewCount(int count)
        {
            while (optionViews.Count < count)
            {
                RewardOptionView view = Instantiate(optionPrefab, optionParent);
                optionViews.Add(view);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot == gameObject)
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
                return;
            }

            panelRoot.SetActive(visible);
        }

        private void RebuildRarityMap()
        {
            rarityByType.Clear();
            for (int i = 0; i < rarityPresentations.Length; i++)
            {
                RarityPresentation item = rarityPresentations[i];
                if (rarityByType.ContainsKey(item.Rarity))
                {
                    Debug.LogError($"{nameof(RewardUI)} duplicated rarity mapping: {item.Rarity}", this);
                    continue;
                }

                rarityByType.Add(item.Rarity, item);
            }
        }

        private bool TryGetRarityPresentation(RewardRarity rarity, out string displayName, out Color color)
        {
            if (!rarityByType.TryGetValue(rarity, out RarityPresentation item))
            {
                displayName = string.Empty;
                color = Color.white;
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.DisplayName))
            {
                displayName = string.Empty;
                color = Color.white;
                return false;
            }

            displayName = item.DisplayName;
            color = item.Color;
            return true;
        }

        private bool TryGetWeaponPresentation(WeaponType weaponType, out string name, out Color color, out Sprite sprite)
        {
            name = string.Empty;
            color = Color.white;
            sprite = null;

            if (rewardSystem == null || !rewardSystem.TryGetWeaponData(weaponType, out WeaponDataSO so) || so == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(so.DisplayName))
            {
                return false;
            }

            name = so.DisplayName;
            color = so.SignatureColor;
            sprite = so.WeaponSprite;
            return true;
        }
    }
}
