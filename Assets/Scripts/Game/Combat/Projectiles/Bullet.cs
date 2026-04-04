using System.Collections.Generic;
using GameCamp.Game.Data;
using GameCamp.Game.Snake;
using UnityEngine;

namespace GameCamp.Game.Combat.Projectiles
{
    public class Bullet : Projectile
    {
        [SerializeField] private Vector2 direction = Vector2.up;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 1f;
        [SerializeField] private int pierceRemaining;
        [SerializeField] private bool useUpperYThreshold = true;
        [SerializeField] private float upperYThreshold = 7.5f;

        private readonly HashSet<int> hitSegmentIds = new();
        private Vector3 initialLocalScale;
        private float projectileScaleMultiplier = 1f;

        private void Awake()
        {
            initialLocalScale = transform.localScale;
        }

        public void Initialize(Vector2 newDirection, float newSpeed, float newDamage, int newPierce, float scaleMultiplier)
        {
            direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector2.up;
            speed = Mathf.Max(0f, newSpeed);
            damage = Mathf.Max(0f, newDamage);
            pierceRemaining = Mathf.Max(0, newPierce);
            hitSegmentIds.Clear();
            projectileScaleMultiplier = Mathf.Max(0.05f, scaleMultiplier);
            transform.localScale = initialLocalScale * projectileScaleMultiplier;
        }

        public override void Simulate(float deltaTime)
        {
            transform.position += (Vector3)(direction * (speed * deltaTime));
        }

        public override bool ShouldDespawn(float remainingLifetime)
        {
            if (remainingLifetime <= 0f)
            {
                return true;
            }

            if (useUpperYThreshold && transform.position.y >= upperYThreshold)
            {
                return true;
            }

            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            SnakeSegmentRuntime segment = other.GetComponentInParent<SnakeSegmentRuntime>();
            if (segment == null)
            {
                return;
            }

            if (!hitSegmentIds.Add(segment.SegmentId))
            {
                return;
            }

            segment.ApplyDamage(damage, transform.position, WeaponType.Rifle, projectileScaleMultiplier);

            if (pierceRemaining > 0)
            {
                pierceRemaining--;
                return;
            }

            RequestDespawn();
        }
    }
}
