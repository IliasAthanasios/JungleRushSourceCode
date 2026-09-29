using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    public int requiredButtons = 3;
    private int activatedCount = 0;

    public GameObject laserObject;
    public AudioSource audioSource; // Used for the one-time activation sound
    public AudioClip activationSound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (laserObject != null) laserObject.SetActive(false);
    }

    public void OnButtonActivated()
    {
        activatedCount++;
        
        if (activatedCount >= requiredButtons)
        {
            ActivateLaser();
        }
    }

    public void ResetSystem()
    {
    activatedCount = 0;
    if (laserObject != null)
    {
        laserObject.SetActive(false);
        AudioSource laserLoop = laserObject.GetComponent<AudioSource>();
        if (laserLoop != null) laserLoop.Stop();
    }
    
    // Find all buttons in the scene and reset them
    BossButton[] buttons = Object.FindObjectsByType<BossButton>(FindObjectsSortMode.None);
    foreach(var button in buttons) button.ResetButton();
    }

    void ActivateLaser()
    {
        if (laserObject != null)
        {
            laserObject.SetActive(true);
            
            // 1. Play the one-time activation sound
            if (audioSource != null && activationSound != null)
            {
                audioSource.PlayOneShot(activationSound);
            }

            // 2. Start the constant looping sound on the laser itself
            AudioSource laserLoop = laserObject.GetComponent<AudioSource>();
            if (laserLoop != null)
            {
                laserLoop.Play();
            }

            Debug.Log("Laser and sounds activated!");
        }
    }
}