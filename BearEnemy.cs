using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(AudioSource))]
public class BearEnemyAI : MonoBehaviour, IResettable
{
    public enum AIState { Patrol, Chase, Attack }

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2.0f;
    public float pointArrivalDistance = 0.6f;
    public float rotationAlignSpeed = 5.0f;

    [Header("Detection Settings")]
    public float viewDistance = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public float proximityRadius = 2.5f; // Circle detection around the bear
    public LayerMask obstacleMask;       // Layers that block vision (e.g., Default, Environment)

    [Header("Aggro & Chase Settings")]
    public float chaseSpeed = 5.5f;
    public float loseAggroDistance = 15f; // Player must exceed this distance to lose aggro

    [Header("Attack Settings")]
    public float attackRange = 2.0f;
    public float attackCooldown = 2.0f;   // Time to wait between attacks (recharge)
    public BearWeapon weaponScript;       // Reference to the weapon script on the bear's hand

    [Header("Audio Settings")]
    public AudioClip[] randomIdleSounds;  // Sounds played randomly while patrolling/idle
    public float minIdleSoundInterval = 5f;
    public float maxIdleSoundInterval = 12f;
    public AudioClip spotPlayerSound;     // Sound played when the bear sees/detects the player
    public AudioClip attackSound;         // Sound played when the bear swings/attacks
    public AudioClip deathSound;          // Sound played when the bear dies

    [Header("Footstep Audio Settings")]
    public AudioClip[] footstepSounds;    // Array of footstep audio clips
    public float walkFootstepInterval = 0.5f; // Time between footsteps when walking
    public float runFootstepInterval = 0.3f;  // Time between footsteps when running

    [Header("Target")]
    public Transform player;

    private NavMeshAgent agent;
    private Animator anim;
    private AudioSource audioSource;

    private AIState currentState = AIState.Patrol;
    private int currentPointIndex = 0;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private bool isDead = false;
    private float nextIdleSoundTime = 0f;
    private float footstepTimer = 0f;
    private Vector3 initialScale;

    // --- SNAPSHOT STATE FOR RESPAWN ---
    private struct BearState
    {
        public bool isActive;
        public Vector3 position;
        public Quaternion rotation;
        public bool isDead;
    }

    public object SaveState()
    {
        return new BearState
        {
            isActive = gameObject.activeSelf,
            position = transform.position,
            rotation = transform.rotation,
            isDead = isDead
        };
    }

    public void LoadState(object state)
    {
        BearState s = (BearState)state;
        StopAllCoroutines(); // Stops any active sinking/shrinking
        isAttacking = false;

        gameObject.SetActive(s.isActive);
        isDead = s.isDead;
        
        // Reset Physics & Colliders first
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = true; }
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = true;

        // Reset Scale
        transform.localScale = (initialScale != Vector3.zero) ? initialScale : Vector3.one;

        if (agent != null)
        {
            agent.enabled = false; // Disable to allow manual positioning
            
            if (s.isActive && !isDead)
            {
                // SAFETY: Find the closest valid NavMesh point (avoids underground errors)
                if (NavMesh.SamplePosition(s.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    transform.rotation = s.rotation;
                    agent.enabled = true; // Re-enable once on valid floor
                    
                    // Only start AI if the agent successfully enabled
                    if (agent.isOnNavMesh)
                    {
                        agent.Warp(hit.position);
                        EnterState(AIState.Patrol);
                    }
                }
                else
                {
                    // If we absolutely can't find a NavMesh, keep it disabled to prevent crashing
                    transform.position = s.position;
                    Debug.LogWarning($"[BearAI] {gameObject.name} could not find NavMesh at {s.position}. AI Disabled.");
                }
            }
        }

        if (anim != null) { anim.Rebind(); anim.enabled = !isDead; anim.SetFloat("Speed", 0); }
}

    void Awake()
    {
        initialScale = transform.localScale;
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null && audioSource.outputAudioMixerGroup == null)
        {
            AudioMixer mixer = Resources.Load<AudioMixer>("MainMixer");
            if (mixer == null)
            {
                AudioMixer[] mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
                if (mixers.Length > 0) mixer = mixers[0];
            }
            if (mixer != null)
            {
                AudioMixerGroup[] groups = mixer.FindMatchingGroups("SFX");
                if (groups.Length > 0) audioSource.outputAudioMixerGroup = groups[0];
            }
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (agent.isActiveAndEnabled && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        ScheduleNextIdleSound();
        SetPatrolDestination();
    }

    void Update()
    {
        if (isDead || player == null || !agent.enabled) return;

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrolState();
                HandleRandomIdleAudio();
                break;
            case AIState.Chase:
                UpdateChaseState();
                break;
            case AIState.Attack:
                UpdateAttackState();
                break;
        }

        float currentSpeed = agent.velocity.magnitude;
        anim.SetFloat("Speed", currentSpeed);
        HandleFootstepAudio(currentSpeed);
    }

