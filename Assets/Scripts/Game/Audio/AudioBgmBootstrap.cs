using UnityEngine;

namespace GameCamp.Game.Audio
{
    public class AudioBgmBootstrap : MonoBehaviour
    {
        [SerializeField] private GameAudioCueId bgmCue = GameAudioCueId.BgmMain;
        [SerializeField] private bool playOnEnable = true;

        private void OnEnable()
        {
            if (playOnEnable)
            {
                AudioSystem.Instance?.PlayBgm(bgmCue);
            }
        }
    }
}
