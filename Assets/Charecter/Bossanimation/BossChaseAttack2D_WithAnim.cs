// File: BossChaseAttack2D_WithAnim.cs  (Attack + Bullet Hell + SFX hooks + Death)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class BossChaseAttack2D_WithAnim : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // เว้นว่างได้ จะหา tag=Player ให้

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.3f;
    public float attackRange = 1.8f;
    public float repathInterval = 0.05f;

    [Header("Attack (animation-driven)")]
    public float attackCooldown = 1.0f;
    public string attackTrigger = "Attack";
    public string attackingBool = "IsAttacking";
    public bool   lockFacingDuringAttack = true;
    public int    baseDamage = 12;

    [Tooltip("ชื่อ state ของคลิปโจมตีใน Animator (เช่น 'Normal_at')")]
    public string attackStateName = "Normal_at";
    [Tooltip("กันค้าง: ถ้าเกินเวลานี้จะปลดล็อกเอง แม้ Animation Event ไม่ถูกยิง")]
    public float maxAttackDuration = 1.2f;

    [Header("Bullet Hell")]
    public string hellTrigger = "Hell";
    [Tooltip("ชื่อ state ของคลิป Bullet Hell (เช่น 'Hell_frame')")]
    public string hellStateName = "Hell_frame";
    public float hellCooldown = 10f;
    public float maxHellDuration = 2.0f;
    public bool  hellOnlyWhenInRange = false;

    [Header("Hitbox (OverlapBox)")]
    public Transform hitOrigin;
    public Vector2  boxSize = new Vector2(1.4f, 0.6f);
    public float    forwardOffset = 0.6f;
    public LayerMask hittableLayers;

    [Header("Animator / Flip")]
    public string runBool = "isRunning";
    public SpriteRenderer bodySprite;

    public enum FlipMethod { SpriteRenderer, LocalScale }
    public FlipMethod flipMethod = FlipMethod.SpriteRenderer;

    [Tooltip("งานศิลป์เดิมหันขวาคือหน้า? (true = ขวา, false = ซ้าย)")]
    public bool artworkFacesRight = true;

    [Tooltip("หันหาผู้เล่นทุกเฟรม (ตอนเดิน/ยืนนิ่ง)")]
    public bool facePlayerAlways = true;

    [Tooltip("กันสั่นขณะผู้เล่นเกือบแนวตั้งเดียวกัน")]
    public float flipDeadZone = 0.02f;

    [Header("SFX (optional)")]
    [Tooltip("อ้างอิง BossAttackSFXEvents เพื่อใช้ Animation Events เล่นเสียง")]
    public BossAttackSFXEvents sfx;

    // ====== Death / Dying ======
    [Header("Death")]
    [Tooltip("Trigger ใน Animator สำหรับอนิเมตาย")]
    public string deathTrigger = "Die";
    [Tooltip("Bool ใน Animator ไว้ล็อกสถานะตาย (ไม่บังคับ)")]
    public string deadBool = "IsDead";
    [Tooltip("ปิดสคริปต์/ระบบยิงกระสุนเมื่อบอสตาย")]
    public bool disableBulletSpawnerOnDeath = true;
    [Tooltip("ปิดคอลลิเดอร์บนบอสเมื่อเริ่มตาย (กันโดนซ้ำ)")]
    public bool disableCollidersOnDeath = true;

    [Header("Bullet Spawner (optional)")]
    public BossBulletSpawner bulletSpawner; // ใส่คอมโพเนนต์ spawner ไว้บนบอส

    // ===== internal =====
    Rigidbody2D rb;
    Animator anim;
    Vector2 moveDir;
    float repathTimer = 0f;

    bool  attacking = false;
    float atkCD = 0f;

    bool  inHell = false;
    float hellCD = 0f;

    bool  isDead = false;

    // กันโดนซ้ำในสวิงเดียวกัน
    readonly HashSet<Collider2D> _hitThisSwing = new HashSet<Collider2D>();

    // สำหรับ Flip แบบ LocalScale
    Vector3 _originalScale;
    int _facingSign = +1; // +1 ขวา, -1 ซ้าย

    // Fallback ตรวจ state
    int _attackStateHash;
    int _hellStateHash;
    float _attackTimer;
    float _hellTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (!player)
        {
            var pGo = GameObject.FindWithTag("Player");
            if (pGo) player = pGo.transform;
#if UNITY_2023_1_OR_NEWER
            if (!player)
            {
                var p = Object.FindFirstObjectByType<PlayerHealth>();
                if (p) player = p.transform;
            }
#endif
        }
        if (!hitOrigin) hitOrigin = transform;
        if (!bulletSpawner) bulletSpawner = GetComponent<BossBulletSpawner>();
        if (!sfx) sfx = GetComponent<BossAttackSFXEvents>();

        // ผูกกับ BossHealth เพื่อฟัง onDeath
        var bh = GetComponent<BossHealth>();
        if (bh != null) bh.onDeath.AddListener(OnBossDeath);

        _originalScale   = transform.localScale;
        _attackStateHash = Animator.StringToHash(attackStateName);
        _hellStateHash   = Animator.StringToHash(hellStateName);
    }

    void Update()
    {
        if (isDead) { anim.SetBool(runBool, false); return; }  // ตายแล้วไม่ทำอะไรต่อ

        if (!player) { anim.SetBool(runBool, false); return; }

        if (atkCD  > 0f) atkCD  -= Time.deltaTime;
        if (hellCD > 0f) hellCD -= Time.deltaTime;

        // อัปเดตทิศวิ่งแบบ step
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            Vector2 toP = (player.position - transform.position);
            moveDir = toP.sqrMagnitude > 0.0001f ? toP.normalized : Vector2.zero;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Face
        if (facePlayerAlways && !attacking && !inHell)
            TryFaceByX(player.position.x - transform.position.x);

        // อยู่ใน Hell
        if (inHell)
        {
            anim.SetBool(runBool, false);

            _hellTimer += Time.deltaTime;
            var st = anim.GetCurrentAnimatorStateInfo(0);
            bool inHellState = st.shortNameHash == _hellStateHash;

            if ((!inHellState && !anim.IsInTransition(0)) || _hellTimer >= maxHellDuration)
            {
                inHell = false;
            }
            return;
        }

        // อยู่ใน Attack ปกติ
        if (attacking)
        {
            anim.SetBool(runBool, false);

            _attackTimer += Time.deltaTime;
            var st = anim.GetCurrentAnimatorStateInfo(0);
            bool inAttackState = st.shortNameHash == _attackStateHash;

            if ((!inAttackState && !anim.IsInTransition(0)) || _attackTimer >= maxAttackDuration)
            {
                attacking = false;
                if (!string.IsNullOrEmpty(attackingBool))
                    anim.SetBool(attackingBool, false);
            }
            return;
        }

        // เรียก Bullet Hell เมื่อคูลดาวน์พร้อม
        if (hellCD <= 0f && (!hellOnlyWhenInRange || dist <= attackRange * 1.2f))
        {
            TriggerHell();
            return;
        }

        // เริ่มโจมตีปกติเมื่อเข้าระยะ
        if (dist <= attackRange && atkCD <= 0f)
        {
            StartAttack();
            return;
        }

        // เดินเข้าใกล้
        if (dist > stopDistance)
        {
            anim.SetBool(runBool, true);
            if (!facePlayerAlways) TryFaceByX(moveDir.x);
        }
        else
        {
            anim.SetBool(runBool, false);
            moveDir = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (isDead || attacking || inHell) return;
        if (moveDir.sqrMagnitude > 0f)
        {
            Vector2 next = (Vector2)transform.position + moveDir * (moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);
        }
    }

    // ===== Attack (Normal) =====
    void StartAttack()
    {
        attacking   = true;
        atkCD       = attackCooldown;
        _attackTimer = 0f;
        _hitThisSwing.Clear();

        if (lockFacingDuringAttack)
            TryFaceByX(player.position.x - transform.position.x);

        if (!string.IsNullOrEmpty(attackingBool))
            anim.SetBool(attackingBool, true);

        anim.ResetTrigger(attackTrigger);
        anim.SetTrigger(attackTrigger);
    }

    // Animation Events (Normal_at)
    public void AE_AttackBegin()
    {
        if (isDead) return;
        _hitThisSwing.Clear();
        sfx?.AE_SwingStart(); // ง้างดาบ/เริ่มสวิง
    }

    public void AE_AttackHit()
    {
        if (isDead) return;
        PerformHitboxOnce();  // ตรวจโดนจริง
        // ไม่เล่นเสียงที่นี่ ให้ไปตัดสินใน AE_MeleeResolve() แทน
    }

    public void AE_AttackEnd()
    {
        if (isDead) return;
        attacking = false;
        if (!string.IsNullOrEmpty(attackingBool))
            anim.SetBool(attackingBool, false);
    }

    // ===== Bullet Hell =====
    void TriggerHell()
    {
        inHell    = true;
        hellCD    = hellCooldown;
        _hellTimer = 0f;

        TryFaceByX(player.position.x - transform.position.x);

        anim.ResetTrigger(hellTrigger);
        anim.SetTrigger(hellTrigger);
    }

    // Animation Events (Hell_frame)
    public void AE_HellBegin()
    {
        if (isDead) return;
        sfx?.AE_HellCharge();
    }

    // เรียกบน “เฟรมดาบปักพื้น”
    public void AE_HellFireBurst()
    {
        if (isDead) return;

        if (bulletSpawner != null)
        {
            bulletSpawner.AE_SpawnRadialBurst();
            // bulletSpawner.AE_SpawnRadialWaves();
            // bulletSpawner.AE_SpawnSpiral();
        }
        sfx?.AE_HellBurst();
    }

    public void AE_HellEnd()
    {
        if (isDead) return;
        inHell = false;
    }

    // ===== SFX resolve (เรียกจากคีย์เฟรม contact) =====
    public void AE_MeleeResolve()
    {
        if (isDead) return;
        sfx?.AE_MeleeResolve();
    }

    public void AE_Hurt()  { if (!isDead) sfx?.AE_Hurt();  }
    public void AE_Death() { sfx?.AE_Death(); }

    // ======= Death (hook จาก BossHealth.onDeath) =======
    public void OnBossDeath()
    {
        if (isDead) return;
        isDead = true;

        // หยุดสถานะการต่อสู้ทั้งหมด
        attacking = false;
        inHell = false;
        moveDir = Vector2.zero;
        atkCD = 999f;
        hellCD = 999f;

        // ปิดคูลดาวน์/ตัววิ่ง
        anim.SetBool(runBool, false);

        // รีเซ็ตทริกเกอร์อื่น ๆ กันชนกับ Die
        anim.ResetTrigger(attackTrigger);
        anim.ResetTrigger(hellTrigger);

        // ตั้งค่า Bool ตาย (ถ้ามี) + ยิงทริกเกอร์ Die
        if (!string.IsNullOrEmpty(deadBool)) anim.SetBool(deadBool, true);
        if (!string.IsNullOrEmpty(deathTrigger)) anim.SetTrigger(deathTrigger);

        // หยุดฟิสิกส์เคลื่อนที่
        if (rb) rb.velocity = Vector2.zero;

        // ปิดสปาวน์กระสุน/คอลลิเดอร์ถ้าต้องการ
        if (disableBulletSpawnerOnDeath && bulletSpawner) bulletSpawner.enabled = false;

        if (disableCollidersOnDeath)
        {
            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;
        }

        // แจ้ง SFX (ถ้าอยากให้มีเสียงตอนเริ่มตาย)
        sfx?.AE_Death();
    }

    // เรียกจากคีย์เฟรมท้ายคลิปตาย (ถ้าอยาก)
    public void AE_DeathFinished()
    {
        // ปล่อยว่างไว้ — ถ้าจะให้ลบตัว/ดรอปของ ค่อยไปทำใน AutoSceneChangeOnBossDeath หรืออีกสคริปต์
    }

    // ===== Hitbox core =====
    void PerformHitboxOnce()
    {
        if (!player || isDead) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Vector2 origin = hitOrigin ? (Vector2)hitOrigin.position : (Vector2)transform.position;
        Vector2 center = origin + dir * forwardOffset;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bool hitAnyone = false;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, angle, hittableLayers);
        foreach (var c in hits)
        {
            if ((c.attachedRigidbody && c.attachedRigidbody.transform == transform) || _hitThisSwing.Contains(c))
                continue;

            _hitThisSwing.Add(c);
            hitAnyone = true;

            var target = c.GetComponentInParent<IDamageable2D>();
            if (target != null)
            {
                target.TakeDamage(baseDamage, transform.position);
            }
            else
            {
                var ph = c.GetComponentInParent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(baseDamage, transform.position);
            }
        }

#if UNITY_EDITOR
        DebugDrawBox(center, boxSize, angle, Color.red, 0.02f);
#endif

        if (hitAnyone) sfx?.RegisterHit(false);
    }

    // ===== Flip =====
    void TryFaceByX(float xDir)
    {
        if (Mathf.Abs(xDir) <= flipDeadZone) return;
        int wantSign = xDir > 0f ? +1 : -1;
        if (wantSign == _facingSign) return;
        _facingSign = wantSign;

        if (flipMethod == FlipMethod.SpriteRenderer)
        {
            if (!bodySprite) return;
            bool shouldFaceLeft = (_facingSign < 0);
            bodySprite.flipX = artworkFacesRight ? shouldFaceLeft : !shouldFaceLeft;
        }
        else
        {
            var s = _originalScale;
            float sign = artworkFacesRight ? _facingSign : -_facingSign;
            s.x = Mathf.Abs(s.x) * sign;
            transform.localScale = s;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    static void DebugDrawBox(Vector2 center, Vector2 size, float angleDeg, Color col, float dur)
    {
        Quaternion rot = Quaternion.Euler(0, 0, angleDeg);
        Vector2 hx = (Vector2)(rot * Vector2.right) * (size.x * 0.5f);
        Vector2 hy = (Vector2)(rot * Vector2.up)    * (size.y * 0.5f);
        Vector2 a = center + hx + hy;
        Vector2 b = center + hx - hy;
        Vector2 c = center - hx - hy;
        Vector2 d = center - hx + hy;
        Debug.DrawLine(a, b, col, dur); Debug.DrawLine(b, c, col, dur);
        Debug.DrawLine(c, d, col, dur); Debug.DrawLine(d, a, col, dur);
    }
#endif
}
