using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class TreeMonsterAI : MonoBehaviour, IResettable
{
    [Header("Stats")]
    public float attackRange = 12f;
    public float fireRate = 2.5f;
    public float rotationSpeed = 5f;
    public float bounceForce = 12f;

    [Header("References")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public GameObject deathEffect;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource idleSource;

    [Header("Audio Containers")]
    public Object attackRandomContainer; 
    public Object idleRandomContainer; 

    [Header("Clips")]
    public AudioClip deathSound;
    public AudioClip spottedSound;

    public float minIdleInterval = 5f;
    public float maxIdleInterval = 12f;

    private Animator animator;
    private Transform player;
    private float nextFireTime;
    private float nextIdleSoundTime;
    private bool isDead = false;
    private bool hasSpottedPlayer = false;
    private Vector3 initialScale;

    // --- SNAPSHOT STATE FOR RESPAWN ---
    private struct TreeState
    {
        public bool isActive;
        public bool isDead;
        public bool hasSpotted;
        public Vector3 position;      
        public Quaternion rotation;
    }

    public object SaveState()
    {
        return new TreeState
        {
            isActive = gameObject.activeSelf,
            isDead = isDead,
            hasSpotted = hasSpottedPlayer,
            position = transform.position,
            rotation = transform.rotation
        };
    }

    public void LoadState(object state)
    {
        TreeState s = (TreeState)state;
        StopAllCoroutines();
        
        gameObject.SetActive(s.isActive);
        isDead = s.isDead;
        hasSpottedPlayer = s.hasSpotted;

        transform.position = s.position;
        transform.rotation = s.rotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        {
        rb.isKinematic=isDead;
        rb.useGravity=!isDead;
        }

        foreach (var col in GetComponentsInChildren<Collider>()) 
        {
        col.enabled = true;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.enabled = !isDead;
        }
        if (initialScale != Vector3.zero) 
        transform.localScale = initialScale; 
        else 
        transform.localScale = Vector3.one; // Fallback         transform.localScale = (initialScale != Vector3.zero) ? initialScale : new Vector3(3, 3, 3);
    }

    void Awake()
    {
        initialScale = transform.localScale;
        animator = GetComponent<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        gameObject.tag = "Breakable";
        nextIdleSoundTime = Time.time + Random.Range(minIdleInterval, maxIdleInterval);
    }

    void Update()
    {
        if (isDead || player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (!hasSpottedPlayer)
            {
                hasSpottedPlayer = true;
                if (sfxSource && spottedSound) sfxSource.PlayOneShot(spottedSound);
            }

            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * rotationSpeed);

            if (Time.time >= nextFireTime)
            {
                animator.SetTrigger("attack");
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            hasSpottedPlayer = false;
            HandleRandomIdleAudio();
        }
    }

    void HandleRandomIdleAudio()
    {
        if (idleRandomContainer != null && Time.time >= nextIdleSoundTime)
        {
            if (idleSource != null && !idleSource.isPlaying)
            {
                idleSource.generator = idleRandomContainer as IAudioGenerator;
                idleSource.Play();
                nextIdleSoundTime = Time.time + Random.Range(minIdleInterval, maxIdleInterval);
            }
        }
    }

    public void Shoot()
    {
        if (isDead || projectilePrefab == null || shootPoint == null) return;

        GameObject projObj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        MonsterProjectile projScript = projObj.GetComponent<MonsterProjectile>();
        
        if (projScript != null)
        {
            Vector3 aimDir = (player.position + Vector3.up * 1f - shootPoint.position).normalized;
            projScript.Launch(aimDir, gameObject); 
        }

        if (sfxSource != null && attackRandomContainer != null)
        {
            sfxSource.generator = attackRandomContainer as IAudioGenerator;
            sfxSource.Play();
        }
    }

    public void Break()
    {
        if (isDead) return;
        isDead = true;

        // 1. Stop audio and particles
        if (idleSource != null) idleSource.Stop();
        if (deathEffect != null) Instantiate(deathEffect, transform.position + Vector3.up, Quaternion.identity);

        // 2. Disable Physics and Collisions
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in GetComponentsInChildren<Collider>()) 
        {
            col.enabled = false;
        }

        if (sfxSource && deathSound) sfxSource.PlayOneShot(deathSound);

        // 3. Trigger the death animation (note the trigger name is "dead")
        if (animator != null) animator.SetTrigger("dead");
        
        // 4. Start the smooth shrink and sink transition
        StartCoroutine(DeactivateAfterDelay(2.5f));
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        // Wait for most of the death animation to finish
        yield return new WaitForSeconds(delay * 0.5f);

        float elapsed = 0f;
        float duration = delay * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Shrink the monster
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            // Sink it into the ground
            transform.position += Vector3.down * Time.deltaTime * 0.5f;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}