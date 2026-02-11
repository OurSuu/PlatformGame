using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    [Header("BGM ที่ต้องการให้เล่น")]
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        // ระบบ Singleton เพื่อป้องกันไม่ให้มีเพลงซ้อนกันเวลาโหลด Scene กลับไปกลับมา
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // สั่งให้ Object นี้มีชีวิตอยู่ข้าม Scene
            SetupBGM();
        }
        else
        {
            Destroy(gameObject); // ถ้ามี MusicManager อยู่แล้ว ให้ทำลายตัวที่เกิดใหม่ทิ้ง
        }
    }

    private void SetupBGM()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = bgmClip;
        audioSource.volume = bgmVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (bgmClip != null)
        {
            audioSource.Play();
        }
    }
}