using UnityEngine;

namespace VectorDash
{
    /// <summary>
    /// EnemyAI handles target acquisition, smooth vector tracking towards the player, 
    /// wave speed scaling, attack mechanics, and drain destruction mechanics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Base movement speed towards player.")]
        public float baseSpeed = 3.5f;

        [Tooltip("Maximum tracking acceleration.")]
        public float maxAcceleration = 10f;

        [Header("Attack Settings")]
        [Tooltip("Damage dealt to player upon contact.")]
        public int damage = 20;

        [Tooltip("Cooldown between attacks in seconds.")]
        public float attackCooldown = 0.8f;

        [Header("Juice & Feedback")]
        [Tooltip("Pulse effect frequency when chasing.")]
        public float pulseFrequency = 4f;

        [Tooltip("Pulse scale intensity.")]
        public float pulseMagnitude = 0.08f;

        private Transform playerTransform;
        private Rigidbody2D rb;
        private float currentMoveSpeed;
        private Vector3 initialScale;
        private float lastAttackTime = -10f;
        private bool hasBeenDrained = false;

        public void Initialize(Transform targetPlayer, float speedMultiplier)
        {
            playerTransform = targetPlayer;
            currentMoveSpeed = baseSpeed * speedMultiplier;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            initialScale = transform.localScale;
        }

        private void Start()
        {
            AcquirePlayerTarget();

            if (currentMoveSpeed <= 0f)
            {
                currentMoveSpeed = baseSpeed;
            }
        }

        private void AcquirePlayerTarget()
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                if (GameManager.Instance != null && GameManager.Instance.Player != null && GameManager.Instance.Player.gameObject.activeInHierarchy)
                {
                    playerTransform = GameManager.Instance.Player.transform;
                }
                else
                {
                    GameObject pObj = GameObject.FindWithTag("Player");
                    if (pObj != null)
                    {
                        playerTransform = pObj.transform;
                    }
                    else
                    {
                        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
                        if (pc != null)
                        {
                            playerTransform = pc.transform;
                        }
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            AcquirePlayerTarget();

            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Vector tracking towards player
            Vector2 direction = ((Vector2)playerTransform.position - rb.position).normalized;
            Vector2 targetVelocity = direction * currentMoveSpeed;

            // Smooth force movement towards target velocity
            Vector2 force = (targetVelocity - rb.linearVelocity) * maxAcceleration;
            rb.AddForce(force, ForceMode2D.Force);
        }

        private void Update()
        {
            // Subtle pulse animation during chase for visual juice
            float scaleFactor = 1f + Mathf.Sin(Time.time * pulseFrequency) * pulseMagnitude;
            transform.localScale = initialScale * scaleFactor;
        }

        /// <summary>
        /// Called when player drains this enemy at high velocity.
        /// </summary>
        public void Drain(int scoreReward)
        {
            if (hasBeenDrained)
                return;

            hasBeenDrained = true;

            if (PowerUpSpawner.Instance != null)
            {
                PowerUpSpawner.Instance.OnEnemyDefeated(transform.position);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(scoreReward);
                GameManager.Instance.OnEnemyDestroyed();
            }

            Destroy(gameObject);
        }
    }
}
