using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace VectorDash
{
    /// <summary>
    /// GameManager controls wave progression, enemy perimeter spawning, 
    /// score & health UI updates, game state loops, and WebGL/Mobile restart triggers.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player & Enemy Prefab References")]
        public PlayerController Player;
        public GameObject enemyPrefab;

        [Header("Wave System Settings")]
        public int initialEnemiesPerWave = 4;
        public int maxEnemiesPerWave = 20;
        public float spawnRadiusPadding = 2f;
        public float waveDelaySeconds = 2f;

        [Header("UI References")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI waveText;
        public TextMeshProUGUI healthText;
        public Slider healthSlider;
        public GameObject gameOverPanel;
        public TextMeshProUGUI finalScoreText;

        // Internal Game State
        public int Score { get; private set; }
        public int CurrentWave { get; private set; }
        public bool IsGameOver { get; private set; }

        private int activeEnemiesCount = 0;
        private Camera mainCam;
        private bool isSpawningWave = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            mainCam = Camera.main;
            EnsurePlayerSpawned();

            if (PowerUpSpawner.Instance == null)
            {
                PowerUpSpawner spawner = FindFirstObjectByType<PowerUpSpawner>();
                if (spawner == null)
                {
                    gameObject.AddComponent<PowerUpSpawner>();
                }
            }

            if (FindFirstObjectByType<InventoryBarUI>() == null)
            {
                GameObject footer = GameObject.Find("Footer");
                if (footer != null)
                {
                    footer.AddComponent<InventoryBarUI>();
                }
            }
        }

        private void Start()
        {
            Score = 0;
            CurrentWave = 0;
            IsGameOver = false;
            isSpawningWave = false;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            EnsurePlayerSpawned();
            UpdateScoreUI();

            if (Player != null)
            {
                UpdateHealthUI(Player.currentHealth, Player.maxHealth);
            }

            StartCoroutine(StartNextWaveRoutine());
        }

        public void EnsurePlayerSpawned()
        {
            if (mainCam == null) mainCam = Camera.main;

            if (Player == null || !Player.gameObject.scene.IsValid())
            {
                // First try finding an existing PlayerController in the scene
                PlayerController scenePlayer = FindFirstObjectByType<PlayerController>();
                if (scenePlayer != null && scenePlayer.gameObject.scene.IsValid())
                {
                    Player = scenePlayer;
                    return;
                }

                // If not in scene, instantiate from prefab
                GameObject prefabToSpawn = null;
                if (Player != null)
                {
                    prefabToSpawn = Player.gameObject;
                }

                if (prefabToSpawn != null)
                {
                    Vector3 spawnPos = Vector3.zero;
                    if (mainCam != null)
                    {
                        spawnPos = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, 0f);
                    }
                    GameObject playerObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                    playerObj.name = "Player";
                    Player = playerObj.GetComponent<PlayerController>();
                }
            }
        }

        private void Update()
        {
            // WebGL / Desktop quick restart & menu input on game over
            if (IsGameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    RestartGame();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    ReturnToMainMenu();
                }
            }
        }

        /// <summary>
        /// Calculates the enemy count for a given wave.
        /// Increases by 1 every 2 waves, capped at maxEnemiesPerWave.
        /// </summary>
        private int CalculateEnemiesForWave(int waveNumber)
        {
            int enemyCount = initialEnemiesPerWave + ((waveNumber - 1) / 2);
            return Mathf.Min(enemyCount, maxEnemiesPerWave);
        }

        /// <summary>
        /// Calculates the speed multiplier for enemies based on the current wave.
        /// </summary>
        private float CalculateSpeedMultiplier(int waveNumber)
        {
            return 1f + (waveNumber - 1) * 0.15f;
        }

        /// <summary>
        /// Coroutine to launch subsequent wave spawner logic.
        /// Waits for all enemies to be defeated before starting the next wave.
        /// </summary>
        private IEnumerator StartNextWaveRoutine()
        {
            if (isSpawningWave) yield break;

            isSpawningWave = true;
            CurrentWave++;
            UpdateWaveUI();

            if (PowerUpSpawner.Instance != null)
            {
                PowerUpSpawner.Instance.OnWaveStarted(CurrentWave);
            }

            yield return new WaitForSeconds(waveDelaySeconds);

            int countToSpawn = CalculateEnemiesForWave(CurrentWave);
            float speedMultiplier = CalculateSpeedMultiplier(CurrentWave);

            // Spawn all enemies for this wave
            for (int i = 0; i < countToSpawn; i++)
            {
                if (IsGameOver) yield break;

                SpawnEnemyPerimeter(speedMultiplier);
                yield return new WaitForSeconds(0.3f);
            }

            isSpawningWave = false;

            // Wait for all enemies in this wave to be defeated
            while (activeEnemiesCount > 0 && !IsGameOver)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Start next wave only if game is not over
            if (!IsGameOver)
            {
                StartCoroutine(StartNextWaveRoutine());
            }
        }

        /// <summary>
        /// Spawns enemy at a random location along the outside perimeter of the camera view.
        /// </summary>
        private void SpawnEnemyPerimeter(float speedMultiplier)
        {
            if (enemyPrefab == null) return;

            EnsurePlayerSpawned();

            Vector3 spawnPos = GetRandomPerimeterPosition();
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null && Player != null)
            {
                enemyAI.Initialize(Player.transform, speedMultiplier);
            }

            activeEnemiesCount++;
        }

        /// <summary>
        /// Calculates coordinates outside the screen bounds (9:16 aspect viewport).
        /// </summary>
        private Vector3 GetRandomPerimeterPosition()
        {
            if (mainCam == null) mainCam = Camera.main;

            float verticalExtent = mainCam.orthographicSize + spawnRadiusPadding;
            float horizontalExtent = (mainCam.orthographicSize * mainCam.aspect) + spawnRadiusPadding;

            int side = Random.Range(0, 4); // 0: Top, 1: Bottom, 2: Left, 3: Right
            Vector3 spawnPos = Vector3.zero;

            switch (side)
            {
                case 0: // Top
                    spawnPos = new Vector3(Random.Range(-horizontalExtent, horizontalExtent), verticalExtent, 0);
                    break;
                case 1: // Bottom
                    spawnPos = new Vector3(Random.Range(-horizontalExtent, horizontalExtent), -verticalExtent, 0);
                    break;
                case 2: // Left
                    spawnPos = new Vector3(-horizontalExtent, Random.Range(-verticalExtent, verticalExtent), 0);
                    break;
                case 3: // Right
                    spawnPos = new Vector3(horizontalExtent, Random.Range(-verticalExtent, verticalExtent), 0);
                    break;
            }

            return spawnPos;
        }

        public void OnEnemyDestroyed()
        {
            activeEnemiesCount--;
        }

        public void AddScore(int points)
        {
            Score += points;
            UpdateScoreUI();
        }

        public void UpdateHealthUI(int currentHP, int maxHP)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {currentHP}/{maxHP}";
            }
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHP;
                healthSlider.value = currentHP;
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {Score}";
            }
        }

        private void UpdateWaveUI()
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {CurrentWave}";
            }
        }

        public void TriggerGameOver()
        {
            IsGameOver = true;

            int bestScore = PlayerPrefs.GetInt("HighScore", 0);
            if (Score > bestScore)
            {
                bestScore = Score;
                PlayerPrefs.SetInt("HighScore", bestScore);
            }

            int bestWave = PlayerPrefs.GetInt("HighestWave", 1);
            if (CurrentWave > bestWave)
            {
                bestWave = CurrentWave;
                PlayerPrefs.SetInt("HighestWave", bestWave);
            }
            PlayerPrefs.Save();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (finalScoreText != null)
            {
                finalScoreText.text = $"FINAL SCORE\n{Score}\n\n<size=65%>BEST: {bestScore} | WAVE: {CurrentWave}</size>";
            }
            Time.timeScale = 0f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