    void HandleFootstepAudio(float currentSpeed)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        if (currentSpeed > 0.2f && !isAttacking)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                float interval = (currentSpeed > (patrolSpeed + 0.5f)) ? runFootstepInterval : walkFootstepInterval;
                footstepTimer = interval;
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, 0.6f);
            }
        }
        else footstepTimer = 0f;
    }

    public void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            if (clip != null) audioSource.PlayOneShot(clip, 0.6f);
        }
    }

    void HandleRandomIdleAudio()
    {
        if (randomIdleSounds != null && randomIdleSounds.Length > 0)
        {
            if (Time.time >= nextIdleSoundTime)
            {
                if (audioSource != null && !audioSource.isPlaying)
                {
                    AudioClip randomClip = randomIdleSounds[Random.Range(0, randomIdleSounds.Length)];
                    if (randomClip != null) audioSource.PlayOneShot(randomClip);
                }
                ScheduleNextIdleSound();
            }
        }
    }

    void ScheduleNextIdleSound()
    {
        nextIdleSoundTime = Time.time + Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
    }

    void PlaySpotSound() { if (spotPlayerSound != null && audioSource != null) audioSource.PlayOneShot(spotPlayerSound); }
    void PlayAttackSound() { if (attackSound != null && audioSource != null) audioSource.PlayOneShot(attackSound); }
    void PlayDeathSound()
    {
        if (deathSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(deathSound);
            else SFXManager.PlaySFXAtPoint(deathSound, transform.position);
        }
    }

    void UpdatePatrolState()
    {
        if (CanDetectPlayer()) { EnterState(AIState.Chase); return; }
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        if (patrolPoints.Length == 1)
        {
            Transform targetPoint = patrolPoints[0];
            agent.SetDestination(targetPoint.position);
            if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
            {
                agent.isStopped = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetPoint.rotation, Time.deltaTime * rotationAlignSpeed);
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= pointArrivalDistance)
            {
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                SetPatrolDestination();
            }
        }
    }

    void SetPatrolDestination()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
    }

    bool CanDetectPlayer()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= proximityRadius) return true;
        if (distToPlayer <= viewDistance)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle <= viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distToPlayer, obstacleMask)) return true;
            }
        }
        return false;
    }

    void UpdateChaseState()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer > loseAggroDistance) { EnterState(AIState.Patrol); return; }
        if (distToPlayer <= attackRange) { EnterState(AIState.Attack); return; }
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);
    }

    void UpdateAttackState()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        FaceTarget(player.position);
        if (Time.time >= nextAttackTime && !isAttacking) StartCoroutine(ExecuteAttack());
        else if (!isAttacking && distToPlayer > attackRange) EnterState(AIState.Chase);
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        agent.isStopped = true;
        anim.SetTrigger("Attack");
        PlayAttackSound();
        nextAttackTime = Time.time + attackCooldown;
        if (weaponScript != null)
        {
            yield return new WaitForSeconds(0.4f);
            weaponScript.isAttacking = true;
            yield return new WaitForSeconds(0.3f);
            weaponScript.isAttacking = false;
        }
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        if (Vector3.Distance(transform.position, player.position) > attackRange) EnterState(AIState.Chase);
    }

    void EnterState(AIState newState)
    {
        if (currentState == AIState.Patrol && newState == AIState.Chase) PlaySpotSound();
        currentState = newState;
        if (newState == AIState.Patrol) { ScheduleNextIdleSound(); SetPatrolDestination(); }
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationAlignSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        if (other.CompareTag("Player"))
        {
            CrashPlayerController playerController = other.GetComponentInParent<CrashPlayerController>();
            if (playerController == null) return;
            if (playerController.IsSpinning || playerController.IsGroundPounding)
            {
                Break();
                if (!playerController.IsGroundPounding) playerController.Bounce(10f);
                return;
            }
            float playerBottom = playerController.transform.position.y;
            float bearTop = transform.position.y + 1.2f;
            if (playerController.Velocity.y < 0 && playerBottom > bearTop)
            {
                Break();
                playerController.ResetDoubleJump();
                playerController.Bounce(12f);
            }
        }
    }

    public void Break()
    {
        if (isDead) return;
        isDead = true;

        // 1. Disable navigation immediately
        if (agent != null) 
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 2. Disable Physics to prevent "infinite position" errors during shrinking
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) 
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 3. Disable all colliders so the bear sinks through the floor
        foreach (var col in GetComponentsInChildren<Collider>()) 
        {
            col.enabled = false;
        }

        PlayDeathSound();
        
        // 4. Trigger the death animation
        if (anim != null) anim.SetTrigger("Die");

        // 5. Start the smooth shrink and sink transition
        StartCoroutine(DeactivateAfterDelay(3.5f));
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, proximityRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, loseAggroDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}