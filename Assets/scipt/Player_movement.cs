// File: Player_movement.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player_movement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;       // ความเร็วขณะ Dash (หน่วยต่อวิ)
    [SerializeField] private float dashDuration = 0.15f;  // ระยะเวลาที่พุ่ง (วินาที)
    [SerializeField] private float dashCooldown = 0.4f;   // คูลดาวน์ระหว่าง Dash (วินาที)
    [SerializeField] private bool  allowAirTurn = true;   // ระหว่าง Dash อนุญาตให้เปลี่ยนทิศด้วยเมาส์ไหม (ส่วนใหญ่ false จะนิ่งกว่า)

    [Header("Refs")]
    [SerializeField] private Animator _animator;          // ถ้าไม่ drag จะหาในลูกให้อัตโนมัติ
    [SerializeField] private SpriteRenderer _sprite;      // สไปรต์ "ตัวละคร" (ไม่ใช่ดาบ)

    [Header("Tuning")]
    [SerializeField] private float faceDeadzone = 0.01f;  // กัน jitter เวลาตัดสิน flip จาก lookX

    // Cached
    private Rigidbody2D _rb;
    private Vector2 _input;

    // Dash state
    private bool   _isDashing;
    private float  _dashTimer;     // นับเวลาที่เหลือของ Dash
    private float  _cdTimer;       // นับคูลดาวน์ที่เหลือ
    private Vector2 _dashDir;      // ทิศขณะ Dash (normalized)

    // Animator param hashes
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int MoveX     = Animator.StringToHash("moveX");
    private static readonly int MoveY     = Animator.StringToHash("moveY");
    private static readonly int LookX     = Animator.StringToHash("lookX");
    private static readonly int LookY     = Animator.StringToHash("lookY");

    // เก็บทิศมองล่าสุด (กัน lookX/lookY = 0 พร้อมกัน)
    private Vector2 _lastLook = Vector2.down;

    // ให้สคริปต์อื่นเช็คได้ว่า “พุ่งอยู่ไหม” (เช่น ระบบชน/อมตะช่วงสั้น ๆ)
    public bool IsDashing => _isDashing;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_sprite   == null) _sprite   = GetComponentInChildren<SpriteRenderer>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        // ค่าที่เหมาะกับ top-down
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // ลดคูลดาวน์/ตัวจับเวลา Dash
        if (_cdTimer   > 0f) _cdTimer   -= Time.deltaTime;
        if (_dashTimer > 0f) _dashTimer -= Time.deltaTime;
        if (_dashTimer <= 0f && _isDashing)
        {
            _isDashing = false; // จบ Dash
        }

        // 1) รับอินพุตเดิน (ปกติ)
        float ix = Input.GetAxisRaw("Horizontal"); // -1,0,1
        float iy = Input.GetAxisRaw("Vertical");   // -1,0,1
        _input = new Vector2(ix, iy).normalized;
        bool walking = _input.sqrMagnitude > 0.0001f;

        // 2) หาเวคเตอร์จากผู้เล่น -> เมาส์ (หันหน้าอิงเมาส์)
        if (Camera.main == null) return; // อย่าลืม Tag กล้องหลักเป็น "MainCamera"
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 toMouse = (mouseWorld - transform.position);

        // เลือกแกนเด่นเพื่อบอก Idle_Up/Down/Side ชัด ๆ + กันศูนย์
        Vector2 lookDir = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : _lastLook;
        float ax = Mathf.Abs(lookDir.x);
        float ay = Mathf.Abs(lookDir.y);

        float lookX = 0f, lookY = 0f;
        if (ay > ax) { lookY = Mathf.Sign(lookDir.y); }
        else         { lookX = Mathf.Sign(lookDir.x); }

        // เก็บทิศมองล่าสุดไว้เป็น fallback
        if (lookX != 0f || lookY != 0f) _lastLook = new Vector2(lookX, lookY);

        // 3) เริ่ม Dash เมื่อกด Shift (ซ้ายหรือขวา) และไม่ติดคูลดาวน์/ไม่ได้พุ่งอยู่
       bool dashPressed = Input.GetKeyDown(KeyCode.Space);
        if (dashPressed && !_isDashing && _cdTimer <= 0f)
        {
            // เลือกทิศ Dash: ถ้ากำลังเดิน → ตามอินพุต, ถ้าไม่เดิน → ตามทิศมอง (เมาส์)
            _dashDir = walking
                ? _input
                : (_lastLook.sqrMagnitude > 0.0001f ? _lastLook.normalized : Vector2.down);

            _isDashing = true;
            _dashTimer = dashDuration;
            _cdTimer   = dashCooldown;

            // เคลียร์ความเร็วทันทีเพื่อให้ dash คม
            _rb.linearVelocity = Vector2.zero;
        }

        // 4) ส่งค่าให้ Animator (ไม่เพิ่มแอนิเมชัน Dash ตามคำขอ)
        if (_animator)
        {
            _animator.SetBool(IsWalking, !_isDashing && walking); // ถ้า dash อยู่ ถือว่าไม่เดิน
            _animator.SetFloat(MoveX, _input.x);
            _animator.SetFloat(MoveY, _input.y);
            _animator.SetFloat(LookX, lookX);
            _animator.SetFloat(LookY, lookY);
        }

        // 5) flipX ของสไปรต์ตัวละคร ให้หันตามเมาส์ (ซ้าย/ขวา)
        if (_sprite && Mathf.Abs(lookX) > faceDeadzone)
        {
            // ถ้า asset ฐานหันขวา: flip เมื่อเมาส์ซ้าย
            // ถ้า asset ฐานหันซ้าย ให้กลับเงื่อนไขเป็น (lookX > 0f)
            _sprite.flipX = (lookX < 0f);
        }

        // (ออปชัน) ระหว่าง Dash จะล็อกทิศตาม _dashDir ไม่ให้เปลี่ยนกลางอากาศ
        if (_isDashing && allowAirTurn && _input.sqrMagnitude > 0.0001f)
        {
            // ถ้าอยากให้ "หมุนกลาง Dash ตามเมาส์" เปิดบรรทัดนี้เป็นตามเมาส์แทน:
            // _dashDir = (_lastLook.sqrMagnitude > 0.0001f ? _lastLook.normalized : _dashDir);
            _dashDir = _input; // หรือจะให้หันตามปุ่มก็ได้
        }
    }

    void FixedUpdate()
    {
        if (_isDashing)
        {
            // 6) เคลื่อนที่ขณะ Dash (override การเดิน)
            Vector2 step = _dashDir.normalized * dashSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(_rb.position + step);
        }
        else
        {
            // 7) เคลื่อนที่ปกติด้วยฟิสิกส์
            _rb.MovePosition(_rb.position + _input * moveSpeed * Time.fixedDeltaTime);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_sprite   == null) _sprite   = GetComponentInChildren<SpriteRenderer>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
    }
#endif
}
