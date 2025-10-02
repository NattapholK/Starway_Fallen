// File: AutoGameOverOnDeath.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerHealth))]
public class AutoGameOverOnDeath : MonoBehaviour
{
    [Header("Scene to load on death")]
    [SerializeField] private string gameOverSceneName = "Game over"; // ต้องใส่ใน Build Settings

    [Header("Optional: Session link")]
    [SerializeField] private GameSession session;   // เว้นได้ เดี๋ยวหา/สร้างให้

    void Awake()
    {
        // Session
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

        // Hook PlayerHealth.onDeath
        var ph = GetComponent<PlayerHealth>();
        if (ph != null) ph.onDeath.AddListener(OnPlayerDeath);
        else Debug.LogError("[AutoGameOverOnDeath] PlayerHealth not found.");
    }

    public void OnPlayerDeath()
    {
        // บันทึกเวลาไฟต์ล่าสุด
        if (session) session.lastRunSeconds = Time.timeSinceLevelLoad;

        // โหลดซีน Game Over แบบเฟด
        if (SceneFader.Instance != null)
            SceneFader.Instance.LoadSceneWithFade(gameOverSceneName, 0.6f, 0.6f);
        else
            SceneManager.LoadScene(gameOverSceneName);
    }

    void OnDestroy()
    {
        var ph = GetComponent<PlayerHealth>();
        if (ph != null) ph.onDeath.RemoveListener(OnPlayerDeath);
    }
}
