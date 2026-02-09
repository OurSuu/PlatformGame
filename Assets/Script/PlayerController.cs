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

    //[Header("Quick Jump Settings")]
    //public float instantJumpBoost = 2.5f;      // ตัวคูณเพิ่มความเร็วทันทีตอนกระโดด
    // ลบ instantJumpBoost ออก (หรือคอมเมนต์) เพราะจะไม่คูณแรงกระโดด

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) cachedGravityScale = rb.gravityScale;
        currentStamina = maxStamina;
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
            // ความสูงต้องได้ เท่า jumpForce (เดิมคือ .velocity = (x, jumpForce))
            // หาเวลาถึงสูงสุด t = jumpForce / -g  => v = u + at, ตอนถึงยอดสูง v=0 => 0 = jumpVelocity + gravity*t, t = -jumpVelocity/g
            // หากอยากไปถึงยอดไวขึ้น เช่น ครึ่งหนึ่งของเวลาปกติ ต้องเพิ่มความเร่ง y ขึ้น (แต่ให้ความสูงเท่าเดิม)
            // แทนที่จะใช้ velocity เดิม เราใช้ velocity y ที่มากขึ้น แต่ scale gravity เชิง local สำหรับ jump period ก่อนตกดิ่ง
            // แต่ถ้าไม่ยุ่งยาก: ให้ gravity scale คงเดิม แต่ "ปรับสูตร velocity y" เพื่อให้ขึ้นไวขึ้น (โดยประมาณ)

            // วิธี: velocity y ให้มากขึ้น (ไปถึงสุดยอดไวขึ้น) แต่แก้ให้ความสูงสูงสุดเท่าเดิม (h = v^2/2g)
            // อยากกระโดดใช้เวลาน้อยลง เช่น 60% ของเดิม
            float jumpSpeedTweak = 0.55f; // 0.5 = เร็วขึ้นเป็นสองเท่า, 1 = ปกติ; ปรับค่านี้เอาตามต้องการ
            float gravity = Physics2D.gravity.y * cachedGravityScale;
            float targetHeight = jumpForce;

            // สูตร v = sqrt(-2 * g * h) / t_scale
            float newVy = Mathf.Sqrt(-2f * gravity * targetHeight) / jumpSpeedTweak;

            rb.velocity = new Vector2(rb.velocity.x, newVy);
        }

        // กลับด้านตัวละคร
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        // ============ เพิ่มการตกเร็วขึ้น ============
        if (rb != null)
        {
            if (rb.velocity.y < 0)
            {
                // ถ้ากำลังตก ให้ตกเร็วขึ้น
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime * cachedGravityScale;
            }
            else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
            {
                // ถ้ากระโดดแล้วปล่อยปุ่ม ให้ตกเร็วกว่าเดิม (กระโดดเตี้ย)
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime * cachedGravityScale;
            }
        }
        // ============================================
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

        // Stamina หมด = mark depleted หยุดวิ่งทันที
        if (currentStamina <= 0)
        {
            hasStaminaDepleted = true;
            currentStamina = 0f;
        }

        // ต้องฟื้นถึง 30 ถึงจะวิ่งได้อีก
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

        // ฟื้น stamina (กด Shift ค้าง = ไม่ฟื้น)
        if (!Input.GetKey(KeyCode.LeftShift) && currentStamina < maxStamina)
        {
            currentStamina += staminaRegen * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    // ฟังก์ชันสำหรับ Script อื่นมาสั่งหยุด/เดิน (เช่นตอนซ่อนตัว)
    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state) rb.velocity = Vector2.zero;
    }
}