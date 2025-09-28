// File: BossBulletSpawner.cs
using System.Collections;
using UnityEngine;

public class BossBulletSpawner : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("จุดปล่อยกระสุน (ไม่ใส่จะใช้ตำแหน่งบอส)")]
    public Transform fireOrigin;
    [Tooltip("Prefab กระสุน (ต้องมีสคริปต์ BossBullet + Collider2D isTrigger)")]
    public GameObject bulletPrefab;
    [Tooltip("Transform ของผู้เล่น (ไม่ใส่จะหา tag=Player)")]
    public Transform player;

    [Header("Bullet Defaults")]
    [Min(0.1f)] public float bulletSpeed = 7f;
    [Min(1)]    public int   bulletDamage = 8;

    // ---------- Radial (ยิงเป็นวงกลมครั้งเดียว) ----------
    [Header("Radial Burst")]
    [Min(1)]    public int   radialCount = 24; // จำนวนกระสุนใน 1 วง
    public float angleOffsetDeg = 0f;          // หมุนแพทเทิร์นทั้งวง
    public float spawnRadius   = 0.0f;         // จุดเกิดห่างศูนย์กลางนิดหน่อย (กันชนตัวบอส)

    // ---------- Radial Waves (ยิงเป็นวงกลมหลายระลอก) ----------
    [Header("Radial Waves")]
    [Min(1)]    public int   waves = 3;
    public float waveInterval = 0.15f;         // เวลาห่างแต่ละระลอก
    public float waveSpeedAdd = 1.5f;          // ระลอกถัดไปเร็วขึ้นเล็กน้อย
    public bool  wavePhaseShift = true;        // หมุนเฟสระลอกรถัดไปให้สวยขึ้น

    // ---------- Spiral (พ่นสไปรัลต่อเนื่อง) ----------
    [Header("Spiral")]
    [Min(1)]    public int   spiralCount = 36; // จำนวนช็อตทั้งหมด
    public float spiralStepDeg = 12f;          // มุมเพิ่มต่อช็อต
    public float spiralInterval = 0.05f;       // เวลาห่างแต่ละช็อต
    public float spiralStartDeg = 0f;          // มุมตั้งต้น

    // ---------- Aimed Spread (เล็งผู้เล่นแล้วแตกพัด) ----------
    [Header("Aimed Spread")]
    [Min(1)]    public int   spreadCount = 5;  // จำนวนกระสุนในพัด
    public float spreadAngle = 30f;            // องศารวมของพัด

    void Awake()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (!fireOrigin) fireOrigin = transform;
    }

    // ===================== Helpers =====================
    void SpawnOne(Vector2 dir, Vector3? customPos = null, float speedMul = 1f)
    {
        if (!bulletPrefab) return;

        Vector3 pos = customPos ?? fireOrigin.position;
        var go = Instantiate(bulletPrefab, pos, Quaternion.identity);
        var b  = go.GetComponent<BossBullet>();
        if (b)
        {
            b.direction = dir.normalized;
            b.speed     = Mathf.Max(0.01f, bulletSpeed * speedMul);
            b.damage    = bulletDamage;
        }
    }

    Vector3 CenterPos() => fireOrigin ? fireOrigin.position : transform.position;

    // ===================== Animation Events =====================
    // ใส่ Event นี้ใน "เฟรมดาบปักพื้น" เพื่อยิงเป็นวงกลมรอบตัวครั้งเดียว
    public void AE_SpawnRadialBurst()
    {
        if (!bulletPrefab) return;
        Vector3 center = CenterPos();

        float step = 360f / Mathf.Max(1, radialCount);
        for (int i = 0; i < radialCount; i++)
        {
            float ang = (angleOffsetDeg + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized;

            Vector3 spawnPos = center + (Vector3)(dir * Mathf.Max(0f, spawnRadius));
            SpawnOne(dir, spawnPos);
        }
    }

    // ใส่ Event นี้แทน ถ้าอยากปล่อยหลายระลอกต่อเนื่อง
    public void AE_SpawnRadialWaves()
    {
        StopCoroutine(nameof(RadialWavesCR));
        StartCoroutine(RadialWavesCR());
    }

    IEnumerator RadialWavesCR()
    {
        float baseSpeed   = bulletSpeed;
        float baseOffset  = angleOffsetDeg;

        for (int w = 0; w < waves; w++)
        {
            AE_SpawnRadialBurst();
            bulletSpeed += waveSpeedAdd;

            if (wavePhaseShift && radialCount > 0)
                angleOffsetDeg += (360f / radialCount) * 0.5f; // หมุนเฟสระลอกถัดไป

            yield return new WaitForSeconds(waveInterval);
        }

        // คืนค่า
        bulletSpeed   = baseSpeed;
        angleOffsetDeg = baseOffset;
    }

    // สไปรัลต่อเนื่อง (จะพ่นทีละช็อตเป็นวงหมุน)
    public void AE_SpawnSpiral()
    {
        StopCoroutine(nameof(SpiralCR));
        StartCoroutine(SpiralCR());
    }

    IEnumerator SpiralCR()
    {
        Vector3 center = CenterPos();
        float angDeg = spiralStartDeg;

        for (int i = 0; i < spiralCount; i++)
        {
            float rad = angDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            Vector3 spawnPos = center + (Vector3)(dir * Mathf.Max(0f, spawnRadius));
            SpawnOne(dir, spawnPos);

            angDeg += spiralStepDeg;
            yield return new WaitForSeconds(spiralInterval);
        }
    }

    // เล็งผู้เล่นแล้วแตกเป็นพัด
    public void AE_SpawnAimedSpread()
    {
        if (!player) return;

        Vector3 center = CenterPos();
        Vector2 baseDir = ((Vector2)player.position - (Vector2)center).normalized;
        int n = Mathf.Max(1, spreadCount);
        float total = Mathf.Max(0f, spreadAngle);

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (i / (float)(n - 1) - 0.5f); // -0.5..0.5
            float ang = t * total;
            Vector2 dir = Quaternion.Euler(0, 0, ang) * baseDir;

            Vector3 spawnPos = center + (Vector3)(dir * Mathf.Max(0f, spawnRadius));
            SpawnOne(dir, spawnPos);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // แสดงจุดเกิด + รัศมี spawnRadius
        if (!fireOrigin) fireOrigin = transform;
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(fireOrigin.position, Mathf.Max(0f, spawnRadius));

        // พรีวิวทิศของ Radial (เล็กน้อย)
        float step = (radialCount > 0) ? (360f / radialCount) : 90f;
        int preview = Mathf.Clamp(radialCount, 1, 16);
        for (int i = 0; i < preview; i++)
        {
            float ang = (angleOffsetDeg + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Gizmos.DrawLine(fireOrigin.position, fireOrigin.position + (Vector3)(dir * 1.5f));
        }
    }
#endif
}
