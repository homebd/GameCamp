using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCamp.Game.Rewards
{
    public class RewardOptionView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private Image rarityColor;
        [SerializeField] private TMP_Text scopeText;
        [SerializeField] private Image scopeColor;
        [SerializeField] private Image Icon;

        private int rewardId;
        private Action<int> onSelect;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleClick);
            }
        }

        public void Bind(
            int id,
            string rewardName,
            string rewardDescription,
            string rarityName,
            Color rarityColor,
            string scopeName,
            Color effectWeaponColor,
            Sprite effectWeaponSprite,
            RewardEffectType effectType,
            Action<int> onSelectAction)
        {
            rewardId = id;
            onSelect = onSelectAction;

            if (nameText != null)
            {
                nameText.text = rewardName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = rewardDescription;
            }

            if (rarityText != null)
            {
                rarityText.text = rarityName;
            }

            if (this.rarityColor != null)
            {
                this.rarityColor.color = rarityColor;
            }

            if (scopeText != null)
            {
                scopeText.text = scopeName;
            }

            if (scopeColor != null)
            {
                scopeColor.color = effectWeaponColor;
            }

            if (Icon != null)
            {
                Icon.sprite = effectWeaponSprite;
            }

            _ = effectType;
        }

        private void HandleClick()
        {
            onSelect?.Invoke(rewardId);
        }
    }
}
