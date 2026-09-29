using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;

    private Coroutine hideCoroutine;

    void Awake()
    {
        Instance = this;
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    public void ShowTutorial(string message, float duration)
    {
        if (tutorialPanel == null || tutorialText == null) return;

        tutorialText.text = message;
        tutorialPanel.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        tutorialPanel.SetActive(false);
        hideCoroutine = null;
    }
}
