using UnityEngine;

/// <summary>
/// ติดที่ Main Camera - สไตล์ Cinematic Horror (มองนำหน้า, สมูท, ไม่ดึงกลับตอนหยุด)
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("ตัวที่จะติดตาม")]
    public Transform target;

    [Header("ตำแหน่ง Camera (จุดอ้างอิง)")]
    [Tooltip("แกน Y ควรเป็นค่าบวก (เช่น 1.5 หรือ 2) เพื่อให้ตัวละครอยู่ขอบล่างจอ")]
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Horror Camera (มองนำหน้า)")]
    [Tooltip("ระยะกล้องจะขยับนำหน้าทางที่ผู้เล่นหัน (ยิ่งเยอะยิ่งเห็นทางข้างหน้าไกล) แนะนำ 3-4")]
    public float lookAheadDistance = 3.5f;
    [Tooltip("ความหนืดเวลาหันหลังกลับ (ค่าน้อย = กล้องจะค่อยๆ แพนช้าๆ ดูหลอนๆ) แนะนำ 2-3")]
    public float lookAheadSpeed = 2.5f;

    private float currentLookAheadX = 0f;
    private float targetLookAheadX = 0f;

    [Header("Smooth (ความหนืดของกล้อง)")]
    [Tooltip("ความหนืดตอนวิ่งตามแกน X (แนะนำ 4-5)")]
    public float smoothSpeedX = 4f;
    [Tooltip("ความหนืดตอนโดดแกน Y (แนะนำ 2-3)")]
    public float smoothSpeedY = 2f;

    [Header("Y Dead Zone (พื้นที่กระโดด/ลงเนินแล้วกล้องไม่ตาม)")]
    [Tooltip("ถ้าโดดเตี้ยกว่าค่านี้ กล้องจะไม่ขยับขึ้นลงตามให้เวียนหัว")]
    public float yDeadZone = 1.0f;

    private float cameraTargetY;

    [Header("จำกัดขอบเขตฉาก (ว่าง = ไม่จำกัด)")]
    public bool useBounds;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    void Start()
    {
        if (target == null)
            target = GameObject.FindWithTag("Player")?.transform;

        if (target != null)
        {
            cameraTargetY = target.position.y;
            // ตั้งค่าเริ่มต้นให้กล้องมองนำหน้าไปเลยแต่แรก
            currentLookAheadX = Mathf.Sign(target.localScale.x) * lookAheadDistance;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. เช็คทิศทางที่ผู้เล่นหัน (อิงจาก Scale X)
        // ถ้าเกมคุณหันหน้าด้วยวิธีอื่น (เช่น flipX) ให้แก้บรรทัดนี้ครับ
        float facingDir = Mathf.Sign(target.localScale.x);

        // 2. Look Ahead (ทิ้งระยะไปข้างหน้าเสมอ ไม่ว่าหยุดหรือเดิน)
        targetLookAheadX = facingDir * lookAheadDistance;
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadSpeed);

        // 3. แกน Y Deadzone (กันกล้องสั่นตอนเดินขึ้นเนินหรือโดดนิดหน่อย)
        if (Mathf.Abs(target.position.y - cameraTargetY) > yDeadZone)
        {
            cameraTargetY = Mathf.Lerp(cameraTargetY, target.position.y, Time.deltaTime * smoothSpeedY);
        }
        // else: ไม่ขยับ Y ให้เวียนหัว

        // 4. คำนวณจุดเป้าหมายที่กล้อง "ควร" จะไปอยู่
        float targetX = target.position.x + currentLookAheadX + offset.x;
        float targetY = cameraTargetY + offset.y;

        // 5. เลื่อนกล้องจากจุดปัจจุบัน ไปหาจุดเป้าหมายอย่างสมูท (Cinematic feel)
        float newX = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * smoothSpeedX);
        float newY = Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * smoothSpeedY);

        Vector3 finalPos = new Vector3(newX, newY, offset.z);

        // 6. จำกัดขอบเขตฉาก
        if (useBounds)
        {
            Camera cam = GetComponent<Camera>();
            finalPos.x = Mathf.Clamp(finalPos.x, minX, maxX);

            if (cam != null && cam.orthographic)
            {
                float minCamY = minY + cam.orthographicSize;
                finalPos.y = Mathf.Clamp(finalPos.y, minCamY, maxY);
            }
            else
            {
                finalPos.y = Mathf.Clamp(finalPos.y, minY, maxY);
            }
        }

        transform.position = finalPos;
    }
}
