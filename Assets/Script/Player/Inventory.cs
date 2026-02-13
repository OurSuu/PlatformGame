using UnityEngine;
using UnityEngine.UI; // เพิ่มเพื่อใช้กับ Image UI
using System.Collections.Generic;
using TMPro; // หากใช้ TextMeshPro

public class Inventory : MonoBehaviour
{
    // ใช้ HashSet เก็บชื่อกุญแจเหมือนเดิม (ป้องกันซ้ำ)
    private HashSet<string> keyIDs = new HashSet<string>();

    [System.Serializable]
    public struct KeyData
    {
        public string keyID;     // ชื่อกุญแจ (เช่น "Key01" "BlueKey")
        public Sprite keySprite; // รูป Sprite ของกุญแจนี้
    }

    [Header("1. ข้อมูลกุญแจ (จับคู่ชื่อกับ sprite)")]
    public KeyData[] allKeyData;

    [Header("2. ช่อง UI บนหน้าจอ (ลากช่อง Image ว่างๆ มาใส่ 5 ช่อง)")]
    public Image[] uiSlots;

    void Start()
    {
        // ตอนเริ่มเกม: สั่งปิด GameObject ของช่อง UI ทั้งหมด
        foreach (Image slot in uiSlots)
        {
            // เปลี่ยนจาก slot.enabled = false; เป็นบรรทัดนี้แทน
            slot.gameObject.SetActive(false); 
            slot.sprite = null;
        }
    }

    // ฟังก์ชันเก็บกุญแจ
    public void GetKey(string keyId)
    {
        if (!keyIDs.Contains(keyId))
        {
            keyIDs.Add(keyId);
            Debug.Log($"เก็บกุญแจ: {keyId}");

            // ค้นหารูปของกุญแจนี้
            Sprite icon = GetSpriteByID(keyId);

            if (icon != null)
            {
                AddToNextSlot(icon);
            }
        }
    }

    // fallback สำหรับระบบเก่า (KeySystem เดิม)
    public void GetKey()
    {
        GetKey("Key01");
    }

    // ใช้กุญแจโดยระบุ keyId: จะลบ icon ออกจาก uiSlot ด้วย
    public void UseKeyID(string keyId)
    {
        if (keyIDs.Contains(keyId))
        {
            keyIDs.Remove(keyId);
            Debug.Log($"ใช้กุญแจ {keyId} แล้ว!");
            RemoveFromSlot(keyId);
        }
        else
        {
            Debug.LogWarning($"ไม่มี {keyId} ใน Inventory!");
        }
    }

    // fallback สำหรับระบบเก่า (ใช้กุญแจเดียว)
    public void UseKey()
    {
        UseKeyID("Key01");
    }

    // ฟังก์ชันเช็คว่ามีกุญแจชื่อนี้ไหม
    public bool HasKey(string keyId)
    {
        return keyIDs.Contains(keyId);
    }

    // fallback สำหรับระบบเก่า (method เดิม)
    public bool HasKeyID(string keyId)
    {
        return HasKey(keyId);
    }

    // เอารูป sprite ไปใส่ในช่องว่าง (ช่องแรกที่เจอที่ GameObject ยังไม่เปิด)
    void AddToNextSlot(Sprite icon)
    {
        foreach (Image slot in uiSlots)
        {
            // เปลี่ยนเงื่อนไขเช็คว่า GameObject ปิดอยู่หรือไม่
            if (!slot.gameObject.activeSelf) 
            {
                slot.sprite = icon;
                slot.preserveAspect = true;
                // สั่งเปิด GameObject ให้เห็นทันที
                slot.gameObject.SetActive(true); 
                return;
            }
        }
    }

    // ฟังก์ชันช่วย: ได้ keyId แล้วหา sprite ที่ตรงชื่อ
    Sprite GetSpriteByID(string id)
    {
        foreach (KeyData data in allKeyData)
        {
            if (data.keyID == id)
                return data.keySprite;
        }
        return null;
    }

    // เมื่อลบกุญแจ: ลบออกจาก uiSlot (slot แรกที่เจอที่มีรูปนี้)
    void RemoveFromSlot(string keyId)
    {
        Sprite wantSprite = GetSpriteByID(keyId);
        if (wantSprite == null) return;

        foreach (Image slot in uiSlots)
        {
            if (slot.enabled && slot.sprite == wantSprite)
            {
                slot.enabled = false;
                slot.sprite = null;
                return;
            }
        }
    }

    // ฟังก์ชันใหม่: RemoveKey จะค้นหา slot ที่เป็นรูปของ keyId และสั่งปิด GameObject ของมัน
    public void RemoveKey(string keyId)
    {
        Sprite wantSprite = GetSpriteByID(keyId);
        if (wantSprite == null) return;

        foreach (Image slot in uiSlots)
        {
            if (slot.gameObject.activeSelf && slot.sprite == wantSprite)
            {
                slot.sprite = null;
                slot.gameObject.SetActive(false);
                return;
            }
        }
    }

    // คุณสมบัติ bool เดิมว่ามีกุญแจไหม (สำหรับระบบเก่า)
    public bool hasKey => keyIDs.Count > 0;
}