using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float jumpForce = 12f;
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

    [Header("Fall Faster Settings")]
    public float fallMultiplier = 2.5f;         // ตัวคูณ gravity ขณะตก
    public float lowJumpMultiplier = 2f;        // ตัวคูณ gravity ถ้าปล่อยปุ่มกระโดดระหว่างขึ้น

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;
    private bool canMove = true;

    // ระบบ Stamina แบบใหม่
    private bool hasStaminaDepleted = false;
    private float staminaEmptyTime = 0f; // เมื่อ stamina หมด

    // --- ปรับสูตรกระโดดใหม่ให้กระโดด "ไวขึ้น" (เวลาไปถึงจุดสูงสุดน้อยลง) แต่ "สูงเท่าเดิมที่ jumpForce ตั้ง" ---
    // เก็บค่าแรงโน้มถ่วงของ rb ตอน start เพื่อใช้คำนวณ velocity jumpStart
    private float cachedGravityScale = 1f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) cachedGravityScale = rb.gravityScale;
        currentStamina = maxStamina;
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // กระโดดแบบใหม่: ปรับสูตรให้กระโดด "ไว" แต่ "สูงเท่า jumpForce"
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            float jumpSpeedTweak = 0.55f;
            float gravity = Physics2D.gravity.y * cachedGravityScale;
            float targetHeight = jumpForce;

            float newVy = Mathf.Sqrt(-2f * gravity * targetHeight) / jumpSpeedTweak;

            rb.velocity = new Vector2(rb.velocity.x, newVy);
        }

        //=============== กลับไป Flip แบบปกติ: ไม่ set localScale.x = -1 หรือ 1 ==============
        // Flip ตลอดเวลาที่เดิน (เหมือน Platformer Classic)
        // (เพื่อป้องกันวาป ให้เช็ค moveInput ทุกเฟรม แล้ว Flip ถ้าทิศเปลี่ยน)
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }

        // ============ เพิ่มการตกเร็วขึ้น ============
        if (rb != null)
        {
            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime * cachedGravityScale;
            }
            else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime * cachedGravityScale;
            }
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