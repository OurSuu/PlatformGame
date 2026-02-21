using UnityEngine;

public class HidingSystem : MonoBehaviour
{
    public bool isHiding = false; // ตัวแปรนี้ศัตรูจะมาแอบดู
    private PlayerController moveScript;
    private SpriteRenderer sprite;
    private Rigidbody2D rb;
    private System.Collections.Generic.Dictionary<Collider2D, bool> originalIsTrigger;

    // เพิ่ม Animator สำหรับเล่นอนิเมชั่น
    private Animator animator;

    // --- เพิ่มส่วนนี้: ปรับตำแหน่งศูนย์กลางจุดซ่อน และเลื่อนข้อความให้ตรงกับกราฟิก ---
    [Header("ปรับตำแหน่งจุดซ่อน (ชดเชยตำแหน่ง Offset)")]
    public Vector3 centerOffset = new Vector3(0, 1f, 0); // สามารถปรับให้เหมาะสมกับ sprite ผู้เล่น

    [Header("ข้อความ & ฟอนต์")]
    public Font customFont; // เช่นเดียวกับ KeySystem

    public string hidingPrompt = "Press E To Hide";
    public string exitPrompt = "Press E To Exit Hiding";
    public string cannotHideText = "Cannot Hide Enermy Saw You!";

    private string displayMessage = "";
    private bool showCannotHideMsg = false;
    private float cannotHideMsgTimer = 0f;
    // -------------------------------------------------------

    // เพิ่มเพื่อเช็คศัตรูเห็นเราไหม
    private bool IsAnyEnemySeePlayer()
    {
        // ดึง Enemy ทั้งหมดในฉาก
        var enemies = GameObject.FindObjectsOfType<EnermyAi>();
        foreach (var enemy in enemies)
        {
            // หาก Enemy เห็นผู้เล่น (canSeePlayer)
            var field = typeof(EnermyAi).GetField("canSeePlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                bool canSee = (bool)field.GetValue(enemy);
                if (canSee)
                    return true;
            }
        }
        return false;
    }

    void Start()
    {
        moveScript = GetComponent<PlayerController>();
        sprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found on Player for hiding animation!");
        }
    }

    /// <summary>
    /// เรียกเมื่อผู้เล่นกด E ที่จุดซ่อน - ดึงตัวไปกลางจุดแล้ว Freeze
    /// </summary>
    public void EnterHidingSpot(Vector3 centerPosition)
    {
        // ปรับตำแหน่งให้ + centerOffset ด้วย
        Vector3 targetPosition = centerPosition + centerOffset;
        // ป้องกันซ่อนถ้าอยู่ในระยะที่ศัตรู "มองเห็น"
        if (IsAnyEnemySeePlayer())
        {
            showCannotHideMsg = true;
            cannotHideMsgTimer = 2.0f;
            displayMessage = cannotHideText;
            Debug.Log("Cannot hide: You are in enemy sight!");
            return;
        }
        if (isHiding) return;

        isHiding = true;
        transform.position = targetPosition;
        rb.velocity = Vector2.zero;

        moveScript.SetCanMove(false);
        sprite.color = new Color(1, 1, 1, 0.4f);
        rb.simulated = false;

        // เล่นอนิเมชั่นซ่อนตัว (ตั้ง Trigger หรือ Bool ให้ตรงกับ Animator ของคุณ)
        if (animator != null)
        {
            animator.SetBool("IsHiding", true);
        }

        // ตั้ง Collider เป็น Trigger เพื่อให้ Enemy เดินทะลุได้
        originalIsTrigger = new System.Collections.Generic.Dictionary<Collider2D, bool>();
        foreach (var c in GetComponentsInChildren<Collider2D>(true))
        {
            originalIsTrigger[c] = c.isTrigger;
            c.isTrigger = true;
        }
    }

    /// <summary>
    /// เรียกเมื่อผู้เล่นกด E อีกครั้งเพื่อออกจากจุดซ่อน
    /// </summary>
    public void ExitHidingSpot()
    {
        if (!isHiding) return;

        isHiding = false;
        moveScript.SetCanMove(true);
        sprite.color = Color.white;
        rb.simulated = true;

        // หยุดอนิเมชั่นซ่อนตัว
        if (animator != null)
        {
            animator.SetBool("IsHiding", false);
        }

        // คืนค่า isTrigger กลับเป็นเดิม
        if (originalIsTrigger != null)
        {
            foreach (var kv in originalIsTrigger)
                if (kv.Key != null) kv.Key.isTrigger = kv.Value;
        }
    }

    // ====== เพิ่มการแสดงข้อความลอยเมื่อตรงจุดซ่อนตัว ======
    void OnGUI()
    {
        if (showCannotHideMsg)
        {
            if (cannotHideMsgTimer > 0f)
            {
                cannotHideMsgTimer -= Time.deltaTime;
                if (Camera.main != null)
                {
                    Vector3 msgPos = Camera.main.WorldToScreenPoint(transform.position + centerOffset);
                    float adjY = Screen.height - msgPos.y;
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    if (customFont != null) style.font = customFont;
                    style.fontSize = 22;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.normal.textColor = Color.red;
                    style.fontStyle = FontStyle.Bold;
                    GUI.Label(new Rect(msgPos.x - 200, adjY - 60, 400, 50), displayMessage, style);
                }
            }
            else
            {
                showCannotHideMsg = false;
            }
        }
        // แสดงปุ่ม prompt "กด E เพื่อซ่อนตัว/ออก" ถ้ายังไม่ซ่อน หรือ "กด E เพื่อออก..." ถ้ากำลังซ่อน
        // (ต้องถูกเรียกใช้จาก HidingSpot)
    }
}
