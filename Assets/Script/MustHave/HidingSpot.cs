using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("จุดกลาง (ว่างไว้ใช้ตำแหน่ง Object นี้)")]
    public Transform centerPoint;

    [Header("ระยะกด E ได้ (ใหญ่ = กดจากไกลได้)")]
    public float interactionRadius = 3f;

    [Header("ข้อความ & ฟอนต์")]
    public Font customFont; // 1. เพิ่มบรรทัดนี้เข้ามา
    public string promptText = "Press E To Hide";
    public string cannotHideText = "Cannot Hide Enermy Saw You!"; // ข้อความกรณีซ่อนไม่ได้

    [Header("เสียงขณะซ่อนตัว")]
    public AudioClip hidingSound; // เอฟเฟกต์เสียงขณะเข้าจุดซ่อน
    public float hidingSoundVolume = 1.0f;
    private AudioSource audioSource;

    private bool playerInRange = false;
    private HidingSystem playerHidingScript;
    private GUIStyle promptStyle;
    private bool styleReady;

    // สำหรับเช็ค state ของเสียง looping
    private bool isHidingLoopPlaying = false;

    // State สำหรับ error message
    private bool cannotHideMsgActive = false;
    private float cannotHideMsgTimer = 0f;
    private string cannotHideMsg = "";

    void Awake()
    {
        if (centerPoint == null)
            centerPoint = transform;

        // เตรียม AudioSource สำหรับเล่นเสียงซ่อนตัว
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && hidingSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        CheckPlayerInRange();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (playerHidingScript != null)
            {
                if (!playerHidingScript.isHiding)
                {
                    // ก่อนซ่อน: ตรวจสอบว่าศัตรูเห็นหรือไม่
                    var field = typeof(HidingSystem).GetMethod("IsAnyEnemySeePlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        bool isSee = (bool)field.Invoke(playerHidingScript, null);
                        if (isSee)
                        {
                            // แสดงข้อความ "ไม่สามารถซ่อนได้ (ศัตรูกำลังเห็นคุณ!)"
                            ShowCannotHideMessage(cannotHideText);
                            return;
                        }
                    }
                    playerHidingScript.EnterHidingSpot(centerPoint.position);
                    // เริ่มเสียง looping เมื่อเข้าจุดซ่อน
                    StartHidingLoop();
                }
                else
                {
                    playerHidingScript.ExitHidingSpot();
                    // หยุดเสียง looping เมื่อออกจากจุดซ่อน
                    StopHidingLoop();
                }
            }
        }

        // หาก player เดินออกจาก range แล้วกำลังซ่อนอยู่ ให้หยุด sound ทันที (safety)
        if ((!playerInRange || playerHidingScript == null || !playerHidingScript.isHiding) && isHidingLoopPlaying)
        {
            StopHidingLoop();
        }

        // timer สำหรับ hide message
        if (cannotHideMsgActive)
        {
            cannotHideMsgTimer -= Time.deltaTime;
            if (cannotHideMsgTimer <= 0f)
            {
                cannotHideMsgActive = false;
                cannotHideMsg = "";
            }
        }
    }

    private void StartHidingLoop()
    {
        if (audioSource != null && hidingSound != null && !isHidingLoopPlaying)
        {
            audioSource.loop = true;
            audioSource.clip = hidingSound;
            audioSource.volume = hidingSoundVolume;
            audioSource.Play();
            isHidingLoopPlaying = true;
        }
    }

    private void StopHidingLoop()
    {
        if (audioSource != null && isHidingLoopPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
            isHidingLoopPlaying = false;
        }
    }

    void CheckPlayerInRange()
    {
        HidingSystem found = null;

        // วิธี 1: หาจาก Collider ในวงกลม (รองรับ Collider บน child)
        Collider2D[] hits = Physics2D.OverlapCircleAll(centerPoint.position, interactionRadius);
        foreach (Collider2D hit in hits)
        {
            HidingSystem hs = hit.GetComponentInParent<HidingSystem>();
            if (hs != null)
            {
                found = hs;
                break;
            }
        }

        // วิธี 2: ถ้าไม่เจอ หาจาก Tag + ระยะทาง (กรณี Player ไม่มี Collider หรือ Layer ชนกัน)
        if (found == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                float dist = Vector2.Distance(centerPoint.position, player.transform.position);
                if (dist <= interactionRadius)
                {
                    found = player.GetComponent<HidingSystem>();
                }
            }
        }

        if (found != null)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                playerHidingScript = found;
            }
        }
        else
        {
            if (playerInRange && (playerHidingScript == null || !playerHidingScript.isHiding))
            {
                playerInRange = false;
                playerHidingScript = null;
            }
        }
    }

    void ShowCannotHideMessage(string msg)
    {
        cannotHideMsg = msg;
        cannotHideMsgActive = true;
        cannotHideMsgTimer = 2.0f;
    }

    void OnGUI()
    {
        // แสดง error message หากซ่อนไม่ได้
        if (cannotHideMsgActive)
        {
            if (!styleReady)
            {
                promptStyle = new GUIStyle(GUI.skin.label);
                if (customFont != null) promptStyle.font = customFont; // เพิ่มบรรทัดนี้
                promptStyle.fontSize = 22;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = Color.red;
                promptStyle.fontStyle = FontStyle.Bold;
                styleReady = true;
            }
            float w = 600f;
            float h = 50f;
            Rect r = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.7f, w, h);
            // ใช้ cannotHideMsg ซึ่งจะ set เป็น cannotHideText เวลาโดนบล็อค
            GUI.Label(r, cannotHideMsg, promptStyle);
            return;
        }

        if (!playerInRange || playerHidingScript == null) return;
        if (!styleReady)
        {
            promptStyle = new GUIStyle(GUI.skin.label);
            if (customFont != null) promptStyle.font = customFont; // เพิ่มบรรทัดนี้
            promptStyle.fontSize = 22;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
            promptStyle.fontStyle = FontStyle.Bold;
            styleReady = true;
        }

        string text = playerHidingScript.isHiding ? "Press E To Exit Hiding" : promptText;
        float w2 = 400f;
        float h2 = 40f;
        Rect r2 = new Rect(Screen.width * 0.5f - w2 * 0.5f, Screen.height * 0.7f, w2, h2);
        GUI.Label(r2, text, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Transform c = centerPoint != null ? centerPoint : transform;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(c.position, interactionRadius);
    }
}
