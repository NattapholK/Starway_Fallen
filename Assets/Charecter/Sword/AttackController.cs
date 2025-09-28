// File: AttackController.cs  (FINAL + FALLBACK) — 2D melee for Boss/Enemies
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AttackController : MonoBehaviour
{
    [Header("Animator (this sword object)")]
    public Animator animator;
    public string normalAttackTrigger = "Attack";
    public string sweepAttackTrigger  = "SweepAttack";
    public string chargingBool        = "IsCharging";

    [Header("Cooldown")]
    public float normalAttackCooldown = 0.5f;
    public float sweepAttackCooldown  = 1.0f;
    float _cooldownTimer;

    [Header("Hit detection (2D)")]
    [Tooltip("ถ้ามี ให้วาง Empty ชื่อ HitOrigin ที่ปลายดาบและลากมาใส่")]
    public Transform hitOrigin;                 // optional
    [Min(0f)] public float hitRadius = 0.9f;
    public LayerMask hittableLayers;            // ติ๊กเฉพาะ Enemy/Boss
    [Tooltip("ตั้ง = transform.root เพื่อกันโดนตัวเอง/ลูก ๆ")]
    public Transform ownerRoot;

    [Header("Damage")]
    public int normalDamage = 12;

    [Header("Sweep Damage (per hit or auto)")]
    public int sweep1Damage   = 8;   // ใช้เมื่อมี Animation Event
    public int sweep2Damage   = 12;
    public int sweepFinisher  = 18;

    [Header("Facing / Filter")]
    public bool frontOnly = true;
    [Range(0f, 180f)] public float frontalArc = 120f;
    [Tooltip("ใช้ .right เป็นทิศหน้า")]
    public Transform directionRef;
    [Tooltip("ดันวงตรวจจาก origin ไปข้างหน้า (ถ้าใช้ HitOrigin อยู่ปลายดาบ แนะนำ 0)")]
    public float originForwardOffset = 0.0f;

    [Header("Fallbacks (no AnimEvent / no HitOrigin)")]
    [Tooltip("ถ้าไม่ได้วาง Animation Event สำหรับ Attack ปกติ ให้ยิงดาเมจทันทีเมื่อกดทริกเกอร์")]
    public bool autoHitOnNormalTrigger = true;
    [Tooltip("ถ้าไม่ได้วาง Animation Event สำหรับ Sweep ให้ยิงดาเมจทันทีเมื่อกดทริกเกอร์")]
    public bool autoHitOnSweepTrigger  = true;
    [Tooltip("ถ้าไม่มี HitOrigin จะคำนวณ origin = ตำแหน่งดาบ + .right * ค่านี้")]
    public float autoForwardOffsetIfNoOrigin = 0.5f;

    AudioSource _audio;

    void Reset()
    {
        animator     = GetComponent<Animator>();
        directionRef = transform;
        ownerRoot    = transform.root;
    }

    void Awake()
    {
        if (!animator)     animator     = GetComponent<Animator>();
        if (!directionRef) directionRef = transform;
        if (!ownerRoot)    ownerRoot    = transform.root;
        _audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0)) TryNormalAttack();
        if (Input.GetMouseButtonDown(1)) StartCharge();
        if (Input.GetMouseButtonUp(1))   ReleaseCharge();
    }

    // ---- Input flow ----
    void TryNormalAttack()
    {
        if (_cooldownTimer > 0f) return;
        _cooldownTimer = normalAttackCooldown;

        if (animator) animator.SetTrigger(normalAttackTrigger);
        PlaySwing();

        // Fallback: ถ้าไม่มี Animation Event สำหรับ Normal
        if (autoHitOnNormalTrigger)
            DoHit(normalDamage);
    }

    void StartCharge()
    {
        if (_cooldownTimer > 0f) return;
        if (!string.IsNullOrEmpty(chargingBool))
            animator.SetBool(chargingBool, true);
    }

    void ReleaseCharge()
    {
        if (!string.IsNullOrEmpty(chargingBool))
            animator.SetBool(chargingBool, false);

        if (_cooldownTimer > 0f) return;
        _cooldownTimer = sweepAttackCooldown;

        if (animator) animator.SetTrigger(sweepAttackTrigger);
        PlaySwing();

        // Fallback: ถ้าไม่มี Animation Event สำหรับ Sweep
        if (autoHitOnSweepTrigger)
            DoHit(sweepFinisher);  // หรือจะใช้ค่ากลางก็ได้ เช่น (sweep2Damage)
    }

    void PlaySwing()
    {
        if (!_audio) return;
        // _audio.PlayOneShot(someClip); // ใส่คลิปเสียงถ้ามี
    }

    // ---- Core hit ----
    void DoHit(int dmg)
    {
        // 1) หาทิศหน้า
        Vector2 forward = directionRef ? (Vector2)directionRef.right : Vector2.right;

        // 2) หา origin
        Vector2 basePos = hitOrigin ? (Vector2)hitOrigin.position
                                    : (Vector2)transform.position + forward * autoForwardOffsetIfNoOrigin;
        Vector2 origin = basePos + forward * originForwardOffset;

        // 3) Overlap
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitRadius, hittableLayers);
        float halfArc = frontalArc * 0.5f;

        foreach (var col in hits)
        {
            // กันตัวเอง
            if (ownerRoot && col.transform.root == ownerRoot) continue;

            // กรองมุมด้านหน้า
            if (frontOnly)
            {
                Vector2 closest  = col.ClosestPoint(origin); // 2D API → Vector2
                Vector2 toTarget = closest - origin;
                if (toTarget.sqrMagnitude < 1e-6f) continue;
                if (Vector2.Angle(forward, toTarget) > halfArc) continue;
            }

            // ยิงดาเมจ
            var d2 = col.GetComponentInParent<IDamageable2D>();
            if (d2 != null) { d2.TakeDamage(dmg, transform.position); continue; }

            var d1 = col.GetComponentInParent<IDamageable>();
            if (d1 != null) d1.TakeDamage(dmg);
        }
    }

    // ---- Animation Events (ถ้ามี) ----
    public void AE_Hit_Normal()        => DoHit(normalDamage);
    public void AE_Hit_Sweep1()        => DoHit(sweep1Damage);
    public void AE_Hit_Sweep2()        => DoHit(sweep2Damage);
    public void AE_Hit_SweepFinisher() => DoHit(sweepFinisher);
    public void AE_Hit_Custom(int dm)  => DoHit(dm);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!directionRef) directionRef = transform;
        Vector2 f = directionRef.right;

        Vector2 basePos = hitOrigin ? (Vector2)hitOrigin.position
                                    : (Vector2)transform.position + f * autoForwardOffsetIfNoOrigin;
        Vector2 o = basePos + f * originForwardOffset;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.55f);
        Gizmos.DrawWireSphere(o, hitRadius);

        if (frontOnly)
        {
            float a = frontalArc * 0.5f;
            Vector2 r = Quaternion.Euler(0,0, +a) * f;
            Vector2 l = Quaternion.Euler(0,0, -a) * f;
            Gizmos.color = new Color(1f, 1f, 0.1f, 0.9f);
            Gizmos.DrawLine(o, o + f * hitRadius);
            Gizmos.DrawLine(o, o + r * hitRadius);
            Gizmos.DrawLine(o, o + l * hitRadius);
        }
    }
#endif
}

// เผื่อระบบเก่าในโปรเจกต์
public interface IDamageable { void TakeDamage(int amount); }
