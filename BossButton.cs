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
    public Vector3 pressedOffset = new Vector3(0, -0.3f, 0); // Moves slightly "inwards"

    private Vector3 originalLocalPos;
    private Color originalColor;


    void Awake()
    {
        // Auto-find the child object if not assigned
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

        // 1. Manual Animation: Move the child object inwards
        if (pressedArea != null)
        {
            pressedArea.localPosition = originalLocalPos + pressedOffset;
        }

        // 2. Play Hit Sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // 3. Visual Feedback: Turn Green and Glow
        if (buttonRenderer != null)
        {
            // Use sharedMaterials if in Editor, materials if in Play Mode
            var mats = Application.isPlaying ? buttonRenderer.materials : buttonRenderer.sharedMaterials;
            foreach (var mat in mats)
            {
                mat.color = activatedColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", activatedColor * 2f);
            }
        }

        // 4. Notify Arena Manager
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