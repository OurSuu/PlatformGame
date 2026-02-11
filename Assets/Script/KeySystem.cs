using UnityEngine;

public class KeySystem : MonoBehaviour
{
    public float interactDistance = 2f;
    public string keyID = "Key01"; // ระบุ Item Key ว่าเป็นกุญแจอะไร

    [Header("ข้อความที่แสดง (ใช้ {key} แทนชื่อกุญแจ)")]
    public string pickupPrompt = "กด E เพื่อเก็บกุญแจ [{key}]";
    public string pickupSuccess = "เก็บกุญแจแล้ว! [{key}]";

    private Inventory playerInv;
    private string displayMessage = "";

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
                    displayMessage = pickupSuccess.Replace("{key}", keyID);
                    Destroy(gameObject);
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
