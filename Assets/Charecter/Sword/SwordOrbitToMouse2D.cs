// File: SwordOrbitToMouse2D.cs
using UnityEngine;

/// ดาบจะโคจรรอบผู้เล่นตามมุมเมาส์ + หมุนปลายดาบชี้ไปทางเมาส์
/// ใช้ได้ทั้งกรณีดาบเป็นอ็อบเจ็กต์อิสระ หรือเป็นลูกของผู้เล่นก็ได้
public class SwordOrbitToMouse2D : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("ตัวผู้เล่น (จุดศูนย์กลางที่ดาบจะโคจรรอบ)")]
    public Transform player;
    [Tooltip("ตัว Transform ของดาบ (ปกติ = ตัวนี้)")]
    public Transform sword;
    [Tooltip("กล้องหลักของฉาก (ถ้าเว้นว่างจะใช้ Camera.main)")]
    public Camera cam;

    [Header("Orbit")]
    [Tooltip("ระยะจากศูนย์กลางผู้เล่น -> ดาบ")]
    public float orbitRadius = 0.7f;
    [Tooltip("ปรับตำแหน่งเพิ่ม/ลดจากรัศมี (เช่น ยกดาบขึ้นเล็กน้อย)")]
    public Vector2 extraOffset = Vector2.zero;
    [Tooltip("ทำให้การโคจรลื่นไหล (0 = หยาบ, 1 = หนืดมาก)")]
    [Range(0f, 1f)] public float orbitSmoothing = 0.0f;

    [Header("Rotation")]
    [Tooltip("ชดเชยมุม ถ้าสปริตดาบวาดเอียง")]
    public float angleOffset = 0f;
    [Tooltip("อยากให้สปริตดาบพลิกแกน Y อัตโนมัติเมื่อชี้ซ้าย")]
    public bool  flipSpriteWhenFacingLeft = true;
    public SpriteRenderer swordSprite;

    // ภายใน
    Vector3 _vel; // ใช้กับ SmoothDamp (แบบเวกเตอร์)

    void Reset()
    {
        sword = transform;
    }

    void Awake()
    {
        if (!sword) sword = transform;
        if (!cam)   cam   = Camera.main;
        if (!player) player = sword.root; // เดาจาก root ถ้าไม่ได้ใส่
    }

    void LateUpdate()
    {
        if (!cam || !player || !sword) return;

        // 1) ตำแหน่งเมาส์ในโลก (z เท่าผู้เล่น/ดาบ)
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = player.position.z;

        // 2) เวกเตอร์จากผู้เล่น -> เมาส์
        Vector2 dir = (Vector2)(mouseWorld - player.position);
        if (dir.sqrMagnitude < 0.000001f) return;
        dir.Normalize();

        // 3) ตำแหน่งเป้าหมายของดาบ = ศูนย์กลาง + รัศมี + ออฟเซ็ต
        Vector3 targetPos = (Vector2)player.position + dir * orbitRadius + extraOffset;

        // 4) เคลื่อนดาบไปตำแหน่งเป้าหมาย
        if (orbitSmoothing > 0f)
        {
            // ใช้ SmoothDamp ให้ลื่น (ระยะเวลา ~ ค่าหนืดเล็กน้อย)
            float smoothTime = Mathf.Lerp(0.0f, 0.08f, orbitSmoothing);
            sword.position = Vector3.SmoothDamp(sword.position, targetPos, ref _vel, smoothTime);
        }
        else
        {
            sword.position = targetPos;
        }

        // 5) หมุนดาบให้ปลายชี้ไปทิศเมาส์
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffset;
        sword.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 6) แก้เคสดาบคว่ำเมื่อชี้ซ้าย: flip Y ของ SpriteRenderer
        if (flipSpriteWhenFacingLeft && swordSprite)
        {
            float a = Mathf.DeltaAngle(0f, angle);
            bool facingLeft = (a > 90f || a < -90f);
            swordSprite.flipY = facingLeft;
        }
    }
}
