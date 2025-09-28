using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [Header("Source")]
    public PlayerHealth player;

    [Header("UI Refs (choose one)")]
    public Image fillImage;   // ใช้กับ Image (Type = Filled)
    public Slider slider;     // หรือใช้ Slider ก็ได้

    [Header("Look & Feel")]
    public bool smooth = true;
    public float lerpSpeed = 8f;
    public Gradient colorByHP; // ไม่ใส่ก็ได้

    float target01;
    float shown01;

    void Reset()
    {
        player = FindObjectOfType<PlayerHealth>();
    }

    void LateUpdate()
    {
        if (!player) return;

        target01 = player.maxHP > 0 ? (player.currentHP / (float)player.maxHP) : 0f;
        target01 = Mathf.Clamp01(target01);

        shown01 = smooth
            ? Mathf.MoveTowards(shown01, target01, lerpSpeed * Time.deltaTime)
            : target01;

        if (fillImage)
        {
            fillImage.fillAmount = shown01;
            if (colorByHP != null) fillImage.color = colorByHP.Evaluate(shown01);
        }

        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = shown01;

            if (colorByHP != null && slider.fillRect)
            {
                var img = slider.fillRect.GetComponent<Image>();
                if (img) img.color = colorByHP.Evaluate(shown01);
            }
        }
    }
}
