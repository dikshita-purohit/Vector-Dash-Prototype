using UnityEngine;

namespace VectorDash
{
    /// <summary>
    /// Spawns power-up items in the arena when waves progress or when enemies are destroyed.
    /// Supports both prefab-based and procedural runtime instantiation.
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        public static PowerUpSpawner Instance { get; private set; }

        [Header("Prefab Reference (Optional - Procedural Fallback included)")]
        public GameObject powerUpPrefab;

        [Header("Drop Chances & Balancing")]
        [Range(0f, 1f)]
        public float enemyDropChance = 0.32f;
        public float spawnPaddingFromEdge = 1.0f;

        private Camera mainCam;

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
        }

        /// <summary>
        /// Spawns bonus power-ups in the arena upon wave progression.
        /// </summary>
        public void OnWaveStarted(int waveNumber)
        {
            if (waveNumber < 1) return;

            // Spawn 1 power-up every wave, and 2 power-ups every 3rd wave
            int countToSpawn = (waveNumber % 3 == 0) ? 2 : 1;

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos = GetRandomArenaPosition();
                PowerUpType type = ChooseWeightedPowerType();
                SpawnPowerUp(spawnPos, type);
            }
        }

        /// <summary>
        /// Called when an enemy is destroyed. Rolls a drop chance.
        /// </summary>
        public void OnEnemyDefeated(Vector3 enemyPosition)
        {
            if (Random.value <= enemyDropChance)
            {
                PowerUpType type = ChooseWeightedPowerType();
                SpawnPowerUp(enemyPosition, type);
            }
        }

        public GameObject SpawnPowerUp(Vector3 position, PowerUpType type)
        {
            GameObject itemObj;

            if (powerUpPrefab != null)
            {
                itemObj = Instantiate(powerUpPrefab, position, Quaternion.identity);
            }
            else
            {
                // Procedural generation fallback
                itemObj = new GameObject($"PowerUp_{type}");
                itemObj.transform.position = position;
                itemObj.AddComponent<SpriteRenderer>();
                itemObj.AddComponent<CircleCollider2D>();
            }

            PowerUpCollectible collectible = itemObj.GetComponent<PowerUpCollectible>();
            if (collectible == null)
            {
                collectible = itemObj.AddComponent<PowerUpCollectible>();
            }

            collectible.Initialize(type);
            return itemObj;
        }

        private PowerUpType ChooseWeightedPowerType()
        {
            // If player health is low, bias towards health
            if (GameManager.Instance != null && GameManager.Instance.Player != null)
            {
                PlayerController p = GameManager.Instance.Player;
                if (p.currentHealth < p.maxHealth * 0.45f)
                {
                    if (Random.value < 0.65f) return PowerUpType.HealthIncrease;
                }
            }

            // Balanced 3-way random distribution
            float roll = Random.value;
            if (roll < 0.38f) return PowerUpType.DoubleBlast;
            if (roll < 0.70f) return PowerUpType.Protection;
            return PowerUpType.HealthIncrease;
        }

        private Vector3 GetRandomArenaPosition()
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return Vector3.zero;

            float vertExtent = mainCam.orthographicSize - spawnPaddingFromEdge;
            float horzExtent = (mainCam.orthographicSize * mainCam.aspect) - spawnPaddingFromEdge;

            Vector3 camPos = mainCam.transform.position;
            float rx = Random.Range(camPos.x - horzExtent, camPos.x + horzExtent);
            float ry = Random.Range(camPos.y - vertExtent, camPos.y + vertExtent);

            return new Vector3(rx, ry, 0f);
        }
    }
}
