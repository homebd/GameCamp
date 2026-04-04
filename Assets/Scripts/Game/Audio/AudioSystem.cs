using System.Collections.Generic;
using UnityEngine;

namespace GameCamp.Game.Audio
{
    public class AudioSystem : MonoBehaviour
    {
        private struct ActiveSource
        {
            public AudioSource Source;
            public float Remaining;

            public ActiveSource(AudioSource source, float remaining)
            {
                Source = source;
                Remaining = remaining;
            }
        }

        public static AudioSystem Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private AudioCueCatalogSO cueCatalog;

        [Header("Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private int initialSfxSourceCount = 8;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Volume")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float bgmVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;

        private readonly Dictionary<GameAudioCueId, AudioCueEntry> cueById = new();
        private readonly Queue<AudioSource> availableSfx = new();
        private readonly List<ActiveSource> activeSfx = new();
        private float currentBgmCueVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            RebuildCache();
            EnsureBgmSource();
            PrewarmSfx(Mathf.Max(0, initialSfxSourceCount));
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = activeSfx.Count - 1; i >= 0; i--)
            {
                ActiveSource item = activeSfx[i];
                item.Remaining -= dt;

                if (item.Remaining <= 0f || item.Source == null || !item.Source.isPlaying)
                {
                    ReleaseSfxSource(item.Source);
                    activeSfx.RemoveAt(i);
                    continue;
                }

                activeSfx[i] = item;
            }
        }

        public void PlayBgm(GameAudioCueId cueId)
        {
            if (!TryGetCue(cueId, out AudioCueEntry cue) || cue.Clip == null)
            {
                return;
            }

            EnsureBgmSource();
            bgmSource.clip = cue.Clip;
            currentBgmCueVolume = cue.ResolvedVolume;
            bgmSource.volume = currentBgmCueVolume * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(bgmVolume);
            bgmSource.pitch = 1f;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlaySfx(GameAudioCueId cueId)
        {
            PlaySfxInternal(cueId, transform.position, false);
        }

        public void PlaySfxAt(GameAudioCueId cueId, Vector3 worldPosition)
        {
            PlaySfxInternal(cueId, worldPosition, true);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            RefreshBgmVolume();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            RefreshBgmVolume();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        private void PlaySfxInternal(GameAudioCueId cueId, Vector3 worldPosition, bool useWorldPosition)
        {
            if (!TryGetCue(cueId, out AudioCueEntry cue) || cue.Clip == null)
            {
                return;
            }

            AudioSource source = GetSfxSource();
            if (source == null)
            {
                return;
            }

            float pitchMin = cue.ResolvedPitchMin;
            float pitchMax = cue.ResolvedPitchMax;
            float pitch = Random.Range(Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax));

            source.transform.position = useWorldPosition ? worldPosition : transform.position;
            source.clip = cue.Clip;
            source.volume = cue.ResolvedVolume * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(sfxVolume);
            source.pitch = pitch;
            source.loop = false;
            source.spatialBlend = 0f;
            source.Play();

            float remain = source.clip.length / Mathf.Max(0.01f, source.pitch);
            activeSfx.Add(new ActiveSource(source, remain));
        }

        private void RefreshBgmVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = currentBgmCueVolume * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(bgmVolume);
            }
        }

        private void RebuildCache()
        {
            cueById.Clear();
            if (cueCatalog == null || cueCatalog.Entries == null)
            {
                return;
            }

            AudioCueEntry[] entries = cueCatalog.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                AudioCueEntry entry = entries[i];
                cueById[entry.CueId] = entry;
            }
        }

        private bool TryGetCue(GameAudioCueId cueId, out AudioCueEntry cue)
        {
            if (cueById.Count == 0)
            {
                RebuildCache();
            }

            return cueById.TryGetValue(cueId, out cue);
        }

        private void EnsureBgmSource()
        {
            if (bgmSource != null)
            {
                return;
            }

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
        }

        private void PrewarmSfx(int count)
        {
            for (int i = 0; i < count; i++)
            {
                availableSfx.Enqueue(CreateSfxSource());
            }
        }

        private AudioSource GetSfxSource()
        {
            if (availableSfx.Count > 0)
            {
                return availableSfx.Dequeue();
            }

            return CreateSfxSource();
        }

        private void ReleaseSfxSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            availableSfx.Enqueue(source);
        }

        private AudioSource CreateSfxSource()
        {
            GameObject go = new("SfxSource");
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
