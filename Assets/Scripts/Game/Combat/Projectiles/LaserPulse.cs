using System.Collections.Generic;
using GameCamp.Game.Data;
using GameCamp.Game.Feedback;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Combat.Projectiles
{
    public class LaserPulse : Projectile
    {
        private readonly HashSet<SnakeSegmentRuntime> trackedSegments = new();
        private static readonly List<SnakeSegmentRuntime> TickTargets = new();

        private Transform followTarget;
        private float damage;
        private float tickInterval;
        private float tickTimer;
        private Vector3 initialLocalScale;
        private BoxCollider2D boxTrigger;
        private float pulseScaleMultiplier = 1f;

        private void Awake()
        {
            initialLocalScale = transform.localScale;
            boxTrigger = GetComponent<BoxCollider2D>();
        }

        public void Initialize(Transform target, float pulseDamage, float intervalSeconds, float scaleMultiplier)
        {
            followTarget = target;
            damage = Mathf.Max(0f, pulseDamage);
            tickInterval = Mathf.Max(0.01f, intervalSeconds);
            tickTimer = tickInterval;
            trackedSegments.Clear();

            float safeScale = Mathf.Max(0.05f, scaleMultiplier);
            pulseScaleMultiplier = safeScale;
            Vector3 scaled = initialLocalScale;
            scaled.x *= safeScale;
            transform.localScale = scaled;
            if (boxTrigger != null)
            {
                boxTrigger.isTrigger = true;
            }

            if (followTarget != null)
            {
                Vector3 p = followTarget.position;
                transform.position = new Vector3(p.x, p.y, transform.position.z);
            }
        }

        public override void Simulate(float deltaTime)
        {
            if (followTarget != null)
            {
                Vector3 p = followTarget.position;
                transform.position = new Vector3(p.x, p.y, transform.position.z);
            }

            tickTimer -= deltaTime;
            while (tickTimer <= 0f)
            {
                ApplyTickDamage();
                tickTimer += tickInterval;
            }
        }

        private void ApplyTickDamage()
        {
            if (trackedSegments.Count == 0)
            {
                return;
            }

            trackedSegments.RemoveWhere(s => s == null || !s.gameObject.activeInHierarchy);
            TickTargets.Clear();
            TickTargets.AddRange(trackedSegments);

            for (int i = 0; i < TickTargets.Count; i++)
            {
                SnakeSegmentRuntime segment = TickTargets[i];
                if (segment == null)
                {
                    continue;
                }

                segment.ApplyDamage(damage, segment.transform.position, WeaponType.Laser, pulseScaleMultiplier);
                FeedbackSystem.Instance?.SpawnWeaponVfx(WeaponType.Laser, segment.transform.position, pulseScaleMultiplier);
            }
        }

        public override void OnDespawned(Transform poolRoot)
        {
            trackedSegments.Clear();
            base.OnDespawned(poolRoot);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTrackSegment(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTrackSegment(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            SnakeSegmentRuntime segment = other.GetComponentInParent<SnakeSegmentRuntime>();
            if (segment == null)
            {
                return;
            }

            trackedSegments.Remove(segment);
        }

        private void TryTrackSegment(Collider2D other)
        {
            SnakeSegmentRuntime segment = other.GetComponentInParent<SnakeSegmentRuntime>();
            if (segment == null)
            {
                return;
            }

            trackedSegments.Add(segment);
        }
    }
}
