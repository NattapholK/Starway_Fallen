// File: UIBossHealthBar.cs
using UnityEngine;
using UnityEngine.UI;

public class UIBossHealthBar : MonoBehaviour
{
    [Header("Source")]
    public BossHealth boss;     // ลากบอสที่มี BossHealth มาวาง

    [Header("UI")]
    public Slider slider;       // ลาก Slider ตัวหลอด HP บอส
    public Gradient colorByHP;  // ไม่ใส่ก็ได้ (ไว้ทำสีตาม %HP)
    public Image fillImage;     // (ออปชัน) ถ้าจะเปลี่ยนสีแท่งใน Slider

    [Header("Behavior")]
    public bool smooth = true;
    public float lerpSpeed = 8f;
    public bool hideWhenDead = true;   // ซ่อนตอนตาย
    public bool hideWhenFull = false;  // ซ่อนถ้าเต็ม 100%

    float shown01 = 1f;

    void Reset()
    {
#if UNITY_2023_1_OR_NEWER
        boss = Object.FindFirstObjectByType<BossHealth>();
#else
        boss = Object.FindObjectOfType<BossHealth>();
#endif
        slider = GetComponent<Slider>();
        if (slider != null) { slider.minValue = 0f; slider.maxValue = 1f; }
    }

    void Start()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (slider != null) { slider.minValue = 0f; slider.maxValue = 1f; }
        if (!boss)
        {
#if UNITY_2023_1_OR_NEWER
            boss = Object.FindFirstObjectByType<BossHealth>();
#else
            boss = Object.FindObjectOfType<BossHealth>();
#endif
        }
    }

    void LateUpdate()
    {
        if (!boss || !slider) return;

        float target01 = boss.maxHP > 0 ? (boss.currentHP / (float)boss.maxHP) : 0f;
        target01 = Mathf.Clamp01(target01);

        shown01 = smooth ? Mathf.MoveTowards(shown01, target01, lerpSpeed * Time.deltaTime) : target01;

        slider.value = shown01;

        if (fillImage && colorByHP != null)
            fillImage.color = colorByHP.Evaluate(shown01);

        // ซ่อน/แสดงตามสถานะ
        if (hideWhenDead && boss.currentHP <= 0)
        {
            if (slider.gameObject.activeSelf) slider.gameObject.SetActive(false);
        }
        else if (hideWhenFull && Mathf.Approximately(target01, 1f))
        {
            if (slider.gameObject.activeSelf) slider.gameObject.SetActive(false);
        }
        else
        {
            if (!slider.gameObject.activeSelf) slider.gameObject.SetActive(true);
        }
    }
}

