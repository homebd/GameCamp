using UnityEngine;
using UnityEngine.UI;

namespace GameCamp.Game.Audio
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSfxBinder : MonoBehaviour
    {
        [SerializeField] private GameAudioCueId clickCue = GameAudioCueId.UiClick;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            AudioSystem.Instance?.PlaySfx(clickCue);
        }
    }
}
