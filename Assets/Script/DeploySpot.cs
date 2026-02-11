using UnityEngine;

public class DeploySpot : MonoBehaviour
{
    public GameObject door;
    public string requiredKeyID = "Key01";
    public float interactDistance = 2f;

    [Header("Customizable Messages")]
    [Tooltip("ข้อความเมื่อมีคีย์และสามารถเปิดประตูได้")]
    public string openDoorMessage = "กด E เพื่อเปิดประตู";
    [Tooltip("ข้อความเมื่อไม่มีกุญแจ")]
    public string noKeyMessage = "ท่านไม่มีกุญแจ";

    private Inventory playerInv;
    private GameObject playerObj;
    private string displayMessage = "";
    private bool used = false;

    void Update()
    {
        // หากประตูถูกเปิดไปแล้ว ไม่ต้องทำอะไรอีก
        if (used)
        {
            displayMessage = "";
            return;
        }

        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            float distance = Vector2.Distance(transform.position, playerObj.transform.position);

            if (distance <= interactDistance)
            {
                if (playerInv == null)
                {
                    playerInv = playerObj.GetComponent<Inventory>();
                }

                // Check if player has keyID (if HasKeyID method exists, use that; fallback to hasKey)
                bool hasCorrectKey = false;
                if (playerInv != null)
                {
                    var hasKeyIdMethod = playerInv.GetType().GetMethod("HasKeyID");
                    if (hasKeyIdMethod != null)
                    {
                        hasCorrectKey = (bool)hasKeyIdMethod.Invoke(playerInv, new object[] { requiredKeyID });
                    }
                    else
                    {
                        hasCorrectKey = playerInv.hasKey;
                    }
                }

                if (hasCorrectKey)
                {
                    displayMessage = openDoorMessage;
                    // Wait for press E to open the door
                    if (Input.GetKeyDown(KeyCode.E) && door != null)
                    {
                        Destroy(door);
                        displayMessage = ""; // Clear because door opened
                        used = true;
                    }
                }
                else
                {
                    displayMessage = noKeyMessage;
                }
            }
            else
            {
                displayMessage = "";
                playerInv = null;
            }
        }
        else
        {
            displayMessage = "";
            playerInv = null;
        }
    }

    void OnGUI()
    {
        // ถ้าใช้ DeploySpot แล้ว จะไม่แสดงข้อความอีก
        if (!string.IsNullOrEmpty(displayMessage) && !used)
        {
            int width = 400;
            int height = 40;
            int x = (Screen.width - width) / 2;
            int y = Screen.height - 100;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 32;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x, y, width, height), displayMessage, style);
        }
    }
}