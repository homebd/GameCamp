using System;
using UnityEngine;

namespace GameCamp.Game.Audio
{
    [Serializable]
    public struct AudioCueEntry
    {
        [field: SerializeField] public GameAudioCueId CueId { get; private set; }
        [field: SerializeField] public AudioClip Clip { get; private set; }
        [field: SerializeField] public float Volume { get; private set; }
        [field: SerializeField] public float PitchMin { get; private set; }
        [field: SerializeField] public float PitchMax { get; private set; }

        public float ResolvedVolume => Mathf.Clamp01(Volume < 0f ? 1f : Volume);
        public float ResolvedPitchMin => Mathf.Clamp(PitchMin <= 0f ? 1f : PitchMin, 0.1f, 3f);
        public float ResolvedPitchMax => Mathf.Clamp(PitchMax <= 0f ? 1f : PitchMax, 0.1f, 3f);
    }

    [CreateAssetMenu(fileName = "AudioCueCatalogSO", menuName = "GameCamp/Data/AudioCueCatalogSO")]
    public class AudioCueCatalogSO : ScriptableObject
    {
        [field: SerializeField] public AudioCueEntry[] Entries { get; private set; } = Array.Empty<AudioCueEntry>();
    }
}
