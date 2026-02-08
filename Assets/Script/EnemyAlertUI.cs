using UnityEngine;

/// <summary>
/// แสดงข้อความ "หาที่แอบ!" เมื่อ Enemy มองเห็น Player
/// </summary>
public class EnemyAlertUI : MonoBehaviour
{
    public static bool IsPlayerSpotted { get; set; }

    private GUIStyle style;
    private bool styleReady;

    void OnGUI()
    {
        if (!IsPlayerSpotted) return;

        if (!styleReady)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 28;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.red;
            style.fontStyle = FontStyle.Bold;
            styleReady = true;
        }

        Rect r = new Rect(0, Screen.height * 0.15f, Screen.width, 50);
        GUI.Label(r, "⚠ หาที่แอบ!", style);
    }
}
