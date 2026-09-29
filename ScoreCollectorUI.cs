using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreCollectorUI : MonoBehaviour
{
    public static ScoreCollectorUI Instance;

    [Header("References")]
    public Canvas canvas;
    public RectTransform scoreIconTarget;
    public Image bananaTemplate;

    [Header("Settings")]
    public float travelTime = 0.8f;
    public float spawnRadius = 60f;
    public AnimationCurve travelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        Instance = this;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (bananaTemplate != null) bananaTemplate.gameObject.SetActive(false);
        if (travelCurve == null) travelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public void SpawnBananas(int count, Vector3 worldPosition, AudioClip collectSound)
    {
        Camera cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        Vector2 screenPos = cam != null ? (Vector2)cam.WorldToScreenPoint(worldPosition) : new Vector2(Screen.width / 2f, Screen.height / 2f);
        StartCoroutine(CollectionRoutine(count, screenPos, worldPosition, collectSound));
    }

    private IEnumerator CollectionRoutine(int count, Vector2 screenStartPos, Vector3 worldStartPos, AudioClip collectSound)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            SpawnSingleBanana(screenStartPos + offset);

            if (collectSound != null)
            {
                SFXManager.PlaySFXAtPoint(collectSound, worldStartPos);
            }

            yield return new WaitForSeconds(0.08f);
        }
    }

    private void SpawnSingleBanana(Vector2 startPos)
    {
        if (bananaTemplate == null || canvas == null) return;
        Image banana = Instantiate(bananaTemplate, canvas.transform);
        banana.gameObject.SetActive(true);
        banana.rectTransform.position = startPos;
        StartCoroutine(TravelRoutine(banana));
    }

    private IEnumerator TravelRoutine(Image banana)
    {
        float elapsed = 0f;
        Vector3 start = banana.rectTransform.position;
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = travelCurve.Evaluate(elapsed / travelTime);
            if (scoreIconTarget != null) banana.rectTransform.position = Vector3.Lerp(start, scoreIconTarget.position, t);
            banana.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t);
            yield return null;
        }
        Destroy(banana.gameObject);
    }
}