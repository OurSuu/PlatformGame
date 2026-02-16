using UnityEngine;

/// <summary>
/// ติดที่ Main Camera - ติดตาม Player พร้อม Look Ahead, Y Damping
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("ตัวที่จะติดตาม")]
    public Transform target;

    [Header("ตำแหน่ง Camera")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Look Ahead (กล้องมองนำหน้าผู้เล่น)")]
    [Tooltip("ระยะกล้องจะขยับนำหน้าทางที่ผู้เล่นหัน")]
    public float lookAheadDistance = 3f;
    [Tooltip("ความเร็วในการเปลี่ยน Look Ahead")]
    public float lookAheadSpeed = 5f;
    private float currentLookAheadX = 0f;
    private float targetLookAheadX = 0f;

    [Header("Smooth (ความลื่นกล้อง)")]
    public float smoothSpeedX = 5f;
    public float smoothSpeedY = 2f; // ลดการสั่น Y

    [Header("Y Dead Zone (กล้องจะไม่ขยับ Y หากผู้เล่นขยับในระยะนี้)")]
    public float yDeadZone = 0.7f;
    private float cameraTargetY;

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

    // ต้องรู้ "ทิศหันหน้าผู้เล่น" อย่างน้อยให้ลอง auto-detect จาก LocalScale.x แบบเกม 2D Platformer ทั่วไป
    private float lastTargetX;
    private int lastFacingDir = 1;

    void Start()
    {
        if (target == null)
            target = GameObject.FindWithTag("Player")?.transform;

        if (target != null)
        {
            lastTargetX = target.position.x;
            cameraTargetY = target.position.y + offset.y;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // ---------- 1. Look Ahead (หาทิศทางเดิน) ----------
        float moveDeltaX = target.position.x - lastTargetX;
        lastTargetX = target.position.x;

        // หาทิศทาง (ดูจาก localScale.x หรือตรวจสอบ moveDeltaX > 0)
        int facingDir = lastFacingDir;
        if (Mathf.Abs(moveDeltaX) > 0.02f)
        {
            facingDir = moveDeltaX > 0 ? 1 : -1;
            lastFacingDir = facingDir;
        }
        else if (target.localScale.x != 0)
        {
            facingDir = target.localScale.x > 0 ? 1 : -1;
            lastFacingDir = facingDir;
        }

        // โหมด Look Ahead เฉพาะเมื่อเคลื่อนที่เร็วพอ
        if (Mathf.Abs(moveDeltaX) > 0.05f)
            targetLookAheadX = facingDir * lookAheadDistance;
        else
            targetLookAheadX = 0f;
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadSpeed);

        // ---------- 2. Y-Axis Damping/Dead Zone ----------
        float rawTargetY = target.position.y + offset.y;
        float camY = transform.position.y;

        // มี dead zone (ตัวละครกระโดดนิดหน่อย กล้องไม่ขยับ)
        if (Mathf.Abs(rawTargetY - cameraTargetY) > yDeadZone)
        {
            cameraTargetY = Mathf.Lerp(cameraTargetY, rawTargetY, Time.deltaTime * smoothSpeedY);
        }
        // หรือ smooth มากเพื่อไม่ตามไวเกิน (จำกัดความลื่น)
        else
        {
            cameraTargetY = Mathf.Lerp(cameraTargetY, rawTargetY, Time.deltaTime * (smoothSpeedY * 0.5f));
        }

        // ตำแหน่ง x นำ Look Ahead + ลื่น X
        float cameraTargetX = Mathf.Lerp(transform.position.x, target.position.x + offset.x + currentLookAheadX, Time.deltaTime * smoothSpeedX);

        // ---------- 3. กันกล้องทะลุพื้น / กำแพง ----------
        Vector2 desiredPos = new Vector2(cameraTargetX, cameraTargetY);
        Vector2 from = target.position;
        Vector2 to = desiredPos;
        Vector2 dir = to - from;
        float dist = dir.magnitude;

        // ตรวจชน
        if (dist > 0.01f && collisionMask.value != 0)
        {
            RaycastHit2D hit = Physics2D.CircleCast(from, collisionRadius, dir.normalized, dist, collisionMask);
            if (hit.collider != null)
            {
                Vector2 hitPos = hit.point;
                desiredPos = hitPos - dir.normalized * collisionRadius;
            }
        }

        // ---------- 4. จำกัดขอบเขต ----------
        Camera cam = GetComponent<Camera>();
        Vector3 boundedPos = new Vector3(desiredPos.x, desiredPos.y, offset.z);

        if (useBounds)
        {
            // ด้านซ้าย-ขวา
            boundedPos.x = Mathf.Clamp(boundedPos.x, minX, maxX);

            // ด้านล่าง (ถ้ากล้อง orthographic ให้ bottom ของจอ >= minY)
            if (cam != null && cam.orthographic)
            {
                float minCamY = minY + cam.orthographicSize;
                boundedPos.y = Mathf.Clamp(boundedPos.y, minCamY, maxY);
            }
            else
            {
                boundedPos.y = Mathf.Clamp(boundedPos.y, minY, maxY);
            }
        }

        transform.position = boundedPos;
    }

    /// <summary>
    /// เช็คจุดสัมผัสพื้น (ใช้ Raycast ลงล่างหรืออื่นๆ)
    /// ยิ่งดีถ้า PlayerController มี isGrounded สาธารณะ แต่ fallback ด้วย Physics2D
    /// </summary>
    bool IsPlayerGrounded()
    {
        if (target == null)
            return false;
        // Override ด้วย isGrounded ถ้ามี
        var player = target.GetComponent<MonoBehaviour>();
        if (player != null)
        {
            var field = player.GetType().GetField("isGrounded");
            if (field != null)
            {
                bool isGrounded = (bool)field.GetValue(player);
                return isGrounded;
            }
        }
        // fallback: Raycast ลงล่างสั้นๆ
        Vector2 pos = target.position;
        float rayLength = 0.2f;
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, rayLength, collisionMask);
        return hit.collider != null;
    }
}
