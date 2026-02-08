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

        Vector3 targetPos = target.position + offset;
        if (useBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        Vector3 smoothPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        smoothPos.z = offset.z;

        // เพิ่ม Screen Shake
        if (screenShake != null)
            smoothPos += screenShake.GetShakeOffset();

        transform.position = smoothPos;
    }
}
