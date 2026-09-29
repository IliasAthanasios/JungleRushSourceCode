using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    public static ArenaManager Instance { get; private set; }

    public int requiredButtons = 3;
    private int activatedCount = 0;

    public GameObject laserObject;
    public AudioSource audioSource; 
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
    
    BossButton[] buttons = Object.FindObjectsByType<BossButton>(FindObjectsSortMode.None);
    foreach(var button in buttons) button.ResetButton();
    }

    void ActivateLaser()
    {
        if (laserObject != null)
        {
            laserObject.SetActive(true);
            
            if (audioSource != null && activationSound != null)
            {
                audioSource.PlayOneShot(activationSound);
            }

            AudioSource laserLoop = laserObject.GetComponent<AudioSource>();
            if (laserLoop != null)
            {
                laserLoop.Play();
            }

            Debug.Log("Laser and sounds activated!");
        }
    }
}
