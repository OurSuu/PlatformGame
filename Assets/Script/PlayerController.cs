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

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool facingRight = true;
    private bool canMove = true;

    // ระบบ Stamina แบบใหม่
    private bool hasStaminaDepleted = false;
    private float staminaEmptyTime = 0f; // เมื่อ stamina หมด

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
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

        // กระโดด
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // กลับด้านตัวละคร
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
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