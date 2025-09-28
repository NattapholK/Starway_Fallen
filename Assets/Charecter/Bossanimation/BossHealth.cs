using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour, IDamageable2D, IDamageable
{
    [Header("Health")]
    public int maxHP = 300;
    public int currentHP;

    [Header("Hit Feedback")]
    public float iFrameTime = 0.1f;
    public bool flashOnHit = true;

    public enum FlashMode { Tint, Blink }
    public FlashMode flashMode = FlashMode.Tint;

    [Tooltip("ถ้าไม่ใส่ ผมจะหา SpriteRenderer ใต้ตัวนี้ให้เองอัตโนมัติ")]
    public SpriteRenderer[] flashTargets;

    [Header("Tint Settings")]
    public Color flashColor = new Color(1f, 0.45f, 0.45f, 1f); // แดงอ่อนเห็นชัด
    public float flashDuration = 0.15f;

    [Header("Blink Settings")]
    public int blinkCount = 2;
    public float blinkInterval = 0.06f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    float iFrameTimer = 0f;
    Coroutine flashRoutine;

    void Awake()
    {
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0, maxHP);

        // หา SpriteRenderer ให้เองถ้าไม่ได้เซ็ต
        if (flashTargets == null || flashTargets.Length == 0)
            flashTargets = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
    }

    void Update()
    {
        if (iFrameTimer > 0f) iFrameTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount, Vector3 hitFrom) => ApplyDamage(amount);
    public void TakeDamage(int amount)                  => ApplyDamage(amount);

    void ApplyDamage(int amount)
    {
        if (currentHP <= 0 || iFrameTimer > 0f) return;

        int dmg = Mathf.Max(0, amount);
        currentHP = Mathf.Max(0, currentHP - dmg);
        onDamaged?.Invoke();

        if (flashOnHit)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(flashMode == FlashMode.Tint ? FlashTintCR() : FlashBlinkCR());
        }

        if (currentHP <= 0)
        {
            onDeath?.Invoke();
            // Destroy(gameObject);
        }
        else
        {
            iFrameTimer = iFrameTime;
        }
    }

    System.Collections.IEnumerator FlashTintCR()
    {
        if (flashTargets == null || flashTargets.Length == 0) yield break;

        var originals = new Color[flashTargets.Length];
        for (int i = 0; i < flashTargets.Length; i++)
            if (flashTargets[i]) originals[i] = flashTargets[i].color;

        for (int i = 0; i < flashTargets.Length; i++)
            if (flashTargets[i] && flashTargets[i].enabled) flashTargets[i].color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < flashTargets.Length; i++)
            if (flashTargets[i]) flashTargets[i].color = originals[i];
    }

    System.Collections.IEnumerator FlashBlinkCR()
    {
        if (flashTargets == null || flashTargets.Length == 0) yield break;

        for (int n = 0; n < blinkCount; n++)
        {
            // ปิด
            for (int i = 0; i < flashTargets.Length; i++)
                if (flashTargets[i]) flashTargets[i].enabled = false;

            yield return new WaitForSeconds(blinkInterval);

            // เปิด
            for (int i = 0; i < flashTargets.Length; i++)
                if (flashTargets[i]) flashTargets[i].enabled = true;

            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
