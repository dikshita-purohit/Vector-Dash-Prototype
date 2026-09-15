using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VectorDash
{
    /// <summary>
    /// Manages player's acquired power-up inventory, activation triggers,
    /// cooldowns, and executes power effects (Double Blast, Health Increase, Protection Shield).
    /// </summary>
    public class PlayerPowerController : MonoBehaviour
    {
        [Header("Inventory Quantities")]
        public int blastCount = 0;
        public int healthCount = 0;
        public int shieldCount = 0;

        [Header("Double Blast Settings")]
        public float blastRadius = 5.5f;
        public float blastImpulseSpeed = 12f;

        [Header("Health Increase Settings")]
        public int healAmount = 40;

        [Header("Protection Shield Settings")]
        public float shieldDuration = 8f;
        public bool IsShieldActive { get; private set; } = false;

        public event Action<PowerUpType, int> OnPowerInventoryChanged;
        public event Action<float> OnShieldDurationUpdated; // Remaining ratio 0..1

        private PlayerController player;
        private GameObject shieldVisualObj;
        private Coroutine shieldCoroutine;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            // Initial notification for UI sync
            NotifyInventoryChanged(PowerUpType.DoubleBlast);
            NotifyInventoryChanged(PowerUpType.HealthIncrease);
            NotifyInventoryChanged(PowerUpType.Protection);
        }

        private void Update()
        {
            // Quick keyboard activation shortcuts
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                UsePower(PowerUpType.DoubleBlast);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                UsePower(PowerUpType.HealthIncrease);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                UsePower(PowerUpType.Protection);
            }
        }

        public void AcquirePower(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.DoubleBlast:
                    blastCount++;
                    break;
                case PowerUpType.HealthIncrease:
                    healthCount++;
                    break;
                case PowerUpType.Protection:
                    shieldCount++;
                    break;
            }

            NotifyInventoryChanged(type);
        }

        public int GetPowerCount(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.DoubleBlast: return blastCount;
                case PowerUpType.HealthIncrease: return healthCount;
                case PowerUpType.Protection: return shieldCount;
                default: return 0;
            }
        }

        public bool UsePower(PowerUpType type)
        {
            if (GetPowerCount(type) <= 0) return false;

            switch (type)
            {
                case PowerUpType.DoubleBlast:
                    blastCount--;
                    ExecuteDoubleBlast();
                    break;

                case PowerUpType.HealthIncrease:
                    healthCount--;
                    ExecuteHealthIncrease();
                    break;

                case PowerUpType.Protection:
                    shieldCount--;
                    ExecuteProtection();
                    break;
            }

            NotifyInventoryChanged(type);
            return true;
        }

        private void NotifyInventoryChanged(PowerUpType type)
        {
            OnPowerInventoryChanged?.Invoke(type, GetPowerCount(type));
        }

        #region Power Execution Logic

        /// <summary>
        /// Double Blast: creates expanding shockwave ring, drains/eliminates all enemies in blast radius.
        /// </summary>
        private void ExecuteDoubleBlast()
        {
            Vector3 center = transform.position;

            // Spawn visual shockwave ring
            GameObject blastFX = new GameObject("ShockwaveFX");
            blastFX.transform.position = center;
            LineRenderer lr = blastFX.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 36;
            lr.startWidth = 0.25f;
            lr.endWidth = 0.05f;

            Color blastCol = new Color(1f, 0.35f, 0.05f, 1f);
            lr.startColor = blastCol;
            lr.endColor = new Color(1f, 0.7f, 0.1f, 0f);

            for (int i = 0; i < 36; i++)
            {
                float angle = i * Mathf.PI * 2f / 36f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
            }

            blastFX.AddComponent<ShockwaveAnimator>().Initialize(blastRadius, 0.45f);

            // Find all enemies in blast radius
            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector2.Distance(center, enemy.transform.position);
                if (dist <= blastRadius)
                {
                    int reward = player != null ? player.enemyScoreReward : 100;
                    enemy.Drain(reward);
                }
            }

            // Also grant player a short impulse boost if moving
            if (player != null && player.rb != null)
            {
                Vector2 boostDir = player.rb.linearVelocity.sqrMagnitude > 0.01f ? player.rb.linearVelocity.normalized : Vector2.up;
                player.rb.AddForce(boostDir * blastImpulseSpeed, ForceMode2D.Impulse);
            }
        }

        /// <summary>
        /// Health Increase: heals player up to max health and triggers a vibrant green pulse.
        /// </summary>
        private void ExecuteHealthIncrease()
        {
            if (player == null) return;

            player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + healAmount);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateHealthUI(player.currentHealth, player.maxHealth);
            }

            // Green healing aura VFX
            GameObject healFX = new GameObject("HealAuraFX");
            healFX.transform.SetParent(transform, false);
            LineRenderer lr = healFX.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = 30;
            lr.startWidth = 0.15f;
            lr.endWidth = 0.05f;

            Color greenCol = new Color(0.1f, 1f, 0.4f, 1f);
            lr.startColor = greenCol;
            lr.endColor = new Color(0.1f, 1f, 0.4f, 0f);

            for (int i = 0; i < 30; i++)
            {
                float angle = i * Mathf.PI * 2f / 30f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.8f, Mathf.Sin(angle) * 0.8f, 0));
            }

            healFX.AddComponent<ShockwaveAnimator>().Initialize(1.8f, 0.5f);
        }

        /// <summary>
        /// Protection: grants invincible energy shield for shieldDuration seconds.
        /// </summary>
        private void ExecuteProtection()
        {
            if (shieldCoroutine != null)
            {
                StopCoroutine(shieldCoroutine);
            }
            shieldCoroutine = StartCoroutine(ShieldRoutine());
        }

        private IEnumerator ShieldRoutine()
        {
            IsShieldActive = true;
            EnsureShieldVisualCreated();
            if (shieldVisualObj != null)
            {
                shieldVisualObj.SetActive(true);
            }

            float elapsed = 0f;
            while (elapsed < shieldDuration)
            {
                elapsed += Time.deltaTime;
                float remainingRatio = 1f - (elapsed / shieldDuration);
                OnShieldDurationUpdated?.Invoke(remainingRatio);

                // Spin shield visual
                if (shieldVisualObj != null)
                {
                    shieldVisualObj.transform.Rotate(0, 0, 180f * Time.deltaTime);

                    // Blink when last 2 seconds
                    if (remainingRatio < 0.25f)
                    {
                        shieldVisualObj.SetActive(Mathf.PingPong(elapsed * 10f, 1f) > 0.4f);
                    }
                }

                yield return null;
            }

            IsShieldActive = false;
            if (shieldVisualObj != null)
            {
                shieldVisualObj.SetActive(false);
            }
            OnShieldDurationUpdated?.Invoke(0f);
            shieldCoroutine = null;
        }

        private void EnsureShieldVisualCreated()
        {
            if (shieldVisualObj != null) return;

            shieldVisualObj = new GameObject("ShieldAura");
            shieldVisualObj.transform.SetParent(transform, false);
            shieldVisualObj.transform.localPosition = Vector3.zero;

            LineRenderer ring = shieldVisualObj.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.startWidth = 0.12f;
            ring.endWidth = 0.12f;

            Color shieldCol = new Color(0f, 0.9f, 1f, 0.9f);
            ring.startColor = shieldCol;
            ring.endColor = shieldCol;

            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.9f, Mathf.Sin(angle) * 0.9f, 0));
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper animator for expanding radial blast shockwaves.
    /// </summary>
    public class ShockwaveAnimator : MonoBehaviour
    {
        private float maxRadius;
        private float duration;
        private float timer = 0f;
        private LineRenderer lr;

        public void Initialize(float radius, float life)
        {
            maxRadius = radius;
            duration = life;
            lr = GetComponent<LineRenderer>();
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float currentScale = Mathf.Lerp(0.1f, maxRadius, Mathf.Sqrt(t));
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            if (lr != null)
            {
                Color sc = lr.startColor;
                sc.a = Mathf.Lerp(1f, 0f, t);
                lr.startColor = sc;
            }
        }
    }
}
