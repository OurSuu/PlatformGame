using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseScript : MonoBehaviour
{
    public GameObject pausePanel;
    public TMP_Text resumeButtonText;
    public TMP_Text mainMenuButtonText;
    public TMP_Text restartButtonText;

    [Header("เสียงเวลาเปิด/ปิด Pause")]
    public AudioClip pauseSound;
    [Range(0f, 1f)] public float pauseSoundVolume = 0.85f;

    [Header("เสียงปุ่ม Resume (Resume Button Sound)")]
    public AudioClip resumeButtonSound;
    [Range(0f, 1f)] public float resumeButtonSoundVolume = 1f;

    private AudioSource audioSource;

    private bool isPaused = false;

    void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // เล่นเสียงตอนเปิด pause
        PlayPauseSound();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // เล่นเสียงตอนกด Resume (Button/ResumeSound)
        PlayResumeButtonSound();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ระบบ Restart: กลับไปจุดล่าสุดที่ Player ได้ Checkpoint
    public void RestartFromCheckpoint()
    {
        // Resume Time ก่อน
        Time.timeScale = 1f;

        // หา Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // ย้ายตำแหน่งกลับไปที่เช็คพอยท์ล่าสุด
            player.transform.position = Checkpoint.GetSpawnPosition();

            // รีเซ็ตความเร็วของ Rigidbody2D (ถ้ามี)
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        // ปิด Pause Panel
        if (pausePanel != null)
            pausePanel.SetActive(false);

        isPaused = false;
    }

    private void PlayPauseSound()
    {
        if (pauseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pauseSound, pauseSoundVolume);
        }
    }

    private void PlayResumeButtonSound()
    {
        if (resumeButtonSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resumeButtonSound, resumeButtonSoundVolume);
        }
    }
}
