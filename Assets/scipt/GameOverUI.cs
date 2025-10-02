// File: GameOverUI.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI timeText;          // ลาก TMP Text มาใส่

    [Header("Fallback scene names (ถ้าไม่มี GameSession)")]
    public string fallbackBossRoomSceneName = "BossRoom";
    public string fallbackTitleSceneName    = "Start";

    void Start()
    {
        // กันลืม: คืนเวลาให้เดินปกติ
        if (Time.timeScale != 1f) Time.timeScale = 1f;

        // แสดงเวลาไฟต์ล่าสุด
        float sec = 0f;
        if (GameSession.Instance) sec = GameSession.Instance.lastRunSeconds;
        if (timeText) timeText.text = FormatTime(sec);

        // ให้ซีนนี้ค่อย ๆ โผล่ (ถ้ามี SceneFader)
        if (SceneFader.Instance != null)
        {
            Debug.Log("[GameOverUI] FadeIn start");
            SceneFader.Instance.FadeIn(0.6f);
        }
        else
        {
            Debug.Log("[GameOverUI] SceneFader not found - skip fade");
        }
    }

    string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = Mathf.FloorToInt(seconds / 60f);
        float s = seconds % 60f;
        return $"{m:00}:{s:00.00}";
    }

    // ----- Buttons -----
    public void OnClick_Restart()
    {
        string sceneName = fallbackBossRoomSceneName;
        if (GameSession.Instance && !string.IsNullOrWhiteSpace(GameSession.Instance.bossRoomSceneName))
            sceneName = GameSession.Instance.bossRoomSceneName;

        Debug.Log($"[GameOverUI] Restart clicked → load '{sceneName}'");

        // เผื่อโดนบังด้วยหน้ากากเฟด: บังคับเปิดใช้งาน + fade out แล้วค่อยโหลด
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneWithFade(sceneName, 0.4f, 0.4f);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void OnClick_QuitToTitle()
    {
        string sceneName = fallbackTitleSceneName;
        Debug.Log($"[GameOverUI] Quit clicked → load '{sceneName}'");

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneWithFade(sceneName, 0.4f, 0.4f);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
