using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("จุดกลาง (ว่างไว้ใช้ตำแหน่ง Object นี้)")]
    public Transform centerPoint;

    [Header("ระยะกด E ได้ (ใหญ่ = กดจากไกลได้)")]
    public float interactionRadius = 3f;

    [Header("ข้อความที่แสดง")]
    public string promptText = "กด E เพื่อซ่อนตัว";

    private bool playerInRange = false;
    private HidingSystem playerHidingScript;
    private GUIStyle promptStyle;
    private bool styleReady;

    void Awake()
    {
        if (centerPoint == null)
            centerPoint = transform;
    }

    void Update()
    {
        CheckPlayerInRange();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (playerHidingScript != null)
            {
                if (!playerHidingScript.isHiding)
                    playerHidingScript.EnterHidingSpot(centerPoint.position);
                else
                    playerHidingScript.ExitHidingSpot();
            }
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

    void OnGUI()
    {
        if (!playerInRange || playerHidingScript == null) return;
        // แสดงเฉพาะเมื่อยังไม่ซ่อน หรือซ่อนอยู่แล้ว (บอกกด E เพื่อออก)
        if (!styleReady)
        {
            promptStyle = new GUIStyle(GUI.skin.label);
            promptStyle.fontSize = 22;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
            promptStyle.fontStyle = FontStyle.Bold;
            styleReady = true;
        }

        string text = playerHidingScript.isHiding ? "กด E เพื่อออกจากจุดซ่อน" : promptText;
        float w = 400f;
        float h = 40f;
        Rect r = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.7f, w, h);
        GUI.Label(r, text, promptStyle);
    }

    void OnDrawGizmosSelected()
    {
        Transform c = centerPoint != null ? centerPoint : transform;
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(c.position, interactionRadius);
    }
}
