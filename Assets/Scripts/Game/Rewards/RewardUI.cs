using System;
using System.Collections.Generic;
using DG.Tweening;
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
        [SerializeField] private float showAnimDuration = 0.22f;
        [SerializeField] private float showAnimScaleFrom = 0.92f;

        private readonly List<RewardOptionView> optionViews = new();
        private readonly Dictionary<RewardRarity, RarityPresentation> rarityByType = new();
        private RewardSystem rewardSystem;
        private CanvasGroup selfCanvasGroup;
        private Tween showTween;

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
            SetPanelVisible(false, false);
        }

        private void OnDisable()
        {
            showTween?.Kill();
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

            SetPanelVisible(true, true);
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
            SetPanelVisible(false, false);
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

        private void SetPanelVisible(bool visible, bool animateShow)
        {
            showTween?.Kill();

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
                return;
            }

            panelRoot.SetActive(visible);
            CanvasGroup cg = panelRoot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panelRoot.AddComponent<CanvasGroup>();
            }

            RectTransform rt = panelRoot.transform as RectTransform;
            if (!visible)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                return;
            }

            cg.interactable = true;
            cg.blocksRaycasts = true;

            if (!animateShow)
            {
                cg.alpha = 1f;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                }

                return;
            }

            cg.alpha = 0f;
            if (rt != null)
            {
                rt.localScale = Vector3.one * Mathf.Max(0.1f, showAnimScaleFrom);
            }

            Sequence rootSeq = DOTween.Sequence().SetUpdate(true);
            rootSeq.Join(DOTween.To(
                () => cg.alpha,
                value => cg.alpha = value,
                1f,
                Mathf.Max(0.01f, showAnimDuration)));
            if (rt != null)
            {
                rootSeq.Join(rt.DOScale(Vector3.one, Mathf.Max(0.01f, showAnimDuration)).SetEase(Ease.OutCubic));
            }

            showTween = rootSeq;
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
