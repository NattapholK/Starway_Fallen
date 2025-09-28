using UnityEngine;

public class BossBullet : MonoBehaviour
{
    [Header("Move")]
    public float speed = 6f;
    public Vector2 direction = Vector2.right; // กำหนดตอนยิง
    public float maxLife = 6f;

    [Header("Damage")]
    public int damage = 8;
    public LayerMask hittableLayers; // ติ๊กเฉพาะ Layer ของ Player

    [Header("FX")]
    public bool destroyOnHit = true;

    float life;

    void OnEnable()
    {
        life = maxLife;
    }

    void Update()
    {
        // วิ่งเส้นตรง 2D
        transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);

        life -= Time.deltaTime;
        if (life <= 0f) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // กรองเลเยอร์
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0) return;

        // ยิงดาเมจไปยัง PlayerHealth หรืออินเทอร์เฟซ 2D
        var d2 = other.GetComponentInParent<IDamageable2D>();
        if (d2 != null) d2.TakeDamage(damage, transform.position);
        else
        {
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage, transform.position);
        }

        if (destroyOnHit) gameObject.SetActive(false);
    }
}
