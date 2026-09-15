using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VectorDash
{
    /// <summary>
    /// Controls the Loading Scene, smooth async loading of MainMenuScene,
    /// dynamic progress bar animations, status messaging, and gameplay tip cycling.
    /// </summary>
    public class LoadingController : MonoBehaviour
    {
        [Header("Scene Transition Target")]
        public string targetSceneName = SceneNames.MainMenu;
        public float minimumLoadDuration = 2.0f;

        [Header("UI References")]
        public Slider progressBar;
        public Image progressFill;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI tipText;

        private readonly string[] gameplayTips = new string[]
        {
            "Tip: Dash at high speed to drain and destroy enemies!",
            "Tip: Collect glowing orbs in the play area to acquire powers.",
            "Tip: Tap acquired powers in the bottom bar or press 1, 2, 3 to activate.",
            "Tip: Protection shield grants 8 seconds of absolute invulnerability.",
            "Tip: Double Blast obliterates all enemies within a huge radius!"
        };

        private void Start()
        {
            EnsureUIFallback();
            StartCoroutine(LoadTargetSceneRoutine());
            StartCoroutine(TipCycleRoutine());
        }

        private IEnumerator LoadTargetSceneRoutine()
        {
            float timer = 0f;
            AsyncOperation asyncOp = null;

            // Check if scene exists in build settings
            if (Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
                if (asyncOp != null)
                {
                    asyncOp.allowSceneActivation = false;
                }
            }

            while (timer < minimumLoadDuration || (asyncOp != null && asyncOp.progress < 0.9f))
            {
                timer += Time.deltaTime;
                float simulatedProgress = Mathf.Clamp01(timer / minimumLoadDuration);
                float realProgress = asyncOp != null ? Mathf.Clamp01(asyncOp.progress / 0.9f) : 1f;
                float currentProgress = Mathf.Min(simulatedProgress, realProgress);

                UpdateProgressUI(currentProgress);
                yield return null;
            }

            // Fill to 100%
            UpdateProgressUI(1.0f);
            if (statusText != null)
            {
                statusText.text = "READY - ENTERING VECTOR ARENA...";
            }

            yield return new WaitForSeconds(0.35f);

            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
            }
            else
            {
                // Fallback direct load
                SceneManager.LoadScene(targetSceneName);
            }
        }

        private void UpdateProgressUI(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            if (progressFill != null)
            {
                progressFill.fillAmount = progress;
            }
            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }
            if (statusText != null)
            {
                if (progress < 0.35f) statusText.text = "INITIALIZING CORE ENGINES...";
                else if (progress < 0.70f) statusText.text = "CALIBRATING DASH VECTORS...";
                else if (progress < 0.95f) statusText.text = "SYNCHRONIZING ARENA...";
                else statusText.text = "SYSTEMS ONLINE";
            }
        }

        private IEnumerator TipCycleRoutine()
        {
            int tipIdx = 0;
            while (true)
            {
                if (tipText != null && gameplayTips.Length > 0)
                {
                    tipText.text = gameplayTips[tipIdx % gameplayTips.Length];
                    tipIdx++;
                }
                yield return new WaitForSeconds(3f);
            }
        }

        private void EnsureUIFallback()
        {
            // If running on a minimal scene without hooked references, find them by type or name
            if (progressBar == null) progressBar = FindFirstObjectByType<Slider>();
            if (statusText == null)
            {
                TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
                foreach (var t in texts)
                {
                    if (t.name.Contains("Status")) statusText = t;
                    else if (t.name.Contains("Tip")) tipText = t;
                    else if (t.name.Contains("Progress")) progressText = t;
                }
            }
        }
    }
}
