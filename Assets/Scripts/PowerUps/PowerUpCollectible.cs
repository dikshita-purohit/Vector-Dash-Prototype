using System.Collections;
using UnityEngine;

namespace VectorDash
{
    /// <summary>
    /// In-world power-up collectible item that floats in the play area.
    /// Collected when the player dashes or moves into it.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PowerUpCollectible : MonoBehaviour
    {
        [Header("Power Configuration")]
        public PowerUpType powerType = PowerUpType.DoubleBlast;
        public float lifetime = 25f;

        [Header("Juice & Animation")]
        public float bobSpeed = 3.5f;
        public float bobHeight = 0.15f;
        public float rotationSpeed = 90f;
        public float pulseSpeed = 4f;

        private CircleCollider2D col;
        private SpriteRenderer spriteRenderer;
        private Vector3 startPos;
        private float spawnTime;
        private bool isCollected = false;

        private static Sprite cachedCircleSprite;

        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Ensure we have a crisp circular sprite if not assigned
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = GetOrCreateCircleSprite();
            }

            transform.localScale = Vector3.one * 0.7f;
        }

        private void Start()
        {
            startPos = transform.position;
            spawnTime = Time.time;
            ApplyVisualTheme();
            StartCoroutine(LifetimeRoutine());
        }

        public void Initialize(PowerUpType type)
        {
            powerType = type;
            ApplyVisualTheme();
        }

        private void ApplyVisualTheme()
        {
            if (spriteRenderer == null) return;

            Color baseColor;
            switch (powerType)
            {
                case PowerUpType.DoubleBlast:
                    baseColor = new Color(1f, 0.25f, 0.1f, 1f); // Neon Red/Orange
                    gameObject.name = "PowerUp_Blast";
                    break;
                case PowerUpType.HealthIncrease:
                    baseColor = new Color(0.1f, 1f, 0.4f, 1f); // Neon Bright Green
                    gameObject.name = "PowerUp_Health";
                    break;
                case PowerUpType.Protection:
                    baseColor = new Color(0f, 0.9f, 1f, 1f); // Neon Cyan Shield
                    gameObject.name = "PowerUp_Shield";
                    break;
                default:
                    baseColor = Color.yellow;
                    break;
            }

            spriteRenderer.color = baseColor;
        }

        private void Update()
        {
            if (isCollected) return;

            // Bobbing floating motion
            float yOffset = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
            transform.position = startPos + new Vector3(0, yOffset, 0);

            // Subtle rotation and scale pulse
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            float scalePulse = 0.7f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f;
            transform.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Collect(player);
            }
        }

        private void Collect(PlayerController player)
        {
            isCollected = true;

            PlayerPowerController powerCtrl = player.GetComponent<PlayerPowerController>();
            if (powerCtrl == null)
            {
                powerCtrl = player.gameObject.AddComponent<PlayerPowerController>();
            }

            powerCtrl.AcquirePower(powerType);

            // Spawn floating pickup effect
            CreatePickupFX();

            Destroy(gameObject);
        }

        private void CreatePickupFX()
        {
            // Transient expanding pulse ring effect
            GameObject fxObj = new GameObject("PickupFX");
            fxObj.transform.position = transform.position;
            LineRenderer ring = fxObj.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 24;
            ring.startWidth = 0.08f;
            ring.endWidth = 0.02f;

            Color c = spriteRenderer != null ? spriteRenderer.color : Color.cyan;
            ring.startColor = c;
            ring.endColor = new Color(c.r, c.g, c.b, 0f);

            // Ring points
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI * 2f / 24f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.6f, Mathf.Sin(angle) * 0.6f, 0));
            }

            // Animate scale up and destroy
            fxObj.AddComponent<PickupRingAnimator>().Initialize(c);
        }

        private IEnumerator LifetimeRoutine()
        {
            float elapsed = 0f;
            float blinkStartTime = lifetime - 4f;

            while (elapsed < lifetime)
            {
                if (isCollected) yield break;

                elapsed += Time.deltaTime;
                if (elapsed >= blinkStartTime && spriteRenderer != null)
                {
                    // Flash faster as expiration nears
                    float alpha = Mathf.PingPong(elapsed * 8f, 1f) > 0.5f ? 1f : 0.2f;
                    Color c = spriteRenderer.color;
                    c.a = alpha;
                    spriteRenderer.color = c;
                }
                yield return null;
            }

            if (!isCollected)
            {
                Destroy(gameObject);
            }
        }

        public static Sprite GetOrCreateCircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = (size / 2f) - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        // Soft anti-aliased edge and subtle inner core glow
                        float edgeAlpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, edgeAlpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            return cachedCircleSprite;
        }
    }

    /// <summary>
    /// Helper animator to expand and fade the pickup burst ring.
    /// </summary>
    public class PickupRingAnimator : MonoBehaviour
    {
        private LineRenderer ring;
        private Color baseCol;
        private float timer = 0f;
        private const float Duration = 0.4f;

        public void Initialize(Color col)
        {
            baseCol = col;
            ring = GetComponent<LineRenderer>();
            Destroy(gameObject, Duration);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = timer / Duration;

            transform.localScale = Vector3.one * (1f + t * 2.5f);
            if (ring != null)
            {
                Color c = baseCol;
                c.a = Mathf.Lerp(1f, 0f, t);
                ring.startColor = c;
                ring.endColor = new Color(c.r, c.g, c.b, 0f);
            }
        }
    }
}
