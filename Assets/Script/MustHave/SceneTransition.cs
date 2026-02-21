using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("ชื่อ Scene ปลายทางที่ต้องการวาร์ปไป")]
    [Tooltip("พิมพ์ชื่อ Scene ให้ตรงกับไฟล์เป๊ะๆ (เช่น Level2, MainMenu)")]
    public string targetSceneName = "Level2";

    [Header("ข้อความ & ฟอนต์")]
    public Font customFont; // 1. เพิ่มตัวแปรฟอนต์

    private bool playerInRange = false;
    private bool showPrompt = false;

    // สร้าง GUI style สำหรับข้อความที่ปรับขนาดตามจอ
    private GUIStyle guiStyle;

    void Start()
    {
        guiStyle = new GUIStyle();
        guiStyle.normal.textColor = Color.white;
        guiStyle.alignment = TextAnchor.MiddleCenter;

        // 2. เช็คและใส่ฟอนต์ตั้งแต่ตอนเริ่มเกม
        if (customFont != null)
        {
            guiStyle.font = customFont;
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("กำลังวาร์ปไปที่: " + targetSceneName);
            Time.timeScale = 1f;
            SceneManager.LoadScene(targetSceneName);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            showPrompt = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            showPrompt = false;
        }
    }

    void OnGUI()
    {
        if (showPrompt)
        {
            // ปรับขนาดฟอนต์ตามความสูงของหน้าจอ (10% ของความสูงหน้าจอ)
            guiStyle.fontSize = Mathf.Max(18, Mathf.RoundToInt(Screen.height * 0.10f));
            string message = "Press E To Transition To " + targetSceneName;
            Rect rect = new Rect(0, Screen.height * 0.8f, Screen.width, Screen.height * 0.1f);
            GUI.Label(rect, message, guiStyle);
        }
    }
}