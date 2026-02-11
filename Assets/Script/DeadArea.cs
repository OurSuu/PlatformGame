using UnityEngine;

public class DeadArea : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        // ไม่สนใจสถานะอมตะ หรือ IsInvincible ของผู้เล่น
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && !pc.GetComponent<Health>().IsDead)
        {
            // ฆ่าผู้เล่นทันที โดยข้ามการเช็คอมตะ
            pc.GetComponent<Health>().Die();
        }
    }
}
