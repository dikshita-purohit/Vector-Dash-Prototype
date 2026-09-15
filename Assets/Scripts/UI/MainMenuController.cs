using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VectorDash
{
    /// <summary>
    /// Manages Main Menu interactions: Play game, How-To-Play modal, Quit,
    /// and displays high score and highest wave stats from PlayerPrefs.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        public string gameplaySceneName;

        [Header("UI Buttons")]
        public Button playButton;
        public Button howToPlayButton;
        public Button closeHowToPlayButton;
        public Button quitButton;

        [Header("Panels")]
        public GameObject howToPlayPanel;

        [Header("Stats Displays")]
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI highestWaveText;

        private void Start()
        {
            Time.timeScale = 1.0f;
            LoadStats();
        }

        public void LoadStats()
        {
            int high = PlayerPrefs.GetInt("HighScore", 0);
            int wave = PlayerPrefs.GetInt("HighestWave", 1);

            if (highScoreText != null)
            {
                highScoreText.text = $"BEST SCORE: {high}";
            }
            if (highestWaveText != null)
            {
                if(wave > 1)
                {
                    highestWaveText.text = $"MAX WAVE: {wave}";
                }
            }
        }

        //assign in inspector on button click event
        public void OnPlayClicked()
        {
            SceneManager.LoadScene(SceneNames.Game);
        }

        public void OnHowToPlayClicked()
        {
            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(true);
            }
        }

        public void OnCloseHowToPlayClicked()
        {
            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(false);
            }
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
