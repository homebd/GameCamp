using System;
using System.Collections.Generic;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Combat.Projectiles
{
    public class ProjectilePool : MonoBehaviour
    {
        [Serializable]
        private struct WeaponProjectileEntry
        {
            public WeaponType WeaponType;
            public Projectile ProjectilePrefab;
            public int InitialSize;
        }

        private struct ActiveProjectile
        {
            public WeaponType WeaponType;
            public Projectile Projectile;
            public float RemainingLifetime;

            public ActiveProjectile(WeaponType weaponType, Projectile projectile, float remainingLifetime)
            {
                WeaponType = weaponType;
                Projectile = projectile;
                RemainingLifetime = remainingLifetime;
            }
        }

        private sealed class PoolBucket
        {
            public WeaponType WeaponType;
            public Projectile Prefab;
            public readonly Queue<Projectile> Available = new();
        }

        [SerializeField] private WeaponProjectileEntry[] weaponProjectileEntries = Array.Empty<WeaponProjectileEntry>();
        [SerializeField] private bool allowExpand = true;

        private readonly Dictionary<WeaponType, PoolBucket> buckets = new();
        private readonly List<ActiveProjectile> active = new();

        private void Awake()
        {
            BuildBuckets();
        }

        private void OnDisable()
        {
            ClearActiveProjectiles();
        }

        private void Update()
        {
            if (active.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveProjectile item = active[i];
                if (item.Projectile == null || !item.Projectile.gameObject.activeSelf)
                {
                    active.RemoveAt(i);
                    continue;
                }

                item.Projectile.Simulate(dt);
                item.RemainingLifetime -= dt;

                if (item.Projectile.ShouldDespawn(item.RemainingLifetime))
                {
                    InternalDespawn(item.Projectile, item.WeaponType);
                    active.RemoveAt(i);
                    continue;
                }

                active[i] = item;
            }
        }

        public T Spawn<T>(WeaponType weaponType, Vector3 position, Quaternion rotation, float lifetime) where T : Projectile
        {
            if (!buckets.TryGetValue(weaponType, out PoolBucket bucket))
            {
                Debug.LogError($"{nameof(ProjectilePool)} has no bucket for weapon type: {weaponType}", this);
                return null;
            }

            Projectile projectile = null;
            if (bucket.Available.Count > 0)
            {
                projectile = bucket.Available.Dequeue();
            }
            else if (allowExpand)
            {
                projectile = CreateInstance(bucket);
            }

            if (projectile == null)
            {
                return null;
            }

            if (projectile is not T typed)
            {
                Debug.LogError($"{nameof(ProjectilePool)} type mismatch. Weapon:{weaponType}, Requested:{typeof(T).Name}, Actual:{projectile.GetType().Name}", this);
                bucket.Available.Enqueue(projectile);
                return null;
            }

            Transform tr = projectile.transform;
            tr.SetPositionAndRotation(position, rotation);
            projectile.gameObject.SetActive(true);
            projectile.OnSpawned();

            active.Add(new ActiveProjectile(weaponType, projectile, Mathf.Max(0.05f, lifetime)));
            return typed;
        }

        public void Despawn(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Projectile == projectile)
                {
                    WeaponType weaponType = active[i].WeaponType;
                    active.RemoveAt(i);
                    InternalDespawn(projectile, weaponType);
                    return;
                }
            }
        }

        private void BuildBuckets()
        {
            buckets.Clear();

            for (int i = 0; i < weaponProjectileEntries.Length; i++)
            {
                WeaponProjectileEntry entry = weaponProjectileEntries[i];
                if (entry.ProjectilePrefab == null)
                {
                    Debug.LogError($"{nameof(ProjectilePool)} weapon projectile entry has null prefab at index {i}.", this);
                    continue;
                }

                if (buckets.ContainsKey(entry.WeaponType))
                {
                    Debug.LogError($"{nameof(ProjectilePool)} duplicated weapon type entry: {entry.WeaponType}", this);
                    continue;
                }

                PoolBucket bucket = new()
                {
                    WeaponType = entry.WeaponType,
                    Prefab = entry.ProjectilePrefab,
                };

                buckets.Add(entry.WeaponType, bucket);
                Prewarm(bucket, Mathf.Max(0, entry.InitialSize));
            }
        }

        private void Prewarm(PoolBucket bucket, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Projectile projectile = CreateInstance(bucket);
                if (projectile == null)
                {
                    continue;
                }

                projectile.gameObject.SetActive(false);
                bucket.Available.Enqueue(projectile);
            }
        }

        private Projectile CreateInstance(PoolBucket bucket)
        {
            Projectile projectile = Instantiate(bucket.Prefab, transform);
            projectile.BindOwner(this);
            return projectile;
        }

        private void InternalDespawn(Projectile projectile, WeaponType weaponType)
        {
            if (!buckets.TryGetValue(weaponType, out PoolBucket bucket))
            {
                Debug.LogError($"{nameof(ProjectilePool)} cannot despawn unknown weapon type: {weaponType}", this);
                projectile.gameObject.SetActive(false);
                return;
            }

            projectile.OnDespawned(transform);
            bucket.Available.Enqueue(projectile);
        }

        private void ClearActiveProjectiles()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                ActiveProjectile item = active[i];
                if (item.Projectile == null)
                {
                    continue;
                }

                InternalDespawn(item.Projectile, item.WeaponType);
            }

            active.Clear();
        }
    }
}
