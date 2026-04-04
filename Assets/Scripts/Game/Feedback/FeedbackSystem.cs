using System.Collections.Generic;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Feedback
{
    public class FeedbackSystem : MonoBehaviour
    {
        private struct ActiveDamageText
        {
            public DamageTextPopup Popup;

            public ActiveDamageText(DamageTextPopup popup)
            {
                Popup = popup;
            }
        }

        public static FeedbackSystem Instance { get; private set; }

        [SerializeField] private FeedbackCatalogSO catalog;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Queue<DamageTextPopup> availableDamageTexts = new();
        private readonly List<ActiveDamageText> activeDamageTexts = new();
        private readonly Dictionary<int, Queue<ParticleSystem>> pooledParticlesByPrefabId = new();
        private readonly Dictionary<WeaponType, ParticleSystem> weaponVfxPrefabByType = new();
        private readonly List<ParticleSystem> activeParticles = new();

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

            Prewarm();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = activeDamageTexts.Count - 1; i >= 0; i--)
            {
                ActiveDamageText item = activeDamageTexts[i];
                if (item.Popup == null)
                {
                    activeDamageTexts.RemoveAt(i);
                    continue;
                }

                item.Popup.Simulate(dt);
                if (!item.Popup.IsPlaying)
                {
                    ReturnDamageText(item.Popup);
                    activeDamageTexts.RemoveAt(i);
                }
            }

            for (int i = activeParticles.Count - 1; i >= 0; i--)
            {
                ParticleSystem particle = activeParticles[i];
                if (particle == null)
                {
                    activeParticles.RemoveAt(i);
                    continue;
                }

                if (!particle.gameObject.activeSelf)
                {
                    ReturnParticleToPool(particle);
                    activeParticles.RemoveAt(i);
                }
            }
        }

        public void SpawnDamageText(int value, Vector3 worldPosition)
        {
            if (catalog == null || catalog.DamageTextPrefab == null)
            {
                return;
            }

            DamageTextPopup popup = GetDamageText();
            popup.transform.position = worldPosition;
            popup.Play(value.ToString(), catalog.DamageTextColor, catalog.DamageTextLifetime, catalog.DamageTextRiseSpeed, catalog.DamageTextRandomX);
            activeDamageTexts.Add(new ActiveDamageText(popup));
        }

        public void SpawnWeaponVfx(WeaponType weaponType, Vector3 worldPosition, float scaleMultiplier = 1f)
        {
            if (!weaponVfxPrefabByType.TryGetValue(weaponType, out ParticleSystem prefab) || prefab == null)
            {
                return;
            }

            float safeScale = Mathf.Max(0.05f, scaleMultiplier);
            SpawnParticleVfx(prefab, worldPosition, Vector3.one * safeScale);
        }

        public void SpawnParticleVfx(ParticleSystem prefab, Vector3 worldPosition, Vector3 scale)
        {
            if (prefab == null)
            {
                return;
            }

            ParticleSystem vfx = GetOrCreateParticle(prefab);
            if (vfx == null)
            {
                return;
            }

            Transform tr = vfx.transform;
            tr.position = worldPosition;
            tr.localScale = scale;
            vfx.gameObject.SetActive(true);
            vfx.Play(true);
            activeParticles.Add(vfx);
        }

        private void Prewarm()
        {
            if (catalog == null)
            {
                return;
            }

            for (int i = 0; i < Mathf.Max(0, catalog.DamageTextPrewarm); i++)
            {
                DamageTextPopup popup = CreateDamageText();
                if (popup != null)
                {
                    availableDamageTexts.Enqueue(popup);
                }
            }

            weaponVfxPrefabByType.Clear();
            if (catalog.WeaponVfxEntries == null)
            {
                return;
            }

            for (int i = 0; i < catalog.WeaponVfxEntries.Length; i++)
            {
                WeaponVfxEntry entry = catalog.WeaponVfxEntries[i];
                if (entry.VfxPrefab == null)
                {
                    continue;
                }

                if (weaponVfxPrefabByType.ContainsKey(entry.WeaponType))
                {
                    Debug.LogError($"{nameof(FeedbackSystem)} duplicated weapon vfx entry: {entry.WeaponType}", this);
                    continue;
                }

                weaponVfxPrefabByType.Add(entry.WeaponType, entry.VfxPrefab);
            }
        }

        private DamageTextPopup GetDamageText()
        {
            if (availableDamageTexts.Count > 0)
            {
                DamageTextPopup popup = availableDamageTexts.Dequeue();
                popup.gameObject.SetActive(true);
                return popup;
            }

            return CreateDamageText();
        }

        private void ReturnDamageText(DamageTextPopup popup)
        {
            popup.gameObject.SetActive(false);
            popup.transform.SetParent(transform, false);
            availableDamageTexts.Enqueue(popup);
        }

        private DamageTextPopup CreateDamageText()
        {
            if (catalog == null || catalog.DamageTextPrefab == null)
            {
                return null;
            }

            DamageTextPopup popup = Instantiate(catalog.DamageTextPrefab, transform);
            popup.gameObject.SetActive(false);
            return popup;
        }

        private ParticleSystem GetOrCreateParticle(ParticleSystem prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            int prefabId = prefab.GetInstanceID();
            if (!pooledParticlesByPrefabId.TryGetValue(prefabId, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                pooledParticlesByPrefabId.Add(prefabId, pool);
            }

            while (pool.Count > 0)
            {
                ParticleSystem reused = pool.Dequeue();
                if (reused != null)
                {
                    return reused;
                }
            }

            return CreateParticleInstance(prefab, prefabId);
        }

        private ParticleSystem CreateParticleInstance(ParticleSystem prefab, int prefabId)
        {
            ParticleSystem vfx = Instantiate(prefab, transform);
            vfx.gameObject.SetActive(false);
            PooledParticleMarker marker = vfx.GetComponent<PooledParticleMarker>();
            if (marker == null)
            {
                marker = vfx.gameObject.AddComponent<PooledParticleMarker>();
            }

            marker.PrefabId = prefabId;
            return vfx;
        }

        private void ReturnParticleToPool(ParticleSystem particle)
        {
            if (particle == null)
            {
                return;
            }

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.transform.SetParent(transform, false);

            int prefabId = ResolveSourcePrefabId(particle);
            if (prefabId == 0)
            {
                return;
            }

            if (!pooledParticlesByPrefabId.TryGetValue(prefabId, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                pooledParticlesByPrefabId.Add(prefabId, pool);
            }

            pool.Enqueue(particle);
        }

        private int ResolveSourcePrefabId(ParticleSystem particle)
        {
            if (particle == null)
            {
                return 0;
            }
            PooledParticleMarker marker = particle.GetComponent<PooledParticleMarker>();
            return marker != null ? marker.PrefabId : 0;
        }
    }
}
