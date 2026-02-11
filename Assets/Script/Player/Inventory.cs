using UnityEngine;
using System.Collections.Generic;
using TMPro; // หากใช้ TextMeshPro

public class Inventory : MonoBehaviour
{
    // เก็บ key IDs ที่ผู้เล่นถืออยู่
    private HashSet<string> keyIDs = new HashSet<string>();

    public bool hasKey => keyIDs.Count > 0; // ยังคงรองรับแบบ bool
    public GameObject keyUI; // ลากรูปกุญแจใน UI มาใส่ตรงนี้

    void Start()
    {
        if (keyUI != null) keyUI.SetActive(false); // เริ่มเกมให้ซ่อนรูปกุญแจไว้ก่อน
    }

    // รับกุญแจโดยระบุ keyId (รองรับหลายดอก)
    public void GetKey(string keyId)
    {
        keyIDs.Add(keyId);
        if (keyUI != null) keyUI.SetActive(true); // โชว์รูปกุญแจที่ด้านล่างจอ
        Debug.Log($"เก็บกุญแจแล้ว! [{keyId}]");
    }

    // fallback สำหรับระบบเก่า (KeySystem เดิม)
    public void GetKey()
    {
        GetKey("Key01"); // กำหนดค่าเริ่มต้น/เพื่อรองรับระบบเดิม
    }

    // ใช้กุญแจโดยระบุ keyId
    public void UseKeyID(string keyId)
    {
        if (keyIDs.Contains(keyId))
        {
            keyIDs.Remove(keyId);
            Debug.Log($"ใช้กุญแจ {keyId} แล้ว!");
        }
        else
        {
            Debug.LogWarning($"ไม่มี {keyId} ใน Inventory!");
        }
        // ซ่อน UI ถ้าไม่มีกุญแจเหลือ
        if (keyUI != null && keyIDs.Count == 0) keyUI.SetActive(false);
    }

    // fallback สำหรับระบบเก่า (ใช้กุญแจเดียว)
    public void UseKey()
    {
        UseKeyID("Key01");
    }

    // ตรวจสอบว่าถือกุญแจ id นี้หรือไม่
    public bool HasKeyID(string keyId)
    {
        return keyIDs.Contains(keyId);
    }
}