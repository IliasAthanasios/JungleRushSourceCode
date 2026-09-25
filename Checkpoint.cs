using UnityEngine;
using UnityEngine.Audio;
using TMPro; // Added for TextMeshPro support

public class Checkpoint : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public ParticleSystem particles;
    public AudioClip activateSound;
    public TextMeshPro checkpointLabel; // Drag your Text object here

    [Tooltip("Drag an AudioClip OR an Audio Random Container here")]
    public Object loopSound;

    [Header("Spawn Settings")]
    public Transform customSpawnPoint;
    public float spawnOffsetForward = 1.5f;

    [Header("UI Settings")]
    public string reachedText = "REACHED!";
    public Color reachedColor = Color.green;

    [Header("Settings")]
    public string openTrigger = "open";
    [Range(0, 1)] public float loopVolume = 0.5f;
    
    private bool isActive = false;
    private AudioSource loopSource;

    private void Start()
    {
        if (particles != null) particles.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive && other.CompareTag("Player"))
        {
            Activate();
        }
    }

    private void Activate()
    {
        isActive = true;
        
        // 1. Update the Text
        if (checkpointLabel != null)
        {
            checkpointLabel.text = reachedText;
            checkpointLabel.color = reachedColor;
        }

        if (GameManager.Instance != null)
        {
            Vector3 respawnPos = customSpawnPoint != null ? customSpawnPoint.position : transform.position + (transform.forward * spawnOffsetForward) + Vector3.up * 0.1f;
            GameManager.Instance.SetCheckpoint(respawnPos);
        }
        
        if (animator != null) animator.SetTrigger(openTrigger);
        if (particles != null) particles.Play();

        if (activateSound != null)
        {
            SFXManager.PlaySFXAtPoint(activateSound, transform.position);
        }

        if (loopSound != null)
        {
            StartLoopingSound();
        }
    }

    private void StartLoopingSound()
    {
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.volume = loopVolume;
        loopSource.spatialBlend = 1.0f; 
        loopSource.minDistance = 2f;
        loopSource.maxDistance = 15f;
        loopSource.rolloffMode = AudioRolloffMode.Linear;

        if (loopSound is IAudioGenerator container)
        {
            loopSource.generator = container;
        }
        else if (loopSound is AudioClip clip)
        {
            loopSource.clip = clip;
            loopSource.loop = true;
        }

        loopSource.Play();
    }
}