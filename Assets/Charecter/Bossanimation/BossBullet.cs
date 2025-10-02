// File: BossBullet.cs  (Parry = Destroy)
using UnityEngine;

public class BossBullet : MonoBehaviour, IDamageable2D, IDamageable
{
    [Header("Move")]
    public float speed = 6f;
    public Vector2 direction = Vector2.right;
    public float maxLife = 6f;

    [Header("Damage")]
    public int damage = 8;
    [Tooltip("เลเยอร์ที่กระสุนทำดาเมจใส่ (ตอนเป็นกระสุนของบอส) — ใส่เฉพาะ Player")]
    public LayerMask hittableLayers = 0;

    [Header("FX (optional)")]
    public bool destroyOnHit = true;
    public GameObject vfxHit;       // เอฟเฟกต์โดนผู้เล่น
    public GameObject vfxParried;   // เอฟเฟกต์โดนฟันทำลาย
    public AudioClip sfxHit;
    public AudioClip sfxParried;

    Rigidbody2D rb;
    AudioSource _audio;
    float life;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _audio = GetComponent<AudioSource>();
        if (!_audio) _audio = gameObject.AddComponent<AudioSource>();
        if (rb) { rb.gravityScale = 0f; } // กันตก
    }

    void OnEnable() => life = maxLife;

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }

        Vector2 step = direction.normalized * speed * Time.deltaTime;
        if (rb) rb.MovePosition((Vector2)transform.position + step);
        else transform.position += (Vector3)step;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ทำดาเมจเฉพาะเลเยอร์ที่กำหนด (เช่น Player)
        if (((1 << other.gameObject.layer) & hittableLayers) != 0)
        {
            var d2 = other.GetComponentInParent<IDamageable2D>();
            if (d2 != null) d2.TakeDamage(damage, transform.position);
            else other.GetComponentInParent<PlayerHealth>()?.TakeDamage(damage, transform.position);

            PlayOneShot(sfxHit);
            if (destroyOnHit) Kill(vfxHit);
        }
    }

    // ===== Parry (ถูกเรียกจากดาบผู้เล่นผ่าน AttackController) =====
    public void TakeDamage(int amount, Vector3 hitFrom) { OnParried(); }
    public void TakeDamage(int amount)                   { OnParried(); }

    void OnParried()
    {
        PlayOneShot(sfxParried);
        Kill(vfxParried); // ทำลายทันที (ไม่สะท้อน)
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip) _audio.PlayOneShot(clip);
    }

    void Kill(GameObject fx)
    {
        if (fx) Instantiate(fx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
