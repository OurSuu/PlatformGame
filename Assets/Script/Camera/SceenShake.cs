using UnityEngine;

// บรรทัดนี้สำคัญมาก! บังคับให้การสั่นทำงาน "หลัง" จากที่ CameraFollow ตามผู้เล่นเสร็จแล้ว กล้องจะได้ไม่ตีกัน
[DefaultExecutionOrder(100)] 
public class SceenShake : MonoBehaviour
{
    [Header("ตั้งค่าความสั่น")]
    public float decaySpeed = 6f;
    [Tooltip("ปรับเพิ่มเลขนี้ถ้าอยากให้จอสั่นแรงขึ้นอีก")]
    public float maxShakePower = 5f; 

    private float currentIntensity;
    private Vector3 shakeOffset;

    void LateUpdate()
    {
        // 1. ดึงตำแหน่งกล้องกลับมาจุดเดิมก่อน (หักของเก่าทิ้ง)
        transform.position -= shakeOffset;

        // 2. คำนวณความสั่นใหม่
        if (currentIntensity > 0.001f)
        {
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentIntensity * maxShakePower,
                Random.Range(-1f, 1f) * currentIntensity * maxShakePower,
                0f
            );
            // ค่อยๆ ลดความสั่นลงเรื่อยๆ
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, decaySpeed * Time.deltaTime);
        }
        else
        {
            currentIntensity = 0f;
            shakeOffset = Vector3.zero;
        }

        // 3. สั่งให้กล้องขยับจริงๆ! (เอาของใหม่บวกเข้าไป)
        transform.position += shakeOffset;
    }

    /// <summary>
    /// EnermyAi จะเรียกฟังก์ชันนี้ และส่งค่า 0.01 - 0.15 มาให้
    /// </summary>
    public void SetShakeIntensity(float intensity)
    {
        float clamped = Mathf.Clamp01(intensity);
        if (clamped > currentIntensity)
            currentIntensity = clamped;
    }
}