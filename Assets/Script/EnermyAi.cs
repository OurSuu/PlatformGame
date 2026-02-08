using UnityEngine;

public class EnermyAi : MonoBehaviour
{
    [Header("Pattern เดิน")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float patrolSpeed = 2f;

    [Header("สายตา - มองเห็น Player")]
    public float sightRange = 8f;
    public float chaseSpeed = 4f;
    public float damagePerSecond = 15f; // ลดเลือดทีละนิดเมื่ออยู่ในสายตา

    [Header("Screen Shake (ไกล=เบา, ใกล้=แรง)")]
    public float shakeFarDistance = 6f;  // ระยะที่เริ่มสั่นเบา
    public float shakeNearDistance = 2f; // ระยะที่สั่นแรงสุด
    public float shakeFarIntensity = 0.03f;
    public float shakeNearIntensity = 0.15f;

    private Transform player;
    private Health playerHealth;
    private HidingSystem playerHiding;
    private SceenShake screenShake;
    private SpriteRenderer sprite;
    private Rigidbody2D rb;

    private bool movingRight = true;
    private bool canSeePlayer;
    private float damageTimer;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerHiding = player.GetComponent<HidingSystem>();
        }

        screenShake = Camera.main?.GetComponent<SceenShake>();
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (leftPoint == null) leftPoint = transform;
        if (rightPoint == null) rightPoint = transform;
        if (leftPoint == rightPoint)
        {
            GameObject left = new GameObject("Enemy_LeftPoint");
            GameObject right = new GameObject("Enemy_RightPoint");
            left.transform.SetParent(transform.parent);
            right.transform.SetParent(transform.parent);
            left.transform.position = transform.position + Vector3.left * 3f;
            right.transform.position = transform.position + Vector3.right * 3f;
            leftPoint = left.transform;
            rightPoint = right.transform;
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                playerHiding = player.GetComponent<HidingSystem>();
            }
            return;
        }
        if (playerHealth != null && playerHealth.IsDead) { canSeePlayer = false; EnemyAlertUI.IsPlayerSpotted = false; return; }

        CheckSight();
        EnemyAlertUI.IsPlayerSpotted = canSeePlayer;

        if (canSeePlayer)
        {
            ChasePlayer();
            DamagePlayerOverTime();
            UpdateScreenShake();
        }
        else
        {
            Patrol();
        }

        FlipSprite();
    }

    void CheckSight()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > sightRange)
        {
            canSeePlayer = false;
            return;
        }
        // ถ้า Player ออกนอก Pattern = หลุดระยะ กลับไป Patrol
        float minX = GetPatternMinX();
        float maxX = GetPatternMaxX();
        if (player.position.x < minX || player.position.x > maxX)
        {
            canSeePlayer = false;
            return;
        }
        // ถ้า Enemy หันหลัง = มองไม่เห็น Player
        bool facingRight = canSeePlayer ? (player.position.x > transform.position.x) : movingRight;
        bool playerInFront = (facingRight && player.position.x > transform.position.x) ||
                            (!facingRight && player.position.x < transform.position.x);
        if (!playerInFront)
        {
            canSeePlayer = false;
            return;
        }
        // ถ้า Player ซ่อนอยู่ มองไม่เห็น
        if (playerHiding != null && playerHiding.isHiding)
        {
            canSeePlayer = false;
            return;
        }
        canSeePlayer = true;
    }

    float GetPatternMinX() => Mathf.Min(leftPoint.position.x, rightPoint.position.x);
    float GetPatternMaxX() => Mathf.Max(leftPoint.position.x, rightPoint.position.x);

    void ClampToPattern()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, GetPatternMinX(), GetPatternMaxX());
        transform.position = pos;
    }

    void Patrol()
    {
        float minX = GetPatternMinX();
        float maxX = GetPatternMaxX();
        float myX = transform.position.x;

        // ถึงขอบซ้ายหรือขวา = กลับทิศทาง
        if (movingRight && myX >= maxX - 0.01f)
            movingRight = false;
        else if (!movingRight && myX <= minX + 0.01f)
            movingRight = true;

        float moveX = (movingRight ? patrolSpeed : -patrolSpeed) * Time.deltaTime;
        transform.Translate(moveX, 0, 0);
        ClampToPattern();
    }

    void ChasePlayer()
    {
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        float moveX = dir * chaseSpeed * Time.deltaTime;
        transform.Translate(moveX, 0, 0);
        ClampToPattern();
    }

    void DamagePlayerOverTime()
    {
        if (playerHealth == null) return;
        damageTimer += Time.deltaTime;
        if (damageTimer >= 0.2f)
        {
            damageTimer = 0f;
            playerHealth.TakeDamage(damagePerSecond * 0.2f);
        }
    }

    void UpdateScreenShake()
    {
        if (screenShake == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float t = Mathf.InverseLerp(shakeFarDistance, shakeNearDistance, dist);
        float intensity = Mathf.Lerp(shakeFarIntensity, shakeNearIntensity, t);
        screenShake.SetShakeIntensity(intensity);
    }

    void FlipSprite()
    {
        if (sprite == null) return;
        bool faceRight = canSeePlayer
            ? player.position.x > transform.position.x
            : movingRight;
        sprite.flipX = !faceRight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null && !h.IsDead && !h.IsInvincible)
            if (playerHiding == null || !playerHiding.isHiding)
                h.Die();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Health h = other.gameObject.GetComponentInParent<Health>();
        if (h != null && !h.IsDead && !h.IsInvincible)
            if (playerHiding == null || !playerHiding.isHiding)
                h.Die();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
