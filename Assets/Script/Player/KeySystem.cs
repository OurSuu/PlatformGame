using UnityEngine;

public class KeySystem : MonoBehaviour
{
    public float interactDistance = 2f;
    [Tooltip("ตั้งชื่อ keyID ให้เหมือนกับใน InventoryInspector และใน KeySlot เช่น \"Key01\" \"BlueKey\" ")]
    public string keyID = "Key01"; // ต้องตั้งให้ตรงกับ keyID ใน Inventory

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
                        // ฟังก์ชันใหม่: เก็บกุญแจตาม keyID แบบใหม่
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
