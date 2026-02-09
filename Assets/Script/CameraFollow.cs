using UnityEngine;

/// <summary>
/// ติดที่ Main Camera - ติดตาม Player พร้อม Screen Shake
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("ตัวที่จะติดตาม")]
    public Transform target;

    [Header("ตำแหน่ง Camera")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("ความลื่น (0 = ติดทันที, สูง = ลื่นมากขึ้น)")]
    public float smoothSpeed = 5f;

    [Header("กันกล้องทะลุฉาก")]
    [Tooltip("เลือก Layer ของพื้น / กำแพง ที่ไม่อยากให้กล้องทะลุ")]
    public LayerMask collisionMask;
    [Tooltip("รัศมีวงกลมตรวจชนจากตัวละครไปหากล้อง")]
    public float collisionRadius = 0.2f;

    [Header("จำกัดขอบเขต (ว่าง = ไม่จำกัด)")]
    public bool useBounds;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    private SceenShake screenShake;

    void Start()
    {
        if (target == null)
            target = GameObject.FindWithTag("Player")?.transform;

        screenShake = GetComponent<SceenShake>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ใช้ Vector2 สำหรับคำนวณในฉาก 2D
        Vector3 startPos3D = target.position + offset;
        Vector2 targetPos = startPos3D;
        if (useBounds)
        {
            // จำกัดด้านซ้าย-ขวา ด้วยตำแหน่งศูนย์กลางกล้อง
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        }

        // กันกล้องทะลุพื้น / กำแพง (Physics2D)
        Vector2 desiredPos = targetPos;
        Vector2 from = target.position;
        Vector2 to = desiredPos;
        Vector2 dir = to - from;
        float dist = dir.magnitude;

        if (dist > 0.01f && collisionMask.value != 0)
        {
            RaycastHit2D hit = Physics2D.CircleCast(from, collisionRadius, dir.normalized, dist, collisionMask);
            if (hit.collider != null)
            {
                // เลื่อนกล้องให้อยู่หน้า Collider นิดหน่อย
                Vector2 hitPos = hit.point;
                targetPos = hitPos - dir.normalized * collisionRadius;
            }
        }

        // แปลงกลับเป็น Vector3 เพื่อใช้กับตำแหน่งกล้อง (ต้องมีค่า Z)
        Vector3 finalTargetPos = new Vector3(targetPos.x, targetPos.y, offset.z);

        // จำกัดไม่ให้ "ขอบล่างของจอ" ต่ำกว่าค่า minY
        // ให้ตั้งค่า minY เป็นตำแหน่ง Y ของพื้นล่างสุดที่อยากให้เห็น
        if (useBounds)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                float minCamY = minY + cam.orthographicSize; // ให้ bottom ของจอ >= minY
                finalTargetPos.y = Mathf.Clamp(finalTargetPos.y, minCamY, maxY);
            }
            else
            {
                finalTargetPos.y = Mathf.Clamp(finalTargetPos.y, minY, maxY);
            }
        }

        Vector3 smoothPos = Vector3.Lerp(transform.position, finalTargetPos, smoothSpeed * Time.deltaTime);
        smoothPos.z = offset.z;

        // เพิ่ม Screen Shake
        if (screenShake != null)
            smoothPos += screenShake.GetShakeOffset();

        transform.position = smoothPos;
    }
}
