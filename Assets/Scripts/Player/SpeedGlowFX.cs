using UnityEngine;
using UnityEngine.UI;

namespace VectorDash
{
    /// <summary>
    /// SpeedGlowFX manages the screen-edge drift glow effect during high-speed moves.
    /// Fades UI Image vignette alpha based on player velocity.
    /// </summary>
    public class SpeedGlowFX : MonoBehaviour
    {
        [Header("Target References")]
        public PlayerController player;
        public Image screenGlowImage;

        [Header("Glow Thresholds")]
        public float minSpeedForGlow = 8f;
        public float maxSpeedForFullGlow = 20f;
        public float maxAlpha = 0.5f;
        public float fadeSpeed = 8f;

        private float targetAlpha = 0f;
        private float currentAlpha = 0f;

        private void Start()
        {
            if (player == null || !player.gameObject.scene.IsValid())
            {
                player = FindFirstObjectByType<PlayerController>();
            }
            if (screenGlowImage != null)
            {
                Color c = screenGlowImage.color;
                c.a = 0f;
                screenGlowImage.color = c;
            }
        }

        private void Update()
        {
            if (player == null || !player.gameObject.scene.IsValid())
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
            }
            if (screenGlowImage == null) return;

            float speed = player.CurrentSpeed;

            if (speed >= minSpeedForGlow)
            {
                float t = Mathf.InverseLerp(minSpeedForGlow, maxSpeedForFullGlow, speed);
                targetAlpha = Mathf.Lerp(0.1f, maxAlpha, t);
            }
            else
            {
                targetAlpha = 0f;
            }

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

            Color col = screenGlowImage.color;
            col.a = currentAlpha;
            screenGlowImage.color = col;
        }
    }
}
