using UnityEngine;

namespace GameCamp.Game.Combat.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        private ProjectilePool ownerPool;

        internal void BindOwner(ProjectilePool pool)
        {
            ownerPool = pool;
        }

        protected void RequestDespawn()
        {
            ownerPool?.Despawn(this);
        }

        public virtual void OnSpawned()
        {
        }

        public virtual void OnDespawned(Transform poolRoot)
        {
            gameObject.SetActive(false);
            transform.SetParent(poolRoot, false);
        }

        public abstract void Simulate(float deltaTime);

        public virtual bool ShouldDespawn(float remainingLifetime)
        {
            return remainingLifetime <= 0f;
        }
    }
}
