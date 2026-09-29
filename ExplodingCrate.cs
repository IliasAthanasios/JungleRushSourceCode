using UnityEngine;
using System.Collections;

public class ExplodingCrate : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionEffect;
    public AudioClip explosionSound;
    public float explosionRadius = 3f;
    public float explosionForce = 500f;

    [Header("Wobble Settings")]
    public float wobbleIntensity = 0.05f;
    public float wobbleDuration = 1.0f;
    public float pauseDuration = 2.0f;
    
    [Header("Audio Settings")]
    public AudioClip fuseSound;
    public float fuseInterval = 8.0f; // Set to 8 seconds as requested
    public float soundMaxDistance = 15f;
    [Range(0, 256)] public int soundPriority = 80;

    private bool hasExploded = false;
    private Vector3 originalPosition;
    private AudioSource audioSource;

    void Start()
    {
        originalPosition = transform.position;
        
        // Setup AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.priority = soundPriority;

        // Start both independent routines
        StartCoroutine(WobbleRoutine());
        StartCoroutine(FuseSoundRoutine());
    }

    // Handles ONLY the visual shaking
    private IEnumerator WobbleRoutine()
    {
        while (!hasExploded)
        {
            yield return new WaitForSeconds(pauseDuration);
            
            if (hasExploded) break;

            float elapsed = 0;
            while (elapsed < wobbleDuration)
            {
                if (hasExploded) break;
                // Wobble around the original spot
                transform.position = originalPosition + Random.insideUnitSphere * wobbleIntensity;
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = originalPosition;
        }
    }

    // Handles ONLY the ticking/fuse sound
    private IEnumerator FuseSoundRoutine()
    {
        while (!hasExploded)
        {
            if (fuseSound != null)
            {
                audioSource.PlayOneShot(fuseSound);
            }

            // Wait exactly 8 seconds before the next tick
            yield return new WaitForSeconds(fuseInterval);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasExploded && other.CompareTag("Player"))
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        StopAllCoroutines(); // Stop both the wobble and the fuse sound

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        if (explosionSound != null)
        {
            SFXManager.PlaySFXAtPoint(explosionSound, transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoseLife();
        }

        CrashPlayerController player = FindFirstObjectByType<CrashPlayerController>();
        if (player != null)
        {
            player.Bounce(15f);
        }

        Destroy(gameObject);
    }
}