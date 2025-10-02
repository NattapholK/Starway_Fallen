// File: BossHealth.cs
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour, IDamageable2D, IDamageable
{
    [Header("Health")]
    public int maxHP = 300;
    public int currentHP;

    [Header("Hit Feedback")]
    [Tooltip("ช่วงอมตะหลังโดนตี (กันโดนหลายครั้งติดกัน)")]
    public float iFrameTime = 0.10f;
    public bool  flashOnHit = true;

    public enum FlashMode { Tint, Blink }
    public FlashMode flashMode = FlashMode.Tint;

    [Tooltip("ถ้าไม่ใส่ จะดึง SpriteRenderer ลูกๆ อัตโนมัติ (เฉพาะที่ active)")]
    public SpriteRenderer[] flashTargets;

    [Header("Tint Settings")]
    public Color  flashColor    = new Color(1f, 0.45f, 0.45f, 1f);
    public float  flashDuration = 0.15f;

    [Header("Blink Settings")]
    public int    blinkCount    = 2;
    public float  blinkInterval = 0.06f;

    [Header("SFX (optional)")]
    public AudioSource sfx;
    public AudioClip   sfxHurtArmor;
    public AudioClip   sfxDeath;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    // ---- internal ----
    float     _iFrameTimer;
    Coroutine _flashRoutine;
    Color[]   _originalColors;   // สำหรับคืนค่าสีตอนจบแฟลช
    bool[]    _originalEnabled;  // สำหรับคืนค่า enabled ตอนจบ Blink

    void Awake()
    {
        if (maxHP < 1) maxHP = 1;
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0, maxHP);

        // หา SpriteRenderer อัตโนมัติถ้าไม่ได้เซ็ต
        if (flashTargets == null || flashTargets.Length == 0)
            flashTargets = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);

        CacheOriginals();

        if (!sfx) sfx = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_iFrameTimer > 0f) _iFrameTimer -= Time.deltaTime;
    }

    // ===== ดาเมจแบบโปรเจกต์นี้ใช้ =====
    public void TakeDamage(int amount, Vector3 hitFrom) => ApplyDamage(amount);

    // ===== เผื่อระบบเก่า =====
    public void TakeDamage(int amount) => ApplyDamage(amount);

    void ApplyDamage(int amount)
    {
        if (currentHP <= 0) return;
        if (_iFrameTimer > 0f) return;

        int dmg = Mathf.Max(0, amount);
        if (dmg == 0) return;

        currentHP = Mathf.Max(0, currentHP - dmg);

        // SFX โดนตี
        if (sfx && sfxHurtArmor) sfx.PlayOneShot(sfxHurtArmor);

        onDamaged?.Invoke();

        // Flash feedback
        if (flashOnHit)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(flashMode == FlashMode.Tint ? FlashTintCR() : FlashBlinkCR());
        }

        if (currentHP <= 0)
        {
            if (sfx && sfxDeath) sfx.PlayOneShot(sfxDeath);
            onDeath?.Invoke();
            // TODO: ปิดคอมโพเนนต์/เล่นอนิเมชันตาย/Destroy ตามต้องการ
            // Destroy(gameObject, 0.1f);
        }
        else
        {
            _iFrameTimer = iFrameTime;
        }
    }

    // ---------- Flash: Tint ----------
    System.Collections.IEnumerator FlashTintCR()
    {
        if (flashTargets == null || flashTargets.Length == 0) yield break;

        // เปลี่ยนสี
        for (int i = 0; i < flashTargets.Length; i++)
            if (flashTargets[i] && flashTargets[i].enabled)
                flashTargets[i].color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        // คืนสีเดิม (กันกรณีโดนซ้อน—_originalColors มีเก็บตั้งแต่ต้น)
        for (int i = 0; i < flashTargets.Length; i++)
            if (flashTargets[i])
                flashTargets[i].color = _originalColors != null && i < _originalColors.Length
                    ? _originalColors[i]
                    : Color.white;
    }

    // ---------- Flash: Blink ----------
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

    void CacheOriginals()
    {
        if (flashTargets == null || flashTargets.Length == 0)
        {
            _originalColors  = null;
            _originalEnabled = null;
            return;
        }

        _originalColors  = new Color[flashTargets.Length];
        _originalEnabled = new bool[flashTargets.Length];

        for (int i = 0; i < flashTargets.Length; i++)
        {
            if (!flashTargets[i]) continue;
            _originalColors[i]  = flashTargets[i].color;
            _originalEnabled[i] = flashTargets[i].enabled;
        }
    }

    void RestoreOriginals()
    {
        if (flashTargets == null) return;

        for (int i = 0; i < flashTargets.Length; i++)
        {
            if (!flashTargets[i]) continue;

            if (_originalColors != null && i < _originalColors.Length)
                flashTargets[i].color = _originalColors[i];

            if (_originalEnabled != null && i < _originalEnabled.Length)
                flashTargets[i].enabled = _originalEnabled[i];
        }
    }

    void OnDisable()
    {
        // กันแฟลชค้างเมื่อปิดวัตถุ/เปลี่ยนฉาก
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        RestoreOriginals();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxHP < 1) maxHP = 1;
        if (currentHP > maxHP) currentHP = maxHP;
        if (iFrameTime < 0f) iFrameTime = 0f;
        if (flashDuration < 0f) flashDuration = 0f;
        if (blinkInterval < 0f) blinkInterval = 0f;
    }
#endif
}
