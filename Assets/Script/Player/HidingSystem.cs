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

    // เอา UI message สำหรับ "ซ่อนไม่ได้" ออก ไม่มี cannotHideMessageUI อีกต่อไป

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
        // ตัด cannotHideMessageUI ออก (ไม่ต้องซ่อน UI)
    }

    /// <summary>
    /// เรียกเมื่อผู้เล่นกด E ที่จุดซ่อน - ดึงตัวไปกลางจุดแล้ว Freeze
    /// </summary>
    public void EnterHidingSpot(Vector3 centerPosition)
    {
        // ป้องกันซ่อนถ้าอยู่ในระยะที่ศัตรู "มองเห็น"
        if (IsAnyEnemySeePlayer())
        {
            Debug.Log("Cannot hide: You are in enemy sight!");
            // ตัด ShowCannotHideMessage ออก ไม่ต้องแสดงข้อความ
            return;
        }
        if (isHiding) return;

        isHiding = true;
        transform.position = centerPosition;
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

    // ตัด ShowCannotHideMessage และ HideCannotHideMessage ออก

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
}
