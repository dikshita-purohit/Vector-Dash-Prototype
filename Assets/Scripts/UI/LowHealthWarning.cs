using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace VectorDash
{
    /// <summary>
    /// Displays a red blinking warning when player health drops below threshold.
    /// Shows "HEAL" text and pulsing red background panels.
    /// </summary>
    public class LowHealthWarning : MonoBehaviour
    {
        [Header("Player Health")]
        public float health = 100f;
        public float lowHealthThreshold = 40f;

        [Header("Warning UI")]
        public Image redLeft;
        public Image redRight;
        public TextMeshProUGUI healText;

        [Header("Blink Settings")]
        public float blinkSpeed = 4f;
        public float maxAlpha = 0.65f;

        private CanvasGroup redLeftCanvasGroup;
        private CanvasGroup redRightCanvasGroup;
        private bool isWarningActive = false;

        private void Start()
        {
            // Cache CanvasGroups for better performance
            if (redLeft != null && redLeft.GetComponent<CanvasGroup>() == null)
            {
                redLeftCanvasGroup = redLeft.gameObject.AddComponent<CanvasGroup>();
            }
            else if (redLeft != null)
            {
                redLeftCanvasGroup = redLeft.GetComponent<CanvasGroup>();
            }

            if (redRight != null && redRight.GetComponent<CanvasGroup>() == null)
            {
                redRightCanvasGroup = redRight.gameObject.AddComponent<CanvasGroup>();
            }
            else if (redRight != null)
            {
                redRightCanvasGroup = redRight.GetComponent<CanvasGroup>();
            }

            SetWarningVisible(false);
        }

        private void Update()
        {
            if (health < lowHealthThreshold)
            {
                if (!isWarningActive)
                {
                    isWarningActive = true;
                    SetWarningVisible(true);
                }
                ShowLowHealthWarning();
            }
            else
            {
                if (isWarningActive)
                {
                    isWarningActive = false;
                    SetWarningVisible(false);
                }
            }
        }

        private void ShowLowHealthWarning()
        {
            // Calculate blinking alpha using sine wave
            float alpha = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) / 2f;
            alpha *= maxAlpha;

            // Apply to red background images
            if (redLeftCanvasGroup != null)
            {
                redLeftCanvasGroup.alpha = alpha;
            }
            else if (redLeft != null)
            {
                SetAlpha(redLeft, alpha);
            }

            if (redRightCanvasGroup != null)
            {
                redRightCanvasGroup.alpha = alpha;
            }
            else if (redRight != null)
            {
                SetAlpha(redRight, alpha);
            }

            // Blink the HEAL text
            if (healText != null)
            {
                Color textColor = healText.color;
                textColor.a = 0.5f + (alpha * 0.5f);
                healText.color = textColor;
            }
        }

        private void SetWarningVisible(bool visible)
        {
            if (redLeft != null)
                redLeft.gameObject.SetActive(visible);

            if (redRight != null)
                redRight.gameObject.SetActive(visible);

            if (healText != null)
                healText.gameObject.SetActive(visible);
        }

        private void SetAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        /// <summary>
        /// Call this from PlayerController when health changes
        /// </summary>
        public void SetHealth(float currentHealth)
        {
            health = currentHealth;
        }
    }
}
