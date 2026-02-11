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

    [Header("เสียงตอนรีสปอนที่เช็คพอยท์ (Respawn at Checkpoint Sound)")]
    public AudioClip respawnSound;
    [Range(0f, 1f)]
    public float respawnSoundVolume = 1f;

    private static AudioClip staticRespawnSound;
    private static float staticRespawnSoundVolume = 1f;

    void Awake()
    {
        if (setAsInitialSpawn)
        {
            lastCheckpointPosition = transform.position;
            hasCheckpoint = true;
        }

        // บันทึกคลิปเสียงและวอลลุ่มใน static ครั้งแรกที่มี Checkpoint ใดถูกสร้าง
        if (respawnSound != null)
        {
            staticRespawnSound = respawnSound;
            staticRespawnSoundVolume = respawnSoundVolume;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // รองรับ Collider บน child ของ Player
        if (other.CompareTag("Player") || other.GetComponentInParent<Health>() != null)
        {
            lastCheckpointPosition = transform.position;
            hasCheckpoint = true;
            // ถ้ามี respawnSound ในเช็คพอยท์นี้ ให้ตั้งเป็น static สำหรับเสียง spawn
            if (respawnSound != null)
            {
                staticRespawnSound = respawnSound;
                staticRespawnSoundVolume = respawnSoundVolume;
            }
        }
    }

    /// <summary>
    /// ตำแหน่ง respawn ล่าสุด และเล่นเสียงทุกครั้งที่ respawn
    /// </summary>
    public static Vector3 GetSpawnPosition()
    {
        Vector3 spawn = Vector3.zero;

        if (hasCheckpoint)
        {
            spawn = lastCheckpointPosition;
        }
        else
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                spawn = player.transform.position;
            }
        }
        spawn.z = 0f; // 2D

        // พยายามเล่นเสียง Spawn ที่ Checkpoint (staticRespawnSound)
        if (staticRespawnSound != null)
        {
            // สร้าง GameObject ชั่วคราวเพื่อเล่นเสียง (ในกรณีไม่มี AudioListener ตรงตำแหน่ง)
            var tempObj = new GameObject("TempRespawnSound");
            tempObj.transform.position = spawn;
            AudioSource src = tempObj.AddComponent<AudioSource>();
            src.clip = staticRespawnSound;
            src.volume = staticRespawnSoundVolume;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D sound
            src.Play();
            Object.Destroy(tempObj, staticRespawnSound.length + 0.1f);
        }

        return spawn;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
