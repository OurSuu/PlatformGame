using UnityEngine;

public class DeadArea : MonoBehaviour
{
    [Header("เสียงเมื่อชนกับ DeadArea")]
    public AudioClip hitSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    void OnTriggerEnter2D(Collider2D other)
    {
        // เล่นเสียงทันทีที่ชน (เสียงเฉพาะ DeadArea) ไม่เล่นเสียงตาย
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
        }

        // ไม่สนใจสถานะอมตะ หรือ IsInvincible ของผู้เล่น
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && !pc.GetComponent<Health>().IsDead)
        {
            // ฆ่าผู้เล่นทันที โดยข้ามการเช็คอมตะ และ "ไม่เล่นเสียงตาย"
            pc.GetComponent<Health>().DieSilently();
        }
    }
}
