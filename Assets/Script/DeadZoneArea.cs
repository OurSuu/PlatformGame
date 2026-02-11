using UnityEngine;
using System.Collections;

public class DeadZoneArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจสอบว่าเป็น Player หรือ child ของ Player (เผื่อ Collider อยู่ที่ลูก)
        if (other.CompareTag("Player") || other.GetComponentInParent<Health>() != null)
        {
            GameObject player = other.CompareTag("Player") ? other.gameObject : other.GetComponentInParent<Health>()?.gameObject;
            if (player != null)
            {
                // ฆ่าผู้เล่น (เช่นรีเซ็ต hp)
                Health health = player.GetComponent<Health>();
                if (health != null)
                {
                    health.Die();
                }

                // รอ 2 วินาทีก่อนเทเลพอร์ตผู้เล่นกลับจุดเช็คพ้อยท์ล่าสุด
                StartCoroutine(TeleportAfterDelay(player, 2f));
            }
        }
    }

    private IEnumerator TeleportAfterDelay(GameObject player, float delay)
    {
        yield return new WaitForSeconds(delay);
        player.transform.position = Checkpoint.GetSpawnPosition();
        // รีเซ็ตความเร็ว/แรงถีบ (ถ้ามี Rigidbody2D)
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        // สามารถ respawn หรือรีเซ็ต health ด้วยถ้าต้องการ เช่น
        // Health health = player.GetComponent<Health>();
        // if (health != null) health.Respawn();
    }
}
