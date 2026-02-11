using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public TMP_Text playButtonText;
    public TMP_Text quitButtonText;

    // เชื่อมปุ่ม Play กับ Scene GamePlay
    public void OnPlayButtonPressed()
    {
        SceneManager.LoadScene("GamePlay");
    }

    // ปุ่ม Quit ใช้ได้จริงตอน build เกม (.exe หรือ package อื่นๆ)
    public void OnQuitButtonPressed()
    {
        Application.Quit();
        #if UNITY_EDITOR
            // ถ้าเล่นใน Editor ให้หยุด Play Mode ด้วย (สำหรับตอน Dev)
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
