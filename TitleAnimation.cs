using UnityEngine;
using TMPro;

public class UltimateTitleAnimator : MonoBehaviour
{
    private TMP_Text m_TextComponent;

    [Header("Warp (Curve) Settings - Always Active")]
    public bool useWarp = true;
    public AnimationCurve VertexCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1.0f), new Keyframe(1, 0));
    public float curveScale = 25f;

    [Header("Wavy (Sine) Settings - Always Active")]
    public bool useWavy = true;
    public float wavyAmount = 10f;
    public float wavySpeed = 2f;
    public float wavyCurveScale = 0.5f;

    [Header("Shake (Jitter) Settings - Randomly Triggered")]
    public bool useShake = true;
    public float shakeAmount = 2f;
    public float shakeSpeed = 10f;

    [Header("Randomness Settings (For Shake Only)")]
    public float minIdleTime = 1f;        
    public float maxIdleTime = 3f;
    public float minShakeDuration = 0.5f; 
    public float maxShakeDuration = 1.5f;

    private struct CharStatus
    {
        public bool isShaking;
        public float timer;
    }

    private CharStatus[] charStatuses;

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        m_TextComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = m_TextComponent.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) return;

        if (charStatuses == null || charStatuses.Length != characterCount)
        {
            charStatuses = new CharStatus[characterCount];
            for (int i = 0; i < characterCount; i++)
            {
                charStatuses[i].timer = Random.Range(0f, maxIdleTime);
                charStatuses[i].isShaking = false;
            }
        }

        float boundsMinX = m_TextComponent.bounds.min.x;
        float boundsMaxX = m_TextComponent.bounds.max.x;

        for (int i = 0; i < characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            if (useShake)
            {
                charStatuses[i].timer -= Time.deltaTime;
                if (charStatuses[i].timer <= 0)
                {
                    charStatuses[i].isShaking = !charStatuses[i].isShaking;
                    charStatuses[i].timer = charStatuses[i].isShaking ? 
                        Random.Range(minShakeDuration, maxShakeDuration) : 
                        Random.Range(minIdleTime, maxIdleTime);
                }
            }

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 offsetToMidBaseline = new Vector2((vertices[vertexIndex + 0] + vertices[vertexIndex + 2]).x / 2, textInfo.characterInfo[i].baseLine);

            float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
            float x1 = x0 + 0.0001f;
            float y0 = useWarp ? VertexCurve.Evaluate(x0) * curveScale : 0;
            float y1 = useWarp ? VertexCurve.Evaluate(x1) * curveScale : 0;
            float angle = useWarp ? Mathf.Atan2(y1 - y0, (x1 - x0) * (boundsMaxX - boundsMinX)) * Mathf.Rad2Deg : 0;
            Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

            float wavyY = useWavy ? Mathf.Sin(Time.time * wavySpeed + i * wavyCurveScale) * wavyAmount : 0;
            Vector3 wavyOffset = new Vector3(0, wavyY, 0);

            Vector3 shakeOffset = Vector3.zero;
            if (charStatuses[i].isShaking)
            {
                shakeOffset = new Vector3(
                    Random.Range(-shakeAmount, shakeAmount), 
                    Random.Range(-shakeAmount, shakeAmount), 
                    0) * Mathf.Sin(Time.time * shakeSpeed);
            }

            for (int j = 0; j < 4; j++)
            {
                Vector3 vert = vertices[vertexIndex + j] - offsetToMidBaseline;
                vert = matrix.MultiplyPoint3x4(vert);
                vertices[vertexIndex + j] = vert + offsetToMidBaseline + wavyOffset + shakeOffset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
