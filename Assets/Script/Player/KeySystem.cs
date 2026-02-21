using UnityEngine;

public class KeySystem : MonoBehaviour
{
    public float interactDistance = 2f;
    public string keyID = "Key01"; // ต้องตั้งให้ตรงกับ keyID ใน Inventory

    [Header("ข้อความ & ฟอนต์")]
    public Font customFont; // 1. เพิ่มบรรทัดนี้

    public string pickupPrompt = "กด E เพื่อเก็บกุญแจ [{key}]";
    public string pickupSuccess = "เก็บกุญแจแล้ว! [{key}]";

    [Header("เสียงตอนเก็บกุญแจ")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupSoundVolume = 0.85f;

    private Inventory playerInv;
    private string displayMessage = "";
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance <= interactDistance)
            {
                if (playerInv == null)
                {
                    playerInv = player.GetComponent<Inventory>();
                }

                // เช็คว่าผู้เล่นยังไม่มีกุญแจนี้ใน inventory ค่อยโชว์ข้อความ
                if (playerInv != null && !playerInv.HasKey(keyID))
                {
                    displayMessage = pickupPrompt.Replace("{key}", keyID);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        playerInv.GetKey(keyID);

                        // เล่นเสียง
                        if (pickupSound != null && audioSource != null)
                        {
                            audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
                        }

                        displayMessage = pickupSuccess.Replace("{key}", keyID);

                        // ทำลาย object (หลังเสียงจบถ้ามีเสียง)
                        float delay = pickupSound ? pickupSound.length : 0f;
                        Destroy(gameObject, delay);
                    }
                }
                else
                {
                    // ถ้าเก็บไปแล้ว ไม่ต้องแสดงข้อความซ้ำ
                    displayMessage = "";
                }
            }
            else
            {
                displayMessage = "";
            }
        }
        else
        {
            displayMessage = "";
        }
    }

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(displayMessage) && Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            float adjustedY = Screen.height - screenPos.y;

            GUIStyle style = new GUIStyle(GUI.skin.label);

            // 2. เพิ่มบรรทัดนี้
            if (customFont != null) style.font = customFont;

            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(screenPos.x - 200, adjustedY - 60, 400, 50), displayMessage, style);
        }
    }
}
