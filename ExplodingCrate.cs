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
    public float fuseInterval = 8.0f; 
    public float soundMaxDistance = 15f;
    [Range(0, 256)] public int soundPriority = 80;

    private bool hasExploded = false;
    private Vector3 originalPosition;
    private AudioSource audioSource;

    void Start()
    {
        originalPosition = transform.position;
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.priority = soundPriority;

        StartCoroutine(WobbleRoutine());
        StartCoroutine(FuseSoundRoutine());
    }

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
                transform.position = originalPosition + Random.insideUnitSphere * wobbleIntensity;
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.position = originalPosition;
        }
    }

    private IEnumerator FuseSoundRoutine()
    {
        while (!hasExploded)
        {
            if (fuseSound != null)
            {
                audioSource.PlayOneShot(fuseSound);
            }

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
        
        StopAllCoroutines(); 

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
