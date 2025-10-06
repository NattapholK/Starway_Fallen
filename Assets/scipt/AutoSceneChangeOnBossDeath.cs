// File: AutoSceneChangeOnBossDeath.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BossHealth))]
public class AutoSceneChangeOnBossDeath : MonoBehaviour
{
    [Header("Scene to load when Boss dies")]
    [Tooltip("ชื่อ Scene ที่จะโหลดหลังบอสตาย (ต้องอยู่ใน Build Settings)")]
    public string nextSceneName = "NextLevel";

    [Header("Transition Settings")]
    [Tooltip("เวลารอก่อนเริ่มเปลี่ยนซีน (หน่วงเพื่อให้อนิเมชั่น/เอฟเฟกต์เล่นก่อน)")]
    public float delayBeforeLoad = 2.0f;

    [Tooltip("ระยะเวลาจางเข้า (fade to black)")]
    public float fadeInDuration = 0.6f;

    [Tooltip("ระยะเวลาจางออก (fade from black)")]
    public float fadeOutDuration = 0.6f;

    [Header("Optional: GameSession link (ถ้ามี)")]
    public GameSession session;

    private bool hasTriggered = false;

    void Awake()
    {
        // หา GameSession ถ้ายังไม่มี
        if (session == null)
        {
#if UNITY_2023_1_OR_NEWER
            session = Object.FindFirstObjectByType<GameSession>(FindObjectsInactive.Include);
#else
            session = FindObjectOfType<GameSession>();
#endif
            if (session == null)
            {
                var go = new GameObject("_GameSession");
                session = go.AddComponent<GameSession>();
            }
        }

        // ผูก event กับ BossHealth
        var bh = GetComponent<BossHealth>();
        if (bh != null)
        {
            bh.onDeath.AddListener(OnBossDeath);
        }
        else
        {
            Debug.LogError("[AutoSceneChangeOnBossDeath] ❌ BossHealth not found on this GameObject.");
        }
    }

    public void OnBossDeath()
    {
        if (hasTriggered) return; // กันไม่ให้เรียกซ้ำ
        hasTriggered = true;

        Debug.Log("[AutoSceneChangeOnBossDeath] 🌀 Boss defeated! Waiting before scene transition...");

        // เก็บเวลาล่าสุดไว้ใน GameSession (ถ้ามี)
        if (session != null)
            session.lastRunSeconds = Time.timeSinceLevelLoad;

        // เริ่ม coroutine รอและโหลดซีน
        StartCoroutine(WaitAndLoadScene());
    }

    private System.Collections.IEnumerator WaitAndLoadScene()
    {
        // หน่วงเวลาให้บอสตายแล้วมีอนิเมชัน/เอฟเฟกต์ก่อนเปลี่ยนซีน
        yield return new WaitForSeconds(delayBeforeLoad);

        // โหลดซีนพร้อม Fade ถ้ามี SceneFader
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneWithFade(nextSceneName, fadeInDuration, fadeOutDuration);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void OnDestroy()
    {
        var bh = GetComponent<BossHealth>();
        if (bh != null)
        {
            bh.onDeath.RemoveListener(OnBossDeath);
        }
    }
}
