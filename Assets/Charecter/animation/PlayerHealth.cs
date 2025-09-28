// File: PlayerHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable2D
{
    [Header("Health")]
    public int maxHP = 100;
    public int currentHP;

    [Header("Invulnerability (optional)")]
    public float iFrameTime = 0.3f;       // อมตะสั้น ๆ หลังโดนตี
    public bool  flashOnHit = true;
    public SpriteRenderer flashTarget;    // ถ้าอยากให้กะพริบตอนโดน

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    float iFrameTimer = 0f;

    void Awake()
    {
        // เริ่มด้วย MaxHP ถ้ายังไม่ได้ตั้ง
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0, maxHP);
        if (!flashTarget) flashTarget = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (iFrameTimer > 0f) iFrameTimer -= Time.deltaTime;
    }

    // ========== ดาเมจ ==========
    public void TakeDamage(int amount, Vector3 hitFrom)
    {
        if (iFrameTimer > 0f || currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - Mathf.Max(0, amount));
        onDamaged?.Invoke();

        if (flashOnHit && flashTarget) StartCoroutine(FlashCR());

        if (currentHP <= 0)
        {
            onDeath?.Invoke();
            // ตัวอย่าง: ปิดคอนโทรล/เล่นอนิเม/รีสตาร์ทฉาก
            // GetComponent<PlayerController2D>()?.enabled = false;
        }
        else
        {
            iFrameTimer = iFrameTime;
        }
    }

    // เผื่อมีที่เรียกแบบพารามิเตอร์เดียว
    public void TakeDamage(int amount) => TakeDamage(amount, transform.position);

    System.Collections.IEnumerator FlashCR()
    {
        if (!flashTarget) yield break;
        var c = flashTarget.color;
        flashTarget.color = new Color(c.r, c.g, c.b, 0.4f);
        yield return new WaitForSeconds(0.06f);
        flashTarget.color = c;
    }
}
