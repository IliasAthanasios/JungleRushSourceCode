using UnityEngine;
using TMPro;
using UnityEngine.UI; // Added for Image
using System.Collections;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI loadingText;
    public RectTransform spinnerIcon;
    public Image loadingBar; // Drag your bar image here

    [Header("Settings")]
    public float fadeSpeed = 2f;
    public float pulseSpeed = 3f;
    public float spinSpeed = -200f;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (loadingBar != null) loadingBar.fillAmount = 0;
    }

    private void Update()
    {
        if (loadingText != null && gameObject.activeInHierarchy)
        {
            float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            loadingText.alpha = Mathf.Lerp(0.3f, 1f, alpha);
        }

        if (spinnerIcon != null && gameObject.activeInHierarchy)
        {
            spinnerIcon.Rotate(0, 0, spinSpeed * Time.deltaTime);
        }
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        if (loadingBar != null) loadingBar.fillAmount = 0;
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f));
    }

    public void SetProgress(float progress)
    {
        if (loadingBar != null)
        {
            // This ensures the fill is smooth
            loadingBar.fillAmount = progress;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool deactivateAtEnd = false)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        if (deactivateAtEnd && targetAlpha == 0) gameObject.SetActive(false);
    }
}