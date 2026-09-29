using UnityEngine;

public class BossButton : MonoBehaviour
{
    private bool isPressed = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    [Header("Visual Feedback")]
    public Transform pressedArea;
    public Renderer buttonRenderer;
    public Color activatedColor = Color.green;
    public Vector3 pressedOffset = new Vector3(0, -0.3f, 0); 

    private Vector3 originalLocalPos;
    private Color originalColor;


    void Awake()
    {
        if (pressedArea == null) pressedArea = transform.Find("Button_Object");

        if (pressedArea != null)
        {
            originalLocalPos = pressedArea.localPosition;
            if (buttonRenderer == null) buttonRenderer = pressedArea.GetComponent<Renderer>();
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (buttonRenderer != null)
        originalColor = buttonRenderer.sharedMaterial.color;
    }

    public void OnPlayerHit()
    {
        if (isPressed) return;
        isPressed = true;

        if (pressedArea != null)
        {
            pressedArea.localPosition = originalLocalPos + pressedOffset;
        }

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (buttonRenderer != null)
        {
            var mats = Application.isPlaying ? buttonRenderer.materials : buttonRenderer.sharedMaterials;
            foreach (var mat in mats)
            {
                mat.color = activatedColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", activatedColor * 2f);
            }
        }

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.OnButtonActivated();
        }
    }
    public void ResetButton()
    {
    isPressed = false;
    if (pressedArea != null) pressedArea.localPosition = originalLocalPos;
    
    if (buttonRenderer != null)
        {
            var mats = Application.isPlaying ? buttonRenderer.materials : buttonRenderer.sharedMaterials;
            foreach (var mat in mats)
            {
                mat.color = originalColor;
                mat.DisableKeyword("_EMISSION");
            }
        }
    }
}
