using UnityEngine;

public class SFXManage : MonoBehaviour
{
    [Header("เสียงปุ่มนี้ (Only this button's SFX)")]
    public AudioClip buttonSFX;
    [Range(0f, 1f)]
    public float volume = 1f;

    // หมายเหตุ: ไม่ต้องใช้ AudioSource บน GameObject ต้นฉบับเพื่อรองรับข้าม scene

    /// <summary>
    /// เรียกจากปุ่ม (ex. OnClick) เพื่อเล่นเสียงปุ่มนี้ หลังจากนั้น Destroy ตามความยาวคลิป
    /// สามารถย้าย scene ได้ เสียงจะดังจนครบ
    /// </summary>
    public void PlayButtonSFX()
    {
        if (buttonSFX != null)
        {
            // สร้าง GameObject temporary อยู่นอก hierarchy หลัก เพื่อเสียงไม่ถูกตัดเมือย้าย scene
            var tempObj = new GameObject("TempButtonSFX");
            // ป้องกันการลบเมื่อโหลด scene ใหม่
            Object.DontDestroyOnLoad(tempObj);

            tempObj.transform.position = Vector3.zero;
            var tempAudio = tempObj.AddComponent<AudioSource>();
            tempAudio.clip = buttonSFX;
            tempAudio.volume = volume;
            tempAudio.playOnAwake = false;
            tempAudio.spatialBlend = 0f; // 2D

            tempAudio.Play();

            // กำหนดลบ tempObject หลังเสียงจบ (plus little buffer)
            Object.Destroy(tempObj, buttonSFX.length + 0.1f);
        }
    }
}
