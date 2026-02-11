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
