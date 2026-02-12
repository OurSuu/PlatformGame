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

    [Header("Coyote Time (Jump leniency)")]
    public float coyoteTime = 0.17f; // เวลาที่จะอนุญาตให้กระโดดหลังจากไม่อยู่พื้นแล้ว (วินาที)
    private float coyoteTimeCounter = 0f;

    [Header("Double Jump")]
    public int maxJumpCount = 2; // สำหรับ Double Jump กำหนด 2 (โดดได้สองครั้ง)
    private int jumpCount = 0;

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

    // Animator สำหรับควบคุมอนิเมชั่นเดิน วิ่ง กระโดด ตก
    private Animator animator;

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

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found on Player for animation control!");
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

        if (!canMove)
        {
            SetIdleAnimation();
            return;
        }

        // เช็คพื้น
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // Coyote Time - รีเซ็ตหากอยู่พื้น, นับถอยหลังหากไม่อยู่พื้น
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpCount = 0; // รีเซ็ตทุกครั้งที่แตะพื้น
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // รับ Input
        moveInput = Input.GetAxisRaw("Horizontal");

        // ระบบ Stamina & วิ่ง
        HandleMovementLogic();

        // บันทึกว่า run อยู่หรือไม่
        isCurrentlyRunning = Input.GetKey(KeyCode.LeftShift) && moveInput != 0 && currentSpeed == runSpeed && currentStamina > 0;

        // ระบบ Double Jump ---------------------------------------------------------------
        bool jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed)
        {
            // กรณี 1: บนพื้นหรืออยู่ในคอยอตี้ไทม์ และยังไม่ได้โดดเลย
            // กรณี 2: ในอากาศ และ jumpCount < maxJumpCount (โดดครั้งที่ 2)
            if ((coyoteTimeCounter > 0f && jumpCount < 1) ||
                (!isGrounded && jumpCount < maxJumpCount))
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

                // Animation: กระโดด
                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }

                // ถ้าเป็นการโดดจาก coyote/pad (ครั้งแรก) ให้รีเซ็ตคอยอตี้เลย
                if (coyoteTimeCounter > 0f)
                {
                    coyoteTimeCounter = 0f;
                }

                jumpCount++;
            }
        }
        // -------------------------------------------------------------------------------

        // Flip ตลอดเวลาที่เดิน (เหมือน Platformer Classic)
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }

        // Animation Logic
        UpdateAnimationState();

        // เพิ่มความ "ลื่น" ของกระโดด/ตก: กระโดดขึ้นจะเร่งลง, ตกลงก็จะเร่งเหมือนกัน ใช้ตัวคูณเดียว
        if (rb != null && !isGrounded)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (jumpGravityMultiplier - 1f) * Time.deltaTime * cachedGravityScale;
        }
    }

    void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isFalling = rb.velocity.y < -0.1f && !isGrounded;
        bool isJumping = rb.velocity.y > 0.1f && !isGrounded;
        bool isMoving = Mathf.Abs(moveInput) > 0.01f;

        // เดิน/วิ่ง
        animator.SetBool("IsWalking", isMoving && currentSpeed == walkSpeed && isGrounded);
        animator.SetBool("IsRunning", isMoving && currentSpeed == runSpeed && isGrounded);
        // กระโดด
        animator.SetBool("IsJumping", isJumping);
        // ร่วง
        animator.SetBool("IsFalling", isFalling);
        // Idle
        animator.SetBool("IsIdle", !isMoving && isGrounded);
    }

    void SetIdleAnimation()
    {
        if (animator == null) return;
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsFalling", false);
        animator.SetBool("IsIdle", true);
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

        // เมื่อหยุด ให้ตั้งเป็น Idle animation
        if (!state)
        {
            SetIdleAnimation();
        }
    }
}