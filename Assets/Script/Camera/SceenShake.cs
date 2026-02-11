using UnityEngine;

/// <summary>
/// ติดที่ Main Camera - จอสั่นตาม intensity (เรียกจาก Enemy)
/// ทำงานร่วมกับ CameraFollow
/// </summary>
public class SceenShake : MonoBehaviour
{
    [Header("ตั้งค่า")]
    public float decaySpeed = 6f;
    public float maxShakeOffset = 0.5f;

    private float currentIntensity;
    private Vector3 shakeOffset;

    void Update()
    {
        if (currentIntensity > 0.001f)
        {
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentIntensity * maxShakeOffset,
                Random.Range(-1f, 1f) * currentIntensity * maxShakeOffset,
                0f
            );
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, decaySpeed * Time.deltaTime);
        }
        else
        {
            currentIntensity = 0f;
            shakeOffset = Vector3.zero;
        }
    }

    /// <summary>
    /// เรียกทุก frame จาก Enemy - intensity 0-1 (ไกล=เบา, ใกล้=แรง)
    /// </summary>
    public void SetShakeIntensity(float intensity)
    {
        float clamped = Mathf.Clamp01(intensity);
        if (clamped > currentIntensity)
            currentIntensity = clamped;
    }

    /// <summary>
    /// ให้ CameraFollow เรียกใช้ - คืนค่า offset สำหรับเพิ่มในตำแหน่ง camera
    /// </summary>
    public Vector3 GetShakeOffset() => shakeOffset;
}
