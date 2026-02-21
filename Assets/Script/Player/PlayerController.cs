using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    [Tooltip("ความสูงของการกระโดด (ยิ่งเยอะยิ่งโดดสูง) แนะนำ 14 - 16")]
    public float jumpForce = 15f; 
    public float runJumpMultiplier = 1.15f; 
    private float currentSpeed;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrain = 25f;  
    public float staminaRegen = 7.5f;  
    public float staminaRunThreshold = 30f; 

    [Header("Ground Check")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    // --- ฟีเจอร์กระโดดแบบ ลื่นไหลขั้นสุด (Snappy Physics) ---
    [Header("Jump Physics (Gravity Scale)")]
    [Tooltip("คูณแรงโน้มถ่วงตอนพุ่งขึ้น แนะนำ 3")]
    public float jumpGravityMultiplier = 3.0f;  
    
    [Tooltip("คูณแรงโน้มถ่วงตอนร่วงลงมา (ดึงลงไวสะใจ) แนะนำ 5 - 6")]
    public float fallGravityMultiplier = 5.5f;  
    
    [Tooltip("ตัวดึงกระชากลงตอนอยู่จุดสูงสุด (แก้ปัญหาค้างกลางอากาศ 100%) แนะนำ 8 - 10")]
    public float apexGravityMultiplier = 9.0f;

    [Tooltip("ช่วงความเร็วที่ถือว่าเป็นจุดสูงสุด แนะนำ 2.0 - 2.5")]
    public float apexVelocityThreshold = 2.5f;

    [Tooltip("จำกัดความเร็วสูงสุดตอนร่วง ไม่ให้พุ่งลงมาเร็วจนทะลุแมพ แนะนำ 25")]
    public float maxFallSpeed = 25f;
    // ------------------------------------

    [Header("Coyote Time")]
    public float coyoteTime = 0.12f; 
    private float coyoteTimeCounter = 0f;

    [Header("Double Jump")]
    public int maxJumpCount = 2; 
    private int jumpCount = 0;

    [Header("Sound")]
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpSoundVolume = 0.7f;
    public AudioClip walkStepSound;
    public AudioClip runStepSound;
    [Range(0f, 1f)] public float footstepVolume = 0.45f;
    public float walkStepInterval = 0.42f;
    public float runStepInterval = 0.24f;
    public AudioClip landSound;
    [Range(0f, 1f)] public float landSoundVolume = 0.7f;

    private float footstepTimer = 0f;
    private bool wasGroundedLastFrame = false;
    private AudioSource audioSource;
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;
    private bool canMove = true;
    private bool hasStaminaDepleted = false;
    private float cachedGravityScale = 1f;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isCurrentlyRunning = false;
    private bool runSoundActive = false; 
    private float runStepSoundElapsed = 0f;
    private bool isJumpSoundPlaying = false;
    private float jumpSoundTimer = 0f;
    private bool jumpSoundRequest = false;
    private bool isLandSoundPlaying = false;
    private float landSoundTimer = 0f;
    private bool landSoundRequest = false;
    private float fallStartY = 0f;
    private bool wasFalling = false;
    
    [Header("Land Sound Settings")]
    public float landMinFallDistance = 0.6f;  
    public float landMinVelocity = 3.5f; 

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

        fallStartY = transform.position.y;
        wasFalling = false;
    }

    void Update()
    {
        if (!canMove && currentStamina < maxStamina && !Input.GetKey(KeyCode.LeftShift))
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }

        bool prevGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (!isGrounded && rb.velocity.y < -0.2f && !wasFalling)
        {
            wasFalling = true;
            fallStartY = transform.position.y;
        }

        if (wasFalling && isGrounded)
        {
            float fallDistance = fallStartY - transform.position.y;
            if (fallDistance >= landMinFallDistance && Mathf.Abs(rb.velocity.y) > landMinVelocity)
            {
                landSoundRequest = true;
            }
            wasFalling = false;
        }

        if (!canMove)
        {
            SetIdleAnimation();
            StopRunSoundLoop();
            wasGroundedLastFrame = isGrounded;
            return;
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpCount = 0; 
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        moveInput = Input.GetAxisRaw("Horizontal");
        HandleMovementLogic();
        isCurrentlyRunning = Input.GetKey(KeyCode.LeftShift) && moveInput != 0 && currentSpeed == runSpeed && currentStamina > 0;

        if (isJumpSoundPlaying)
        {
            jumpSoundTimer += Time.deltaTime;
            if (jumpSound != null && jumpSoundTimer >= jumpSound.length)
            {
                isJumpSoundPlaying = false;
                jumpSoundTimer = 0f;
            }
        }
        if (isLandSoundPlaying)
        {
            landSoundTimer += Time.deltaTime;
            if (landSound != null && landSoundTimer >= landSound.length)
            {
                isLandSoundPlaying = false;
                landSoundTimer = 0f;
            }
        }

        // --- ระบบ Double Jump (คำนวณแบบ Hollow Knight แท้ๆ) ---
        bool jumpPressed = Input.GetButtonDown("Jump");

        if (jumpPressed)
        {
            if ((coyoteTimeCounter > 0f && jumpCount < 1) || (!isGrounded && jumpCount < maxJumpCount))
            {
                // ดึงสูตรคำนวณที่แม่นยำกลับมาใช้ การันตีโดดสูงถึงเป้าหมายแน่นอน
                float gravity = Physics2D.gravity.y * cachedGravityScale * jumpGravityMultiplier;
                float targetHeight = jumpForce;

                if (isCurrentlyRunning) targetHeight *= runJumpMultiplier;

                float newVy = Mathf.Sqrt(-2f * gravity * targetHeight);
                rb.velocity = new Vector2(rb.velocity.x, newVy);

                if (jumpSound != null && audioSource != null) jumpSoundRequest = true;
                if (animator != null) animator.SetTrigger("Jump");
                if (coyoteTimeCounter > 0f) coyoteTimeCounter = 0f;

                jumpCount++;
            }
        }

        if (jumpSoundRequest && !isJumpSoundPlaying)
        {
            if (jumpSound != null && audioSource != null)
            {
                audioSource.clip = jumpSound;
                audioSource.volume = jumpSoundVolume;
                audioSource.loop = false;
                audioSource.Play();
                isJumpSoundPlaying = true;
                jumpSoundTimer = 0f;
            }
            jumpSoundRequest = false;
        }

        if (landSoundRequest && !isLandSoundPlaying)
        {
            if (landSound != null && audioSource != null)
            {
                audioSource.clip = landSound;
                audioSource.volume = landSoundVolume;
                audioSource.loop = false;
                audioSource.Play();
                isLandSoundPlaying = true;
                landSoundTimer = 0f;
            }
            landSoundRequest = false;
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        UpdateAnimationState();
        HandleFootstepSound();
        HandleRunSoundLoop();

        wasGroundedLastFrame = isGrounded;
    }

    void HandleFootstepSound()
    {
        if (isCurrentlyRunning) 
        {
            footstepTimer = 0f; 
            return;
        }

        bool isMovingOnGround = Mathf.Abs(moveInput) > 0.01f && isGrounded && canMove && Mathf.Abs(rb.velocity.x) > 0.1f;
        if (!isMovingOnGround)
        {
            footstepTimer = 0f;
            return;
        }

        float interval = walkStepInterval;

        if (!wasGroundedLastFrame && isGrounded)
        {
            footstepTimer = interval - 0.02f; 
        }

        footstepTimer += Time.deltaTime;
        if (footstepTimer >= interval)
        {
            if (walkStepSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(walkStepSound, footstepVolume);
            }
            footstepTimer = 0f;
        }
    }

    void HandleRunSoundLoop()
    {
        bool shouldRunSound = isCurrentlyRunning && isGrounded && canMove && runStepSound != null && Mathf.Abs(rb.velocity.x) > 0.1f;

        if (!shouldRunSound && runSoundActive) StopRunSoundLoop();

        if (shouldRunSound)
        {
            if (!runSoundActive)
            {
                audioSource.clip = runStepSound;
                audioSource.volume = footstepVolume;
                audioSource.loop = false;
                audioSource.Play();
                runSoundActive = true;
                runStepSoundElapsed = 0f;
            }
            else
            {
                runStepSoundElapsed += Time.deltaTime;
                if (!audioSource.isPlaying) runSoundActive = false; 
            }
        }
    }

    void StopRunSoundLoop()
    {
        if (runSoundActive)
        {
            audioSource.Stop();
            audioSource.clip = null;
            runSoundActive = false;
            runStepSoundElapsed = 0f;
        }
    }

    void UpdateAnimationState()
    {
        if (animator == null) return;
        bool isFalling = rb.velocity.y < -0.1f && !isGrounded;
        bool isJumpingAnim = rb.velocity.y > 0.1f && !isGrounded;
        bool isMoving = Mathf.Abs(moveInput) > 0.01f;

        animator.SetBool("IsWalking", isMoving && currentSpeed == walkSpeed && isGrounded);
        animator.SetBool("IsRunning", isMoving && currentSpeed == runSpeed && isGrounded);
        animator.SetBool("IsJumping", isJumpingAnim);
        animator.SetBool("IsFalling", isFalling);
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

    // ==========================================
    // จัดการ Physics ความเร็วและ Gravity ทั้งหมดที่นี่
    // ==========================================
    void FixedUpdate()
    {
        if (rb == null) return;

        if (canMove)
        {
            // 1. ความเร็วแนวนอน (การเดินซ้ายขวา)
            rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        }

        // 2. ควบคุม Gravity Scale (แก้ปัญหาหน่วง & ค้างกลางอากาศ 100%)
        if (isGrounded)
        {
            rb.gravityScale = cachedGravityScale; // คืนค่าปกติเมื่ออยู่บนพื้น
        }
        else
        {
            // สเตป 1: ถ้าอยู่ "จุดสูงสุด" ให้กระชาก Gravity ลงมาหนักๆ ทันที
            if (Mathf.Abs(rb.velocity.y) < apexVelocityThreshold)
            {
                rb.gravityScale = cachedGravityScale * apexGravityMultiplier;
            }
            // สเตป 2: ขาลง ดึงลงมาเร็วๆ ให้กระชับ
            else if (rb.velocity.y < 0)
            {
                rb.gravityScale = cachedGravityScale * fallGravityMultiplier;

                // ล็อกความเร็วร่วงสูงสุด (Terminal Velocity) ไม่ให้หล่นทะลุพื้น
                if (rb.velocity.y < -maxFallSpeed)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
                }
            }
            // สเตป 3: ขาขึ้น ใช้ Gravity น้อยๆ เพื่อให้พุ่งขึ้นเร็ว ไม่หน่วง
            else if (rb.velocity.y > 0)
            {
                rb.gravityScale = cachedGravityScale * jumpGravityMultiplier;
            }
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

    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state) rb.velocity = Vector2.zero;

        if (!state)
        {
            SetIdleAnimation();
            StopRunSoundLoop();
        }
    }
}