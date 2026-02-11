using UnityEngine;
using UnityEngine.SceneManagement;

public class WinManager : MonoBehaviour
{
    public GameObject winScreenUI; // ลาก Panel WinScreen มาใส่
    private bool hasWon = false;

    // เพิ่มเสียงตอน Player เดินชนเข้า Win
    [Header("Win Sound")]
    public AudioClip winSound;
    [Range(0f, 1f)]
    public float winSoundVolume = 1f;
    private AudioSource audioSource;

    // ชื่อ Scene เเรกสุดที่ต้องเกิดใหม่เสมอ เช่น "GamePlay" ให้เปลี่ยนถ้าตั้งชื่อ Scene อื่น
    [SerializeField]
    private string firstSpawnSceneName = "GamePlay";

    void Start()
    {
        // เตรียม AudioSource สำหรับเล่นเสียง
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    // เพิ่ม Layer "Player" ที่ CheckBox isTrigger!
    void OnTriggerEnter2D(Collider2D other)
    {
        // ตรวจสอบว่าเป็น Player หรือเป็นลูกของ Player (เช่น collider อยู่ที่ child)
        GameObject player = null;
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
        }
        else
        {
            // เช็ค parent เผื่อเป็นลูกของ Player
            Transform parent = other.transform;
            while (parent != null)
            {
                if (parent.CompareTag("Player"))
                {
                    player = parent.gameObject;
                    break;
                }
                parent = parent.parent;
            }
        }

        if (player != null && !hasWon)
        {
            hasWon = true;

            // เล่นเสียงชน Win (ถ้ามีเสียง)
            if (winSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(winSound, winSoundVolume);
            }

            // เด้ง UI พร้อมหยุดเวลา แต่การหยุดเวลาจะเกิดหลัง 1 เฟรม ให้ UI เด้งชัวร์โดยเรียกผ่าน Coroutine
            StartCoroutine(ShowWinUIAndPauseGame());
        }
    }

    // Coroutine ที่ทำให้แน่ใจว่า Win Panel โผล่ขึ้นมาก่อนจะหยุดเกมทันที
    private System.Collections.IEnumerator ShowWinUIAndPauseGame()
    {
        if (winScreenUI != null)
            winScreenUI.SetActive(true); // โชว์ Win Panel

        // รอ 1 เฟรมก่อนหยุดเกม เพื่อแน่ใจว่า UI จะถูก Render แล้ว (แก้ปัญหา UI ไม่ขึ้น)
        yield return null;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // สำหรับปุ่ม Restart: กลับไปเกิดที่จุดเริ่มต้นเเรกของเกม (ไม่ใช่จุดเช็คพอยท์)
    public void RestartGame()
    {
        Time.timeScale = 1f;

        // --- Reset checkpoint position ที่เซฟไว้ (ถ้ามีระบบ Checkpoint ใช้ PlayerPrefs) ---
        // สมมติชื่อ PlayerPrefs ที่ใช้เก็บ checkpoint เป็น "LastCheckpointX", "LastCheckpointY" (หรือปรับตามที่ใช้จริง)
        PlayerPrefs.DeleteKey("LastCheckpointX");
        PlayerPrefs.DeleteKey("LastCheckpointY");
        PlayerPrefs.DeleteKey("LastCheckpointZ");
        // ลบ key อื่นที่เกี่ยวข้องกับเช็คพอยท์ถ้ามีได้ที่นี่
        PlayerPrefs.Save();
        // ------------------------------------------------------------

        // โหลด Scene ต้นเกมใหม่เสมอ (Scene แรกที่กำหนด)
        SceneManager.LoadScene(firstSpawnSceneName);
    }

    // สำหรับปุ่ม Main Menu: กลับสู่หน้าเมนูหลัก
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}