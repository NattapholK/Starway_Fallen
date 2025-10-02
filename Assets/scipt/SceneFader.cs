// File: SceneFader.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Fade Durations")]
    [Min(0f)] public float defaultFadeOut = 0.6f; // ก่อนโหลด
    [Min(0f)] public float defaultFadeIn  = 0.6f; // หลังโหลด

    [Header("Look & Feel")]
    public Color fadeColor = Color.black;
    [Range(0f,1f)] public float startAlpha = 0f;   // เริ่มโปร่ง(0) หรือเริ่มทึบ(1)

    Canvas _canvas;
    CanvasGroup _group;
    Image _img;
    bool _isFading = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupCanvas();
        SetAlpha(Mathf.Clamp01(startAlpha));
    }

    void SetupCanvas()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = true;

        var go = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        _img = go.GetComponent<Image>();
        _img.color = fadeColor;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    void SetAlpha(float a)
    {
        _group.alpha = a;
        _group.blocksRaycasts = a > 0.001f;
    }

    public void LoadSceneWithFade(string sceneName, float fadeOut = -1f, float fadeIn = -1f)
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        if (_isFading) return;
        StartCoroutine(FadeLoadCR(sceneName,
            fadeOut < 0f ? defaultFadeOut : fadeOut,
            fadeIn  < 0f ? defaultFadeIn  : fadeIn));
    }

    IEnumerator FadeLoadCR(string sceneName, float outDur, float inDur)
    {
        _isFading = true;
        yield return FadeTo(1f, outDur);                       // OUT
        yield return SceneManager.LoadSceneAsync(sceneName);   // LOAD
        yield return FadeTo(0f, inDur);                        // IN
        _isFading = false;
    }

    public void FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeIn;
        StartCoroutine(FadeTo(0f, duration));
    }

    public void FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeOut;
        StartCoroutine(FadeTo(1f, duration));
    }

    IEnumerator FadeTo(float target, float dur)
    {
        dur = Mathf.Max(0f, dur);
        float start = _group.alpha;
        if (dur == 0f) { SetAlpha(target); yield break; }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            SetAlpha(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetAlpha(target);
    }
}
