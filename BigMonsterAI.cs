using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using System.Collections;

public class BigMonsterAI : MonoBehaviour
{
    [Header("Movement")]
    public float chaseSpeed = 7f;
    public float attackRange = 4.5f;
    public float attackCooldown = 1.5f;

    [Header("Combat")]
    public Collider handCollider; 
    public GameObject hitBossEffect;
    public GameObject hitPlayerEffect;
    public AudioClip damagePlayerSound;

    [Header("Audio")]
    public AudioMixerGroup sfxGroup; 
    public AudioClip crySound;
    public AudioClip stunSound;
    public AudioClip attackSound;
    public AudioClip[] footstepSounds;
    public AudioClip throwSound;
    public AudioClip deathRoar;

    [Header("Phase 3")]
    public GameObject projectilePrefab;
    public Transform throwPoint;
    public float zoneRadius = 7.5f;

    [Header("Health")]
    public float maxHealth = 2f;
    private float currentHealth;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private AudioSource audioSource;
    private bool isStunned = false;
    private bool isAttacking = false;
    private bool inPhase3 = false;
    private bool hasLanded = false;
    private bool isDead = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup;

        if (handCollider != null) handCollider.enabled = false;
        agent.enabled = false;
    }

    public void StartPhaseIntro(Vector3 jumpStartPosition, Vector3 landPosition)
    {
        currentHealth = maxHealth;
        if (BossHealthUI.Instance != null) BossHealthUI.Instance.Setup(maxHealth);

        transform.position = jumpStartPosition;
        gameObject.SetActive(true);
        StartCoroutine(JumpDownRoutine(landPosition));
        
    }

    IEnumerator JumpDownRoutine(Vector3 landPosition)
    {
        float elapsed = 0;
        float duration = 2.5f;
        Vector3 startPos = transform.position;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 pos = Vector3.Lerp(startPos, landPosition, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 15f; 
            transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = landPosition;
        yield return new WaitForSeconds(2.3f);
        hasLanded = true;
        agent.enabled = true;
        agent.speed = chaseSpeed;
        if (anim != null) anim.SetTrigger("Attack"); 
        if (attackSound != null) audioSource.PlayOneShot(attackSound);
    }

    void Update()
    {
        if (isDead || !hasLanded || isStunned || isAttacking) return;

        Vector3 centerPoint = new Vector3(17.5f, transform.position.y, 67.5f);
        float playerDistToCenter = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(centerPoint.x, centerPoint.z));

        if (inPhase3)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);

            if (playerDistToCenter < zoneRadius)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
                if (Vector3.Distance(transform.position, player.position) <= attackRange)
                    StartCoroutine(AttackRoutine());
            }
            else
            {
                if (Vector3.Distance(transform.position, centerPoint) > 1.5f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(centerPoint);
                }
                else
                {
                    agent.isStopped = true;
                    Vector3 lookPos = player.position;
                    lookPos.y = transform.position.y;
                    transform.LookAt(lookPos);
                }
            }
            return;
        }

        float d = Vector3.Distance(transform.position, player.position);
        if (d <= attackRange) StartCoroutine(AttackRoutine());
        else if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    // Satisfaction for CrashPlayerController.cs
    public void OnHit()
    {
        if (isDead || !isStunned) return;

        // Optional: If the player hits the boss while stunned, we could do damage here too
        if (hitBossEffect != null) Instantiate(hitBossEffect, transform.position + Vector3.up, Quaternion.identity);
        if (crySound != null) audioSource.PlayOneShot(crySound);
    }

    public void EnableHandDamage() { if (handCollider != null) handCollider.enabled = true; }
    public void DisableHandDamage() { if (handCollider != null) handCollider.enabled = false; }

    public void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && audioSource != null)
            audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.7f);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        if (agent.isOnNavMesh) agent.isStopped = true;
        anim.SetFloat("Speed", 0);
        anim.SetTrigger("Attack");
        if (attackSound != null) audioSource.PlayOneShot(attackSound);
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    public void Stun()
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine());
        if (!inPhase3) BossPhaseManager.Instance.TransitionToPhase3();
    }

    IEnumerator StunRoutine()
    {
        isStunned = true;
        if (agent.isOnNavMesh) agent.isStopped = true;
        anim.SetTrigger("Stun");
        if (stunSound != null) audioSource.PlayOneShot(stunSound);
        yield return new WaitForSeconds(5f);
        isStunned = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Player") && handCollider != null && handCollider.enabled)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
                if (hitPlayerEffect != null) Instantiate(hitPlayerEffect, other.transform.position + Vector3.up, Quaternion.identity);
                if (damagePlayerSound != null) audioSource.PlayOneShot(damagePlayerSound, 0.8f);
            }
            DisableHandDamage();
        }

        if (other.CompareTag("Laser"))
        {
            currentHealth -= 1f;
            if (BossHealthUI.Instance != null) BossHealthUI.Instance.UpdateHealth(currentHealth);

            if (currentHealth <= 0) BossPhaseManager.Instance.Victory();
            else if (!inPhase3) Stun();
        }
    }

    public void EnterPhase3()
    {
        if (inPhase3) return;
        inPhase3 = true;
        StartCoroutine(ProjectileRoutine());
    }

    IEnumerator ProjectileRoutine()
    {
        while (inPhase3 && !isDead)
        {
            yield return new WaitForSeconds(Random.Range(3f, 5f));
            Vector3 center = new Vector3(17.5f, 0, 67.5f);
            float playerDistToCenter = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(center.x, center.z));

            if (playerDistToCenter > zoneRadius && !isStunned && !isAttacking)
            {
                anim.SetTrigger("Attack");
                for (int i = 0; i < 3; i++)
                {
                    ThrowProjectile();
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }
    }

    void ThrowProjectile()
    {
        if (projectilePrefab != null && throwPoint != null)
        {
            if (audioSource != null && throwSound != null) audioSource.PlayOneShot(throwSound, 0.6f);
            GameObject projObj = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);
            MonsterProjectile projScript = projObj.GetComponent<MonsterProjectile>();
            if (projScript != null)
            {
                Vector3 targetPos = player.position + Vector3.up * 1.5f;
                Vector3 direction = (targetPos - throwPoint.position).normalized;
                projScript.Launch(direction, gameObject);
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        inPhase3 = false;
        StopAllCoroutines();

        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
        if (audioSource != null && deathRoar != null) audioSource.PlayOneShot(deathRoar, 1.0f);
        
        transform.position = new Vector3(17.5f, 0.25f, 67.5f);
        if (anim != null) 
        {
            anim.SetTrigger("Die");
            StartCoroutine(FreezeAnimator());
        }
    }

    private IEnumerator FreezeAnimator()
    {
        yield return new WaitForSeconds(2.0f);
        anim.speed = 0;
    }

    void OnDisable()
    {
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = true;
        if (anim != null) anim.SetFloat("Speed", 0);
    }

    void OnEnable()
    {
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = false;
    }
}