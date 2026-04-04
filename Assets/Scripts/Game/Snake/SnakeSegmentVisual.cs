using GameCamp.Game.Path;
using UnityEngine;

namespace GameCamp.Game.Snake
{
    public class SnakeSegmentVisual : MonoBehaviour
    {
        [SerializeField] private Transform[] circles = new Transform[0];
        [SerializeField] private float spacing = 0.16f;
        [SerializeField] private float nextSegmentSpacingMultiplier = 1f;
        [SerializeField] private float positionLerpSpeed = 24f;

        [Header("Hit Feedback")]
        [SerializeField] private SpriteRenderer[] hitRenderers = new SpriteRenderer[0];
        [SerializeField] private Transform[] hitScaleTargets = new Transform[0];
        [SerializeField] private float hitFeedbackDuration = 0.14f;
        [SerializeField] private Color hitColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float hitScaleMultiplier = 1.14f;
        [SerializeField] private SpriteRenderer[] enrageSpriteTargets = new SpriteRenderer[0];

        private bool hasPoseInitialized;
        private float hitFeedbackRemaining;
        private bool hitCacheReady;
        private Color[] baseColors = new Color[0];
        private Vector3[] baseScales = new Vector3[0];
        private Sprite[] baseEnrageSprites = new Sprite[0];
        private bool enrageCacheReady;

        public float Spacing => Mathf.Max(0.01f, spacing);

        public float RecommendedNextSegmentSpacing
        {
            get
            {
                int count = circles != null ? circles.Length : 0;
                if (count <= 1)
                {
                    return Spacing;
                }

                return Mathf.Max(0.01f, (count - 1) * Spacing * Mathf.Max(0.1f, nextSegmentSpacingMultiplier));
            }
        }

        public void ResetSmoothing()
        {
            hasPoseInitialized = false;
            hitFeedbackRemaining = 0f;
            EnsureHitCache();
            RestoreHitVisuals();
        }

        public void PlayHitFeedback()
        {
            EnsureHitCache();
            hitFeedbackRemaining = Mathf.Max(0.01f, hitFeedbackDuration);
            UpdateHitFeedbackVisuals();
        }

        public void ApplyPath(PathCurveEvaluator pathCurve, float centerDistance, bool forceSnap = false)
        {
            if (pathCurve == null || !pathCurve.IsValid || circles == null)
            {
                return;
            }

            int count = circles.Length;
            float unitSpacing = Spacing;
            float halfSpanInSteps = (count - 1) * 0.5f;

            bool snapNow = forceSnap || !hasPoseInitialized;
            float posT = snapNow ? 1f : ComputeLerpFactor(positionLerpSpeed);

            for (int i = 0; i < count; i++)
            {
                Transform circle = circles[i];
                if (circle == null)
                {
                    continue;
                }

                float signedStep = halfSpanInSteps - i;
                float d = centerDistance + (signedStep * unitSpacing);

                if (!pathCurve.Evaluate(d, out Vector2 p, out _))
                {
                    continue;
                }

                Vector3 targetPos = new Vector3(p.x, p.y, circle.position.z);
                circle.position = posT >= 1f ? targetPos : Vector3.Lerp(circle.position, targetPos, posT);
            }

            hasPoseInitialized = true;
            TickHitFeedback();
        }

        public void SetEnrageSprite(Sprite sprite)
        {
            EnsureEnrageCache();
            if (!enrageCacheReady)
            {
                return;
            }

            for (int i = 0; i < enrageSpriteTargets.Length; i++)
            {
                SpriteRenderer r = enrageSpriteTargets[i];
                if (r == null)
                {
                    continue;
                }

                r.sprite = sprite != null ? sprite : baseEnrageSprites[i];
            }
        }

        private void TickHitFeedback()
        {
            if (hitFeedbackRemaining <= 0f)
            {
                return;
            }

            hitFeedbackRemaining -= Time.deltaTime;
            if (hitFeedbackRemaining <= 0f)
            {
                hitFeedbackRemaining = 0f;
                RestoreHitVisuals();
                return;
            }

            UpdateHitFeedbackVisuals();
        }

        private void UpdateHitFeedbackVisuals()
        {
            EnsureHitCache();
            if (!hitCacheReady)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, hitFeedbackDuration);
            float intensity = Mathf.Clamp01(hitFeedbackRemaining / duration);
            float progress = 1f - intensity;
            float bump = Mathf.Sin(progress * Mathf.PI);
            float scaleMul = 1f + ((Mathf.Max(1f, hitScaleMultiplier) - 1f) * bump);

            for (int i = 0; i < hitRenderers.Length; i++)
            {
                if (hitRenderers[i] == null)
                {
                    continue;
                }

                hitRenderers[i].color = Color.Lerp(baseColors[i], hitColor, intensity);
            }

            for (int i = 0; i < hitScaleTargets.Length; i++)
            {
                if (hitScaleTargets[i] == null)
                {
                    continue;
                }

                hitScaleTargets[i].localScale = baseScales[i] * scaleMul;
            }
        }

        private void RestoreHitVisuals()
        {
            if (!hitCacheReady)
            {
                return;
            }

            for (int i = 0; i < hitRenderers.Length; i++)
            {
                if (hitRenderers[i] == null)
                {
                    continue;
                }

                hitRenderers[i].color = baseColors[i];
            }

            for (int i = 0; i < hitScaleTargets.Length; i++)
            {
                if (hitScaleTargets[i] == null)
                {
                    continue;
                }

                hitScaleTargets[i].localScale = baseScales[i];
            }
        }

        private void EnsureHitCache()
        {
            if (hitCacheReady)
            {
                return;
            }

            if (hitScaleTargets == null || hitScaleTargets.Length == 0)
            {
                hitScaleTargets = circles;
            }

            if (hitRenderers == null || hitRenderers.Length == 0)
            {
                hitRenderers = CollectRenderersFromScaleTargets();
            }

            baseColors = new Color[hitRenderers.Length];
            for (int i = 0; i < hitRenderers.Length; i++)
            {
                baseColors[i] = hitRenderers[i] != null ? hitRenderers[i].color : Color.white;
            }

            baseScales = new Vector3[hitScaleTargets.Length];
            for (int i = 0; i < hitScaleTargets.Length; i++)
            {
                baseScales[i] = hitScaleTargets[i] != null ? hitScaleTargets[i].localScale : Vector3.one;
            }

            hitCacheReady = true;
        }

        private SpriteRenderer[] CollectRenderersFromScaleTargets()
        {
            if (hitScaleTargets == null)
            {
                return new SpriteRenderer[0];
            }

            SpriteRenderer[] result = new SpriteRenderer[hitScaleTargets.Length];
            for (int i = 0; i < hitScaleTargets.Length; i++)
            {
                result[i] = hitScaleTargets[i] != null ? hitScaleTargets[i].GetComponent<SpriteRenderer>() : null;
            }

            return result;
        }

        private void EnsureEnrageCache()
        {
            if (enrageCacheReady)
            {
                return;
            }

            if (enrageSpriteTargets == null || enrageSpriteTargets.Length == 0)
            {
                enrageSpriteTargets = CollectRenderersFromScaleTargets();
            }

            baseEnrageSprites = new Sprite[enrageSpriteTargets.Length];
            for (int i = 0; i < enrageSpriteTargets.Length; i++)
            {
                baseEnrageSprites[i] = enrageSpriteTargets[i] != null ? enrageSpriteTargets[i].sprite : null;
            }

            enrageCacheReady = true;
        }

        private static float ComputeLerpFactor(float speed)
        {
            if (speed <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }
    }
}
