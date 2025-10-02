// File: GameSession.cs
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Last run result")]
    public float lastRunSeconds = 0f;      // เวลาไฟต์ล่าสุด (วินาที)

    [Header("Scenes")]
    public string bossRoomSceneName = "BossRoom"; // ไว้ Restart

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
