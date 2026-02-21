using UnityEngine;

public class DeploySpot : MonoBehaviour
{
    public GameObject door;

    [Header("ใส่รายชื่อกุญแจที่ต้องใช้ที่นี่ (เช่น 5 ดอก)")]
    public string[] requiredKeys; // เปลี่ยนจาก string เดียวเป็น Array
    public float interactDistance = 3f;

    [Header("ข้อความ & ฟอนต์")]
    public Font customFont; // 1. เพิ่มบรรทัดนี้
    public string openMessage = "กด E เพื่อไขประตู";
    public string lockedMessage = "กุญแจยังไม่ครบ!";

    [Header("เสียง")]
    public AudioClip openSound;
    [Range(0f, 1f)] public float openSoundVolume = 0.8f;

    private bool doorOpened = false;
    private bool playerInRange = false;
    private Inventory playerInv;

    void Update()
    {
        if (doorOpened) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);

            if (dist <= interactDistance)
            {
                playerInRange = true;
                playerInv = player.GetComponent<Inventory>();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (CheckAllKeys())
                    {
                        OpenDoor();
                    }
                }
            }
            else
            {
                playerInRange = false;
            }
        }
        else
        {
            playerInRange = false;
            playerInv = null;
        }
    }

    // ฟังก์ชันเช็คว่ามีครบทุกดอกไหม
    bool CheckAllKeys()
    {
        if (playerInv == null) return false;

        foreach (string key in requiredKeys)
        {
            if (!playerInv.HasKey(key))
            {
                return false; // ถ้าขาดแม้แต่ดอกเดียว คืนค่า false ทันที
            }
        }
        return true; // ถ้าวนครบแล้วไม่ขาดเลย คืนค่า true
    }

    void OpenDoor()
    {
        doorOpened = true;
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position, openSoundVolume);
        }

        // ลบกุญแจทุกดอกจาก Inventory เมื่อเปิดประตู
        if (playerInv != null && requiredKeys != null)
        {
            foreach (string key in requiredKeys)
            {
                playerInv.UseKeyID(key);
            }
        }

        if (door != null)
        {
            Destroy(door);
        }
    }

    void OnGUI()
    {
        if (playerInRange && !doorOpened && playerInv != null)
        {
            // สเกลอิงตามความสูงหน้าจอ (รองรับทุกจอ)
            float baseHeight = 1080f;
            float scale = Mathf.Clamp(Screen.height / baseHeight, 0.5f, 2.5f);

            GUIStyle style = new GUIStyle(GUI.skin.label);

            // 2. เพิ่มบรรทัดนี้
            if (customFont != null) style.font = customFont;

            style.fontSize = Mathf.RoundToInt(36 * scale); // ใหญ่ขึ้นสำหรับ 4K, ปรับได้ตามต้องการ
            style.fontStyle = FontStyle.Bold;

            float panelWidth = 600f * scale;
            float headerHeight = 64f * scale;
            float itemHeight = 48f * scale;

            float totalListHeight = itemHeight * requiredKeys.Length;
            float startX = Screen.width / 2f - panelWidth / 2f;
            float startY = Screen.height / 2f - (headerHeight + totalListHeight) / 2f;

            // แสดงหัวข้อ
            bool isReady = CheckAllKeys();
            string header = isReady ? openMessage : lockedMessage;
            style.normal.textColor = isReady ? Color.green : Color.red;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(startX, startY, panelWidth, headerHeight), header, style);

            // แสดงรายการกุญแจ
            style.alignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < requiredKeys.Length; i++)
            {
                string keyName = requiredKeys[i];
                bool hasIt = playerInv.HasKey(keyName);
                string status = hasIt ? "[ / ] Already Have It" : "[ X ] Not Have";
                style.normal.textColor = hasIt ? Color.green : Color.gray;
                GUI.Label(
                    new Rect(startX + 32f * scale, startY + headerHeight + i * itemHeight, panelWidth - 64f * scale, itemHeight),
                    $"{keyName} : {status}", style
                );
            }
        }
    }
}