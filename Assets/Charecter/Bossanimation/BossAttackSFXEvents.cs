// File: BossAttackSFXEvents.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BossAttackSFXEvents : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("จะสร้างให้อัตโนมัติถ้าเว้นว่างไว้")]
    public AudioSource source;

    [Header("Clips (ใส่เองตามใจ)")]
    public AudioClip swingStart;      // ช่วงง้าง/ยกดาบ
    public AudioClip swingWhoosh;     // ฟันลม/ไม่โดน
    public AudioClip hitFlesh;        // โดนเนื้อ
    public AudioClip hitArmor;        // โดนเกราะ/ของแข็ง
    public AudioClip step;            // เสียงเท้า
    public AudioClip hellCharge;      // ชาร์จปล่อย bullet hell
    public AudioClip hellBurst;       // ช็อตปล่อย bullet hell
    public AudioClip hurt;            // บอสโดนตี
    public AudioClip death;           // บอตาย

    [Header("Mix")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 1f)] public float pitchJitter = 0.08f;  // สุ่ม pitch นิด ๆ ให้ไม่ซ้ำ

    [Header("Smart Hit/Whiff")]
    [Tooltip("กี่วินาทีหลังจาก RegisterHit ที่ยังนับว่าเป็น 'ตีโดน' สำหรับเฟรมเสียง")]
    public float resolveWindow = 0.20f;

    float lastHitTime = -999f;
    bool  lastHitWasArmored = false;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D (ถ้าอยากเป็น 3D ปรับเป็น 1)
    }

    // ====== เมธอดสำหรับ Animation Event ======
    public void AE_SwingStart()  => Play(swingStart);
    public void AE_Step()        => Play(step);
    public void AE_HellCharge()  => Play(hellCharge);
    public void AE_HellBurst()   => Play(hellBurst);
    public void AE_Hurt()        => Play(hurt);
    public void AE_Death()       => Play(death);

    /// เรียกใน “เฟรมโดนจริง” ของคลิป: จะเลือกเล่น Hit หรือ Whoosh ให้เอง
    public void AE_MeleeResolve()
    {
        bool hit = (Time.time - lastHitTime) <= resolveWindow;
        if (hit)
            Play(lastHitWasArmored && hitArmor ? hitArmor : hitFlesh);
        else
            Play(swingWhoosh);
    }

    // ====== ให้สคริปต์บอสเรียกเมื่อทำดาเมจได้จริง ======
    public void RegisterHit(bool armored = false)
    {
        lastHitTime = Time.time;
        lastHitWasArmored = armored;
    }

    // ====== helper ======
    void Play(AudioClip clip)
    {
        if (!clip || !source) return;
        float p = 1f + Random.Range(-pitchJitter, pitchJitter);
        source.pitch = p;
        source.PlayOneShot(clip, volume);
    }
}
