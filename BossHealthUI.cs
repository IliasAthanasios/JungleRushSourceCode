using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI Instance;
    public Slider healthSlider;
    private UIGradient gradientEffect;

    void Awake()
    {
        Instance = this;
        if (healthSlider == null) healthSlider = GetComponent<Slider>();
        if (healthSlider.fillRect != null)
            gradientEffect = healthSlider.fillRect.GetComponent<UIGradient>();
            
        gameObject.SetActive(false);
    }

    public void Setup(float maxHealth)
    {
        gameObject.SetActive(true);
        healthSlider.maxValue = maxHealth;
        StopAllCoroutines();
        StartCoroutine(AnimateValue(0, maxHealth, 0.5f)); 
    }

    public void UpdateHealth(float currentHealth)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateValue(healthSlider.value, currentHealth, 1.0f)); 
    }

    private IEnumerator AnimateValue(float start, float end, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            healthSlider.value = Mathf.Lerp(start, end, elapsed / duration);
            
            if (gradientEffect != null)
                gradientEffect.Refresh(healthSlider.normalizedValue);
                
            yield return null;
        }
        healthSlider.value = end;
        if (gradientEffect != null) gradientEffect.Refresh(healthSlider.normalizedValue);
    }

    public void Hide() => gameObject.SetActive(false);
}
