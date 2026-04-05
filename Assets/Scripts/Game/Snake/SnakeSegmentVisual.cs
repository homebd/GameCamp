using GameCamp.Game.Path;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameCamp.Game.Snake
{
    public class SnakeSegmentVisual : MonoBehaviour
    {
        [SerializeField] private Transform[] circles = new Transform[0];
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private int baseSortingOrder = 0;
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
        [Header("Spawn Fade")]
        [SerializeField] private float spawnFadeDuration = 0.2f;

        private bool hasPoseInitialized;
        private float hitFeedbackRemaining;
        private bool hitCacheReady;
        private Color[] baseColors = new Color[0];
        private Vector3[] baseScales = new Vector3[0];
        private Sprite[] baseEnrageSprites = new Sprite[0];
        private SpriteRenderer[] spawnFadeRenderers = new SpriteRenderer[0];
        private float[] spawnFadeTargetAlpha = new float[0];
        private float spawnFadeElapsed;
        private bool spawnFadePlaying;
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

        public void PlaySpawnFadeIn()
        {
            EnsureSpawnFadeRenderers();
            spawnFadeElapsed = 0f;
            spawnFadePlaying = true;
            if (spawnFadeTargetAlpha == null || spawnFadeTargetAlpha.Length != spawnFadeRenderers.Length)
            {
                spawnFadeTargetAlpha = new float[spawnFadeRenderers.Length];
            }

            for (int i = 0; i < spawnFadeRenderers.Length; i++)
            {
                SpriteRenderer renderer = spawnFadeRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color c = renderer.color;
                float targetAlpha = c.a;
                spawnFadeTargetAlpha[i] = targetAlpha;
                c.a = 0f;
                renderer.color = c;
            }
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
            TickSpawnFade();
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

        public void SetSpawnOrderIndex(int spawnOrderIndex)
        {
            if (sortingGroup == null)
            {
                sortingGroup = GetComponentInChildren<SortingGroup>();
            }

            if (sortingGroup == null)
            {
                return;
            }

            int index = Mathf.Max(0, spawnOrderIndex);
            sortingGroup.sortingOrder = baseSortingOrder - index;
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

        private void EnsureSpawnFadeRenderers()
        {
            spawnFadeRenderers = CollectRenderersFromScaleTargets();
        }

        private void TickSpawnFade()
        {
            if (!spawnFadePlaying || spawnFadeRenderers == null || spawnFadeRenderers.Length == 0)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, spawnFadeDuration);
            spawnFadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(spawnFadeElapsed / duration);
            float eased = 1f - ((1f - t) * (1f - t) * (1f - t));

            for (int i = 0; i < spawnFadeRenderers.Length; i++)
            {
                SpriteRenderer renderer = spawnFadeRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color c = renderer.color;
                float target = i < spawnFadeTargetAlpha.Length ? spawnFadeTargetAlpha[i] : 1f;
                c.a = Mathf.Lerp(0f, target, eased);
                renderer.color = c;
            }

            if (t >= 1f)
            {
                spawnFadePlaying = false;
            }
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
