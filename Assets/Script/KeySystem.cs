using UnityEngine;

public class KeySystem : MonoBehaviour
{
    public float interactDistance = 2f;
    public string keyID = "Key01"; // ระบุ Item Key ว่าเป็นกุญแจอะไร

    [Header("ข้อความที่แสดง (ใช้ {key} แทนชื่อกุญแจ)")]
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
        // เตรียม AudioSource เอาไว้เล่นเสียงเก็บกุญแจ
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
                    playerInv = player.GetComponent<Inventory>();

                displayMessage = pickupPrompt.Replace("{key}", keyID);

                if (Input.GetKeyDown(KeyCode.E) && playerInv != null)
                {
                    // ส่ง keyID ไปที่ฟังก์ชัน GetKey ของ Inventory
                    playerInv.GetKey(keyID);

                    // เล่นเสียงเมื่อเก็บกุญแจ
                    if (pickupSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
                    }

                    displayMessage = pickupSuccess.Replace("{key}", keyID);

                    // ให้ Destroy หลังเสียงจบ (หรือทันทีถ้าไม่มีเสียง)
                    float delay = (pickupSound != null) ? pickupSound.length : 0f;
                    Destroy(gameObject, delay);
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
        if (!string.IsNullOrEmpty(displayMessage))
        {
            int width = 400;
            int height = 40;
            int x = (Screen.width - width) / 2;
            int y = Screen.height - 100;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 32;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y, width, height), displayMessage, style);
        }
    }
}
