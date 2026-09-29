using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using System.Collections;

public class SmallMonsterAI : MonoBehaviour
{
    [Header("Combat")]
    public float maxHealth = 3f;
    public float currentHealth;
    public float chargeSpeed = 16f;
    public float tiredTime = 3.5f;
    public float chargeDuration = 5f;
    
    [Header("Audio")]
    public AudioMixerGroup sfxGroup; 
    public AudioClip crySound;
    public AudioClip tiredSound;
    public AudioClip hitPlayerSound;
    public AudioClip chargeSound;
    public AudioClip[] footstepSounds;

    [Header("Particles")]
    public GameObject hitBossEffect;
    public GameObject hitPlayerEffect;
    
    [Header("Vulnerability Visuals")]
    public Renderer bossRenderer;
    public Color vulnerableColor = Color.yellow;
    public GameObject stunEffect; 


    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private AudioSource audioSource;
    private bool isInvulnerable = true;
    private bool fightStarted = false;
    private float originalAngularSpeed;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        originalAngularSpeed = agent.angularSpeed;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup;

        if (agent.isOnNavMesh) agent.isStopped = true;
        anim.SetFloat("Speed", 0f);

        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void TriggerStart()
    {
        if (!fightStarted)
        {
            fightStarted = true;
            Debug.Log("Boss Fight Started!");
            StartCoroutine(BehaviorLoop());
        }
    }

    void Update()
    {
        if (fightStarted && currentHealth > 0)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    public void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.4f);
        }
    }

    IEnumerator BehaviorLoop()
    {
        while (currentHealth > 0)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.angularSpeed = 120;
            
            float rotateTimer = 0;
            while (rotateTimer < 1.0f) 
            {
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
                }
                rotateTimer += Time.deltaTime;
                yield return null;
            }

            if (chargeSound != null) audioSource.PlayOneShot(chargeSound);
            yield return new WaitForSeconds(0.3f);

            isInvulnerable = true;
            if (agent.isOnNavMesh) agent.isStopped = false;
            agent.speed = chargeSpeed;
            agent.angularSpeed = 0; 

            Vector3 targetPosition = transform.position + transform.forward * 50f;
            agent.SetDestination(targetPosition);

            yield return new WaitForSeconds(chargeDuration);

            isInvulnerable = false;
            if (agent.isOnNavMesh) agent.isStopped = true;
            anim.SetTrigger("Tired");
            if (bossRenderer != null) bossRenderer.material.color = vulnerableColor;
            if (stunEffect != null) stunEffect.SetActive(true);
            if (tiredSound != null) audioSource.PlayOneShot(tiredSound);
            
            yield return new WaitForSeconds(tiredTime);
            if (bossRenderer != null) bossRenderer.material.color = Color.white;
            if (stunEffect != null) stunEffect.SetActive(false);
        }
    }

    public void OnHit()
    {
        if (isInvulnerable || !fightStarted || currentHealth <= 0) return;

        currentHealth--;
        if (BossHealthUI.Instance != null) BossHealthUI.Instance.UpdateHealth(currentHealth);
        if (hitBossEffect != null) Instantiate(hitBossEffect, transform.position + Vector3.up, Quaternion.identity);
        if (crySound != null) audioSource.PlayOneShot(crySound);
        
        if (currentHealth <= 0) StartCoroutine(DieAndTransition());
    }

    IEnumerator DieAndTransition()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        fightStarted = false;
        anim.SetTrigger("Die");
        yield return new WaitForSeconds(3.0f);
        BossPhaseManager.Instance.TransitionToPhase2();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fightStarted && isInvulnerable && other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
                if (hitPlayerEffect != null) Instantiate(hitPlayerEffect, other.transform.position + Vector3.up, Quaternion.identity);
                if (hitPlayerSound != null) audioSource.PlayOneShot(hitPlayerSound);
            }
        }
    }
}
