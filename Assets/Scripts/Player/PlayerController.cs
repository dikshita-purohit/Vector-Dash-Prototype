using System.Collections;
using UnityEngine;
using VectorDash;

/// <summary>
/// Controls player movement (drag slingshot & keyboard), speed tracking,
/// health management, damage reception, and combat collisions with enemies.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Component References")]
    public Rigidbody2D rb;
    public CircleCollider2D col;
    public LineRenderer aimLine;
    public SpriteRenderer spriteRenderer;

    [Header("Dash & Movement Settings")]
    public float dashPower = 18f;
    public float maxDashForce = 35f;
    public float maxDragDistance = 3.5f;
    public float minDragDistance = 0.4f;
    public float keyboardSpeed = 8f;

    [Header("Combat & Health")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float killSpeedThreshold = 6.5f;
    public int enemyScoreReward = 10;
    public float invulnerabilityDuration = 0.8f;

    [Header("Screen Boundary Settings")]
    public bool clampToScreen = true;
    public float boundaryPadding = 0.4f;

    [Header("Power Up Component")]
    public PlayerPowerController powerController;

    public float CurrentSpeed { get; internal set; }
    public bool IsInvulnerable { get; private set; }


    public LowHealthWarning lowHealthWarning;

    private Vector2 startTouchPosition;
    private bool isDragging = false;
    private Camera mainCam;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (col == null) col = GetComponent<CircleCollider2D>();
        if (aimLine == null) aimLine = GetComponent<LineRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (powerController == null) powerController = GetComponent<PlayerPowerController>();
        if (powerController == null) powerController = gameObject.AddComponent<PlayerPowerController>();

        // Find LowHealthWarning in the scene if not assigned
        if (lowHealthWarning == null)
        {
            lowHealthWarning = FindFirstObjectByType<LowHealthWarning>();
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (col != null)
        {
            col.radius = 0.5f;
            col.isTrigger = false;
        }

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }

        currentHealth = maxHealth;
        mainCam = Camera.main;
    }

    private void Start()
    {
        if (mainCam == null) mainCam = Camera.main;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        CurrentSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;

        if (currentHealth <= 0) return;

        HandleInput();
        HandleKeyboardMovement();
        EnforceScreenBounds();
    }

    private void HandleInput()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Slingshot drag input (Mouse / Touch)
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            startTouchPosition = GetMouseWorldPosition();

            if (aimLine != null)
            {
                aimLine.enabled = true;
                aimLine.SetPosition(0, transform.position);
                aimLine.SetPosition(1, transform.position);
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector2 currentTouchPosition = GetMouseWorldPosition();
            Vector2 pullVector = startTouchPosition - currentTouchPosition;
            Vector2 clampedAim = Vector2.ClampMagnitude(pullVector, maxDragDistance);

            if (aimLine != null)
            {
                aimLine.enabled = true;
                aimLine.SetPosition(0, transform.position);
                aimLine.SetPosition(1, (Vector2)transform.position + clampedAim);
            }
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (aimLine != null) aimLine.enabled = false;

            Vector2 endTouchPosition = GetMouseWorldPosition();
            Vector2 dragVector = startTouchPosition - endTouchPosition;

            if (dragVector.magnitude >= minDragDistance)
            {
                Vector2 dashDir = dragVector.normalized;
                float impulse = Mathf.Min(dragVector.magnitude * dashPower, maxDashForce);

                rb.linearVelocity = Vector2.zero;
                rb.AddForce(dashDir * impulse, ForceMode2D.Impulse);
            }
        }
    }

    private void HandleKeyboardMovement()
    {
        if (isDragging) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0f || v != 0f)
        {
            Vector2 moveDir = new Vector2(h, v).normalized;
            rb.AddForce(moveDir * keyboardSpeed * 15f * Time.deltaTime, ForceMode2D.Force);
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Input.mousePosition;
        if (mainCam != null)
        {
            mouseScreen.z = -mainCam.transform.position.z;
            return mainCam.ScreenToWorldPoint(mouseScreen);
        }
        return Vector2.zero;
    }

    private void EnforceScreenBounds()
    {
        if (!clampToScreen || mainCam == null) return;

        float vertExtent = mainCam.orthographicSize - boundaryPadding;
        float horzExtent = (mainCam.orthographicSize * mainCam.aspect) - boundaryPadding;

        Vector3 camPos = mainCam.transform.position;
        Vector3 pos = transform.position;

        float minX = camPos.x - horzExtent;
        float maxX = camPos.x + horzExtent;
        float minY = camPos.y - vertExtent;
        float maxY = camPos.y + vertExtent;

        bool bounced = false;
        Vector2 vel = rb.linearVelocity;

        if (pos.x < minX)
        {
            pos.x = minX;
            vel.x = Mathf.Abs(vel.x) * 0.7f;
            bounced = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            vel.x = -Mathf.Abs(vel.x) * 0.7f;
            bounced = true;
        }

        if (pos.y < minY)
        {
            pos.y = minY;
            vel.y = Mathf.Abs(vel.y) * 0.7f;
            bounced = true;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            vel.y = -Mathf.Abs(vel.y) * 0.7f;
            bounced = true;
        }

        transform.position = pos;
        if (bounced)
        {
            rb.linearVelocity = vel;
        }
    }

    /*    public void TakeDamage(int damage)
        {
            if (IsInvulnerable || currentHealth <= 0) return;
            if (powerController != null && powerController.IsShieldActive) return; // Shield blocks all damage

            currentHealth -= damage;
            if (lowHealthWarning != null)
            {
                lowHealthWarning.SetHealth(currentHealth);
            }

            if (currentHealth < 0) currentHealth = 0;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
        }*/

    public void TakeDamage(int damage)
    {
        if (IsInvulnerable || currentHealth <= 0) return;

        if (powerController != null && powerController.IsShieldActive)
            return; // Shield blocks all damage

        currentHealth -= damage;

        // Keep health between 0 and maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update low-health warning
        if (lowHealthWarning != null)
        {
            lowHealthWarning.SetHealth(currentHealth);
        }

        // Update normal health UI
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }



    private void Die()
    {
        rb.linearVelocity = Vector2.zero;
        if (aimLine != null) aimLine.enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;

        if (spriteRenderer != null)
        {
            Color origColor = spriteRenderer.color;
            float elapsed = 0f;
            float blinkInterval = 0.1f;

            while (elapsed < invulnerabilityDuration)
            {
                spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, 0.3f);
                yield return new WaitForSeconds(blinkInterval);
                spriteRenderer.color = origColor;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval * 2f;
            }

            spriteRenderer.color = origColor;
        }
        else
        {
            yield return new WaitForSeconds(invulnerabilityDuration);
        }

        IsInvulnerable = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (currentHealth <= 0) return;

        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            // If shield protection is active, drain enemy and protect player
            if (powerController != null && powerController.IsShieldActive)
            {
                enemy.Drain(enemyScoreReward);
                Vector2 pushDir = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                rb.AddForce(-pushDir * 4f, ForceMode2D.Impulse);
                return;
            }

            // If dashing fast enough, destroy/drain the enemy!
            if (CurrentSpeed >= killSpeedThreshold)
            {
                enemy.Drain(enemyScoreReward);
            }
            else
            {
                // Enemy attacks player
                TakeDamage(enemy.damage);

                // Knock player away slightly
                Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                rb.AddForce(pushDir * 8f, ForceMode2D.Impulse);
            }
        }
    }
}