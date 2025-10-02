/// File: AttackController.cs  (FINAL + SFX + Simple Bullet Destroy)
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
    public LayerMask hittableLayers;            // ต้องติ๊กเลเยอร์ Enemy/Boss/ Bullet
    [Tooltip("ตั้ง = transform.root เพื่อกันโดนตัวเอง/ลูก ๆ")]
    public Transform ownerRoot;

    [Header("Damage")]
    public int normalDamage = 12;

    [Header("Sweep Damage (per hit or auto)")]
    public int sweep1Damage   = 8;
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
    public bool autoHitOnNormalTrigger = true;
    public bool autoHitOnSweepTrigger  = true;
    [Tooltip("ถ้าไม่มี HitOrigin จะคำนวณ origin = ตำแหน่งดาบ + .right * ค่านี้")]
    public float autoForwardOffsetIfNoOrigin = 0.5f;

    // ============== SFX ==============
    [Header("SFX")]
    public bool playWhooshOnTrigger = false;
    public AudioSource audioSource;
    public AudioClip sfxWhoosh;
    public AudioClip sfxHitArmor;   // ใช้เป็นเสียงฟันโดนกระสุนได้ด้วย
    public AudioClip sfxHitFlesh;
    [Range(0f,1f)] public float sfxVolume = 1f;

    [Header("SFX Variation")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    public int maxHitSoundsPerSwing = 2;

    // ============== Bullet Parry (simple) ==============
    [Header("Bullet Parry (simple)")]
    [Tooltip("เปิดไว้เพื่อให้ดาบลบกระสุนทันที (ไม่สะท้อน)")]
    public bool destroyBulletsOnHit = true;
    [Tooltip("Tag ของกระสุน (ให้ตั้งใน prefab กระสุน)")]
    public string bulletTag = "Bullet";

    void Reset()
    {
        animator     = GetComponent<Animator>();
        directionRef = transform;
        ownerRoot    = transform.root;
        audioSource  = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!animator)     animator     = GetComponent<Animator>();
        if (!directionRef) directionRef = transform;
        if (!ownerRoot)    ownerRoot    = transform.root;
        if (!audioSource)  audioSource  = GetComponent<AudioSource>();
        if (!audioSource)  audioSource  = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
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
        if (playWhooshOnTrigger) PlayOneShotVar(sfxWhoosh);

        if (autoHitOnNormalTrigger) DoHit(normalDamage);
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
        if (playWhooshOnTrigger) PlayOneShotVar(sfxWhoosh);

        if (autoHitOnSweepTrigger) DoHit(sweepFinisher);
    }

    // ---- Core hit ----
    void DoHit(int dmg)
    {
        Vector2 forward = directionRef ? (Vector2)directionRef.right : Vector2.right;

        Vector2 basePos = hitOrigin ? (Vector2)hitOrigin.position
                                    : (Vector2)transform.position + forward * autoForwardOffsetIfNoOrigin;
        Vector2 origin = basePos + forward * originForwardOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitRadius, hittableLayers);
        float halfArc = frontalArc * 0.5f;

        bool hitAny = false;
        int  hitSoundCount = 0;

        foreach (var col in hits)
        {
            // กันตัวเอง
            if (ownerRoot && col.transform.root == ownerRoot) continue;

            // กรองมุมด้านหน้า
            if (frontOnly)
            {
                Vector2 closest  = col.ClosestPoint(origin);
                Vector2 toTarget = closest - origin;
                if (toTarget.sqrMagnitude < 1e-6f) continue;
                if (Vector2.Angle(forward, toTarget) > halfArc) continue;
            }

            // ---- ลบกระสุนทันที (ไม่สะท้อน) ----
            if (destroyBulletsOnHit)
            {
                if (!string.IsNullOrEmpty(bulletTag) && col.CompareTag(bulletTag))
                {
                    var go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.gameObject;
                    Destroy(go);
                    PlayHitSound(ref hitSoundCount, isBoss:false);
                    hitAny = true;
                    continue; // ไม่ยิงดาเมจอย่างอื่นต่อ
                }
                // เผื่อไม่ได้ตั้ง Tag ไว้ แต่มีสคริปต์ BossBullet
                var bb = col.GetComponentInParent<BossBullet>();
                if (bb)
                {
                    Destroy(bb.gameObject);
                    PlayHitSound(ref hitSoundCount, isBoss:false);
                    hitAny = true;
                    continue;
                }
            }

            // ---- ยิงดาเมจใส่ศัตรู/บอส ----
            var d2 = col.GetComponentInParent<IDamageable2D>();
            var d1 = (d2 == null) ? col.GetComponentInParent<IDamageable>() : null;

            if (d2 != null) d2.TakeDamage(dmg, transform.position);
            else if (d1 != null) d1.TakeDamage(dmg);
            else continue;

            hitAny = true;
            bool isBoss = col.GetComponentInParent<BossHealth>() != null;
            PlayHitSound(ref hitSoundCount, isBoss);
        }

        // ถ้าฟันลม และไม่ได้เล่น whoosh ตอนกด → เล่น whoosh ตอนนี้
        if (!hitAny && !playWhooshOnTrigger) PlayOneShotVar(sfxWhoosh);
    }

    void PlayHitSound(ref int count, bool isBoss)
    {
        if (count >= maxHitSoundsPerSwing) return;
        PlayOneShotVar(isBoss ? sfxHitArmor : sfxHitFlesh);
        count++;
    }

    // ---- Animation Events ----
    public void AE_Hit_Normal()        => DoHit(normalDamage);
    public void AE_Hit_Sweep1()        => DoHit(sweep1Damage);
    public void AE_Hit_Sweep2()        => DoHit(sweep2Damage);
    public void AE_Hit_SweepFinisher() => DoHit(sweepFinisher);
    public void AE_Hit_Custom(int dm)  => DoHit(dm);

    // ---- SFX helper ----
    void PlayOneShotVar(AudioClip clip)
    {
        if (!clip || !audioSource) return;
        float p = Random.Range(pitchRange.x, pitchRange.y);
        float old = audioSource.pitch;
        audioSource.pitch = p;
        audioSource.PlayOneShot(clip, sfxVolume);
        audioSource.pitch = old;
    }

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
