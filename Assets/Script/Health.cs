using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Respawn")]
    public float respawnDelay = 1.5f;
    public float invincibilityDuration = 2f; // กันตายซ้ำทันทีหลัง spawn

    [Header("ฟื้นเลือด")]
    public float regenDelay = 5f;     // รอ 5 วิ หลังโดนดาเมจถึงจะเริ่มฟื้น
    public float regenPerSecond = 5f; // ฟื้นต่อวินาที

    [Header("Sound")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 0.8f;
    public AudioClip damageSound;
    [Range(0f, 1f)] public float damageSoundVolume = 0.8f;
    private AudioSource audioSource;

    private HidingSystem hidingSystem;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private bool isDead;
    private bool isInvincible;
    private float invincibleEndTime;
    private float lastDamageTime;

    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;

    void Start()
    {
        hidingSystem = GetComponent<HidingSystem>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (isInvincible && Time.time >= invincibleEndTime)
        {
            isInvincible = false;
            if (sprite != null) sprite.color = Color.white;
        }

        // ฟื้นเลือดหลัง 5 วิ ไม่โดนดาเมจ
        if (!isDead && currentHealth < maxHealth && currentHealth > 0)
        {
            if (Time.time - lastDamageTime >= regenDelay)
            {
                currentHealth += regenPerSecond * Time.deltaTime;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
            }
        }
    }

    /// <summary>
    /// รับ damage (เรียกจาก Enemy เมื่อ Player อยู่ในสายตา)
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead || isInvincible) return;

        if (amount > 0f && damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound, damageSoundVolume);
        }

        currentHealth -= amount;
        lastDamageTime = Time.time;
        if (currentHealth < 0f) currentHealth = 0f;

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// ตายทันที (ชน Enemy หรือเลือดหมด)
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // เล่นเสียงตาย
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }

        // ออกจากจุดซ่อนถ้าซ่อนอยู่
        if (hidingSystem != null && hidingSystem.isHiding)
            hidingSystem.ExitHidingSpot();

        playerController.SetCanMove(false);
        if (rb != null) rb.velocity = Vector2.zero;
        if (rb != null) rb.simulated = false;

        // ซ่อนการมองเห็นและชน (Invoke ต้องใช้ GameObject ยัง active)
        SetPlayerVisible(false);

        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        Vector3 spawnPos = Checkpoint.GetSpawnPosition();
        transform.position = spawnPos;

        currentHealth = maxHealth;
        isDead = false;
        isInvincible = true;
        invincibleEndTime = Time.time + invincibilityDuration;

        SetPlayerVisible(true);

        playerController.SetCanMove(true);
        if (rb != null) rb.simulated = true;

        if (sprite != null) sprite.color = new Color(1, 1, 1, 0.7f);
    }

    void SetPlayerVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = visible;
    }

    /// <summary>
    /// สำหรับ UI แสดงเลือด
    /// </summary>
    public float GetHealthPercent() => Mathf.Clamp01(currentHealth / maxHealth);
}
