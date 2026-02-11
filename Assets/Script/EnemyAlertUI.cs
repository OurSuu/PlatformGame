using UnityEngine;

/// <summary>
/// แสดงข้อความ "หาที่แอบ!!!" กลางหน้าจอด้านบน ตัวใหญ่ๆ เมื่อ Enemy ส่องเจอ หรือเลือดผู้เล่นลด
/// </summary>
public class EnemyAlertUI : MonoBehaviour
{
    public static bool IsPlayerSpotted { get; set; }
    public static bool IsPlayerDamaged { get; set; }

    private GUIStyle style;
    private bool styleReady;

    // กำหนดเวลาที่จะโชว์ข้อความต่อเนื่องถ้าโดนดาเมจ
    private static float damagedShowDuration = 2f;
    private static float damagedTimer = 0f;

    // สำหรับ Shake Effect
    private float shakeIntensity = 6f; // ลดแรงสั่นเหลือ 6px (จาก 18) ให้ดูน่ากลัวแต่ยังอ่านรู้เรื่อง
    private float shakeFrequency = 18f; // ลดความถี่ลงเหลือ 18Hz (จาก 30)

    void Update()
    {
        // อัปเดตสถานะโดนดาเมจ (ถ้า Health ลดต้องไปตั้งค่า IsPlayerDamaged = true)
        if (IsPlayerDamaged)
        {
            damagedTimer = damagedShowDuration;
            IsPlayerDamaged = false; // เคลียร์เพื่อรอรอบใหม่
        }

        if (damagedTimer > 0f)
            damagedTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (IsPlayerSpotted || damagedTimer > 0f)
        {
            if (!styleReady)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 56; // ตัวใหญ่ๆ
                style.alignment = TextAnchor.UpperCenter;
                style.normal.textColor = Color.red;
                style.fontStyle = FontStyle.Bold;
                styleReady = true;
            }

            float labelWidth = 600f;
            float labelHeight = 80f;
            float x = (Screen.width - labelWidth) / 2f;
            float y = 40f; // ห่างขอบบนลงมาหน่อย

            // ลดความสั่นให้พอมี motion รุนแรงแต่ไม่อ่านยาก
            float shakeAmount = (IsPlayerSpotted || damagedTimer > 0f) ? shakeIntensity : 0f;
            float t = Time.time * shakeFrequency;

            // ลด random ลงนิดหน่อย ปรับการคูณของ random เป็น 0.7f เพื่อให้ไม่สวิงสุดขอบ
            float offsetX = Random.Range(-0.7f, 0.7f) * shakeAmount + Mathf.Sin(t) * (shakeAmount * 0.25f);
            float offsetY = Random.Range(-0.7f, 0.7f) * shakeAmount + Mathf.Cos(t * 1.1f) * (shakeAmount * 0.2f);

            Rect labelRect = new Rect(x + offsetX, y + offsetY, labelWidth, labelHeight);
            GUI.Label(labelRect, "⚠ แอบเร็วว!!!", style);
        }
    }

    /// <summary>
    /// ฟังก์ชันนี้ให้ Health เรียกเมื่อเลือดผู้เล่นลด
    /// </summary>
    public static void NotifyPlayerDamaged()
    {
        IsPlayerDamaged = true;
    }
}
