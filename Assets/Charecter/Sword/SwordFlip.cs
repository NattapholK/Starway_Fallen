// File: SwordAimToMouse2D.cs
using UnityEngine;

public class SwordFlip : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("ตัว Transform ของดาบหรือ AimPivot ที่ต้องหมุนให้ชี้ไปทางเมาส์")]
    public Transform sword;

    [Tooltip("Main camera ของฉาก (ถ้าเว้นว่าง จะใช้ Camera.main)")]
    public Camera cam;

    [Header("Tuning")]
    [Tooltip("ปรับแก้มุมถ้าดาบวาดเอียง (องศาเพิ่มเข้าไปหลังคำนวณ)")]
    public float angleOffset = 0f;

    [Tooltip("เปิดถ้าต้องการให้สไปรต์ดาบพลิกแกน Y อัตโนมัติเมื่อชี้ซ้าย")]
    public bool flipSpriteWhenFacingLeft = true;

    [Tooltip("ใส่ SpriteRenderer ของดาบ (ถ้าอยากให้สคริปต์ flipY ให้อัตโนมัติ)")]
    public SpriteRenderer swordSprite;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!sword) sword = transform;
    }

    void Update()
    {
        if (!cam || !sword) return;

        // 1) แปลงตำแหน่งเมาส์จากจอ -> โลก
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = sword.position.z; // ล็อก z ให้เท่าดาบ (2D)

        // 2) เวกเตอร์จากดาบไปหาเมาส์
        Vector2 dir = (mouseWorld - sword.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        // 3) หมุนดาบให้ชี้ไปทางเมาส์
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        sword.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 4) แก้เคสดาบคว่ำเมื่อชี้ซ้าย: flip สไปรต์ตามมุม
        if (flipSpriteWhenFacingLeft && swordSprite)
        {
            // ถ้ามุมเกิน 90° หรือ น้อยกว่า -90° แปลว่า "กำลังชี้ซ้าย" -> พลิกแกน Y
            float a = Mathf.DeltaAngle(0f, angle);
            bool facingLeft = (a > 90f || a < -90f);
            swordSprite.flipY = facingLeft;
        }
    }
}
