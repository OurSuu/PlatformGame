using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpForce = 12f;
    public float runJumpMultiplier = 1.25f; // กระโดดสูงขึ้นเมื่อวิ่งและกดกระโดด
    private float currentSpeed;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrain = 25f;  // ลดลงต่อวินาทีขณะวิ่ง
    public float staminaRegen = 7.5f;  // ฟื้นฟูต่อวินาที
    public float staminaRunThreshold = 30f; // stamina ที่ต้องฟื้นถึงก่อนวิ่งได้อีกครั้ง

    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Physics")]
    public float jumpGravityMultiplier = 2.2f;  // คูณ gravity ตอนขึ้นและลง ให้พุ่ง/ตกไวพอๆ กัน

    [Header("Sound")]
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpSoundVolume = 0.7f;
    private AudioSource audioSource;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;
    private bool canMove = true;

    // ระบบ Stamina แบบใหม่
    private bool hasStaminaDepleted = false;
    private float staminaEmptyTime = 0f; // เมื่อ stamina หมด

    // สำหรับคูณ gravity ถ่วง jump
    private float cachedGravityScale = 1f;

    private SpriteRenderer spriteRenderer;

    // State สำหรับวิ่ง
    private bool isCurrentlyRunning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) cachedGravityScale = rb.gravityScale;
        currentStamina = maxStamina;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Stamina ฟื้นได้แม้ซ่อนตัว (canMove = false) แต่ถ้ากด Shift ค้าง = ไม่ฟื้น
        if (!canMove && currentStamina < maxStamina && !Input.GetKey(KeyCode.LeftShift))
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        if (!canMove) return;

        // เช็คพื้น
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // รับ Input
        moveInput = Input.GetAxisRaw("Horizontal");

        // ระบบ Stamina & วิ่ง
        HandleMovementLogic();

        // บันทึกว่า run อยู่หรือไม่
        isCurrentlyRunning = Input.GetKey(KeyCode.LeftShift) && moveInput != 0 && currentSpeed == runSpeed && currentStamina > 0;

        // กระโดด: ปรับ jump ให้ขึ้น/ลง ไว้เท่าๆ กัน ลื่นขึ้น
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            float gravity = Physics2D.gravity.y * cachedGravityScale * jumpGravityMultiplier;
            float targetHeight = jumpForce;

            // ถ้าวิ่งและกดกระโดด - โดดสูงขึ้น
            if (isCurrentlyRunning)
            {
                targetHeight *= runJumpMultiplier;
            }

            float newVy = Mathf.Sqrt(-2f * gravity * targetHeight);

            rb.velocity = new Vector2(rb.velocity.x, newVy);

            // --- Play jump sound ---
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound, jumpSoundVolume);
            }
        }

        // Flip ตลอดเวลาที่เดิน (เหมือน Platformer Classic)
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }

        // เพิ่มความ "ลื่น" ของกระโดด/ตก: กระโดดขึ้นจะเร่งลง, ตกลงก็จะเร่งเหมือนกัน ใช้ตัวคูณเดียว
        if (rb != null && !isGrounded)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (jumpGravityMultiplier - 1f) * Time.deltaTime * cachedGravityScale;
        }
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        }
    }

    void HandleMovementLogic()
    {
        bool isTryingToRun = Input.GetKey(KeyCode.LeftShift) && moveInput != 0;

        if (currentStamina <= 0)
        {
            hasStaminaDepleted = true;
            currentStamina = 0f;
        }

        bool canRun = !hasStaminaDepleted || currentStamina >= staminaRunThreshold;
        if (canRun && currentStamina >= staminaRunThreshold)
            hasStaminaDepleted = false;

        if (isTryingToRun && canRun && currentStamina > 0)
        {
            currentSpeed = runSpeed;
            currentStamina -= staminaDrain * Time.deltaTime;
            if (currentStamina < 0f) currentStamina = 0f;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        if (!Input.GetKey(KeyCode.LeftShift) && currentStamina < maxStamina)
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }
    }

    // Flip direction: กลับมาใช้วิธีปกติที่ platformer ทั่วไปใช้
    void Flip()
    {
        facingRight = !facingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
        else
        {
            Vector3 scaler = transform.localScale;
            scaler.x *= -1;
            transform.localScale = scaler;
        }
    }

    // ฟังก์ชันสำหรับ Script อื่นมาสั่งหยุด/เดิน (เช่นตอนซ่อนตัว)
    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state) rb.velocity = Vector2.zero;
    }
}