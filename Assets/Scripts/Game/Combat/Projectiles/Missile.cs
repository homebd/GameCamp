using System.Collections.Generic;
using GameCamp.Game.Data;
using GameCamp.Game.Feedback;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Combat.Projectiles
{
    public class Missile : Projectile
    {
        [SerializeField] private float curveStrength = 0.85f;
        [SerializeField] private float snapDistance = 0.25f;
        [Header("VFX")]
        [SerializeField] private ParticleSystem explosionVfxPrefab;

        private readonly HashSet<int> hitSegmentIds = new();

        private SnakeSegmentRuntime target;
        private float speed;
        private float damage;
        private float explosionRadius;
        private float explosionScaleMultiplier = 1f;
        private float arcSign;
        private bool exploded;
        private Vector3 initialLocalScale;

        private void Awake()
        {
            initialLocalScale = transform.localScale;
        }

        public void Initialize(SnakeSegmentRuntime initialTarget, float moveSpeed, float hitDamage, float radius, float scaleMultiplier)
        {
            target = initialTarget;
            speed = Mathf.Max(0.1f, moveSpeed);
            damage = Mathf.Max(0f, hitDamage);
            float safeScale = Mathf.Max(0.05f, scaleMultiplier);
            explosionRadius = Mathf.Max(0.05f, radius * safeScale);
            explosionScaleMultiplier = safeScale;
            arcSign = Random.value < 0.5f ? -1f : 1f;
            exploded = false;
            hitSegmentIds.Clear();
            transform.localScale = initialLocalScale * safeScale;
        }

        public override void Simulate(float deltaTime)
        {
            if (exploded)
            {
                return;
            }

            if (target == null || target.CurrentHp <= 0f)
            {
                SnakeSegmentRuntime.TryGetLowestHpTarget(out target);
                if (target == null)
                {
                    Vector2 fallbackDir = Vector2.up;
                    transform.position += (Vector3)(fallbackDir * (speed * deltaTime));
                    transform.up = fallbackDir;
                    return;
                }
            }

            Vector3 currentPos = transform.position;
            Vector3 targetPos = target.transform.position;

            Vector2 toTarget = targetPos - currentPos;
            float distance = toTarget.magnitude;
            if (distance <= snapDistance)
            {
                Explode();
                return;
            }

            Vector2 dir = toTarget / Mathf.Max(0.0001f, distance);
            Vector2 side = new(-dir.y, dir.x);
            float curveScale = Mathf.Clamp01(distance / 3f);
            Vector2 moveDir = (dir + side * (curveStrength * arcSign * curveScale)).normalized;

            transform.position += (Vector3)(moveDir * (speed * deltaTime));
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                transform.up = moveDir;
            }
        }

        public override bool ShouldDespawn(float remainingLifetime)
        {
            return exploded || remainingLifetime <= 0f;
        }

        private void Explode()
        {
            if (exploded)
            {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                SnakeSegmentRuntime segment = hits[i].GetComponentInParent<SnakeSegmentRuntime>();
                if (segment == null)
                {
                    continue;
                }

                if (!hitSegmentIds.Add(segment.SegmentId))
                {
                    continue;
                }

                segment.ApplyDamage(damage, transform.position, WeaponType.Missile, explosionScaleMultiplier);
            }

            SpawnExplosionVfx();
            exploded = true;
        }

        private void SpawnExplosionVfx()
        {
            if (explosionVfxPrefab == null)
            {
                return;
            }

            FeedbackSystem.Instance?.SpawnParticleVfx(
                explosionVfxPrefab,
                transform.position,
                Vector3.one * explosionScaleMultiplier);
        }
    }
}
