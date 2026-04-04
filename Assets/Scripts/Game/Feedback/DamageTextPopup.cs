using TMPro;
using UnityEngine;

namespace GameCamp.Game.Feedback
{
    public class DamageTextPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        private Vector3 origin;
        private float driftX;
        private float riseDistance;
        private float lifetime;
        private float elapsed;
        private Color baseColor;

        public bool IsPlaying { get; private set; }

        public void Play(string message, Color color, float duration, float riseAmount, float randomX)
        {
            if (text == null)
            {
                text = GetComponentInChildren<TMP_Text>();
            }

            if (text != null)
            {
                text.text = message;
                text.color = color;
            }

            origin = transform.position;
            driftX = Random.Range(-Mathf.Abs(randomX), Mathf.Abs(randomX));
            // Keep vertical movement very subtle.
            riseDistance = Mathf.Max(0.01f, riseAmount) * 0.2f;
            lifetime = Mathf.Max(0.05f, duration);
            elapsed = 0f;
            baseColor = color;

            IsPlaying = true;
            gameObject.SetActive(true);
        }

        public void Simulate(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            transform.position = origin + new Vector3(driftX * t, riseDistance * t, 0f);

            if (text != null)
            {
                float alpha = EvaluateAlpha(t);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            if (elapsed >= lifetime)
            {
                IsPlaying = false;
            }
        }

        private static float EvaluateAlpha(float t)
        {
            // Stay crisp for most of the lifetime, then fade out quickly with out-quad.
            const float fadeStart = 0.72f;
            if (t <= fadeStart)
            {
                return 1f;
            }

            float localT = Mathf.InverseLerp(fadeStart, 1f, t);
            float eased = EaseOutQuad(localT);
            return 1f - eased;
        }

        private static float EaseOutQuad(float t)
        {
            float oneMinus = 1f - Mathf.Clamp01(t);
            return 1f - (oneMinus * oneMinus);
        }
    }
}
