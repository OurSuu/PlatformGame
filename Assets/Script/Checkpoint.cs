using UnityEngine;

/// <summary>
/// วางไว้ตรงไหน = ตรงนั้นคือ Checkpoint
/// เมื่อ Player เดินผ่านจะบันทึก และตายจะไปเกิดที่ Checkpoint ล่าสุด
/// </summary>
public class Checkpoint : MonoBehaviour
{
    private static Vector3 lastCheckpointPosition;
    private static bool hasCheckpoint;

    [Tooltip("ติ้ก = ใช้เป็นจุดเกิดครั้งแรกของ Level (ก่อนโดน Checkpoint อื่น)")]
    public bool setAsInitialSpawn;

    void Awake()
    {
        if (setAsInitialSpawn)
        {
            lastCheckpointPosition = transform.position;
            hasCheckpoint = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // รองรับ Collider บน child ของ Player
        if (other.CompareTag("Player") || other.GetComponentInParent<Health>() != null)
        {
            lastCheckpointPosition = transform.position;
            hasCheckpoint = true;
        }
    }

    /// <summary>
    /// ตำแหน่ง respawn ล่าสุด
    /// </summary>
    public static Vector3 GetSpawnPosition()
    {
        if (hasCheckpoint)
        {
            Vector3 p = lastCheckpointPosition;
            p.z = 0f; // 2D
            return p;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 p = player.transform.position;
            p.z = 0f;
            return p;
        }

        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
