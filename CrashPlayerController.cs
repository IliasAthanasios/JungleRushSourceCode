using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

[RequireComponent(typeof(CharacterController))]
public class CrashPlayerController : MonoBehaviour
{
    [Header("Audio Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 720f;
    public float gravity = -20f;
    public float jumpHeight = 2.5f;
    public float doubleJumpHeight = 1.8f;

    [Header("Platforming Feel")]
    public float coyoteTime = 0.15f; 
    private float coyoteTimeCounter;

    [Header("Attack")]
    public float spinDuration = 0.5f;
    public float spinRadius = 1.0f; 
    public float spinRotationSpeed = 1500f; 
    public GameObject spinEffect;

    [Header("Abilities")]
    public float groundPoundForce = 30f;
    public float crouchSpeedMultiplier = 0.5f;
    public float groundPoundRecoveryTime = 0.5f;
    public float groundPoundDelay = 0.1f;

    [Header("Physics")]
    public float slideSpeed = 10f;
    public float stickToGroundForce = 10f; 
    public float groundSnapDistance = 0.2f; 

    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip spinSound;
    public AudioClip bgmMusic;

    [Header("Slam Effects")]
    public GameObject slamEffectPrefab;
    public AudioClip slamSound;
    
    [Header("Footstep Sources")]
    public AudioSource runFootstepSource;
    public AudioSource sneakFootstepSource;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 hitNormal;
    private Vector3 groundNormal = Vector3.up; 
    private bool isGrounded;
    private bool canDoubleJump;
    private bool isGroundPounding;
    private float groundPoundDelayTimer;
    private bool isRecovering;
    private float recoveryTimer;
    private Vector2 moveInput;
    private bool isSpinning;
    private float spinTimer;
    private float idleTimer;
    private Transform modelTransform;
    private Vector3 originalModelScale;
    private AudioSource sfxSource;
    private AudioSource bgmSource;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction crouchAction;
    private Animator animator;

    public bool IsGroundPounding => isGroundPounding;
    public bool IsSpinning => isSpinning;
    public bool IsRecovering => isRecovering;
    public Vector3 Velocity => velocity;

    private Transform slamTrigger;
    private AudioSource footstepSource;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        hitNormal = Vector3.up;
        RefreshModelReferences();
        
        footstepSource = null;
        foreach(var source in GetComponentsInChildren<AudioSource>(true))
        {
            if(source.gameObject.name == "FootstepSource")
            {
                footstepSource = source;
                break;
            }
        }
        
        slamTrigger = transform.Find("SlamTrigger");
        if (slamTrigger != null) slamTrigger.gameObject.SetActive(false);

        sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
        if (musicGroup != null) bgmSource.outputAudioMixerGroup = musicGroup;
        if (footstepSource != null && sfxGroup != null) 
            footstepSource.outputAudioMixerGroup = sfxGroup;

        if (bgmMusic != null)
        {
            bgmSource.clip = bgmMusic;
            bgmSource.loop = true;
            bgmSource.volume = 0.5f;
            bgmSource.mute = false; 
            bgmSource.Play();
        }
        
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        attackAction = InputSystem.actions.FindAction("Attack");
        crouchAction = InputSystem.actions.FindAction("Crouch");
    }

    public void RefreshModelReferences()
    {
        FindAnimator();

        modelTransform = null;
        foreach (Transform child in transform)
        {
            if (child.name != "SpinEffect" && child.gameObject.activeSelf)
            {
                modelTransform = child;
                originalModelScale = modelTransform.localScale;
                break;
            }
        }
        
        if (modelTransform == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name != "SpinEffect")
                {
                    modelTransform = child;
                    originalModelScale = modelTransform.localScale;
                    break;
                }
            }
        }
    }

    public void Bounce(float force)
    {
        velocity.y = force;
        isGroundPounding = false;
        
        if (animator != null) animator.SetTrigger("JumpTrigger");
        
        if (jumpSound != null) 
        {
            sfxSource.Stop();
            sfxSource.clip = jumpSound;
            sfxSource.loop = false;
            sfxSource.Play();
        }
    }

    public void ResetDoubleJump()
    {
        canDoubleJump = true;
    }

    void FindAnimator()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator not found on children of Crash!");
        }
    }

    public void ResetState()
    {
        isGroundPounding = false;
        isRecovering = false;
        isSpinning = false;
        velocity = Vector3.zero;
        if (slamTrigger != null) slamTrigger.gameObject.SetActive(false);
        if (spinEffect != null) spinEffect.SetActive(false);
        if (animator != null)
        {
            animator.SetBool("IsGroundPounding", false);
            animator.SetBool("IsRecovering", false);
            animator.SetFloat("Speed", 0);
        }
    }

    void Update()
    {
        if (animator == null) FindAnimator();

        if (isGroundPounding && groundPoundDelayTimer > 0)
        {
            groundPoundDelayTimer -= Time.deltaTime;
        }

        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        bool isOnSteepSlope = controller.isGrounded && slopeAngle > controller.slopeLimit;
        
        if (!controller.isGrounded)
        {
            groundNormal = Vector3.up;
        }

        isGrounded = controller.isGrounded && !isOnSteepSlope;
        
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime; 
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime; 
        }

        if (isGrounded)
        {
            canDoubleJump = true;
            
            if (isGroundPounding)
            {
                if (groundPoundDelayTimer <= 0)
                {
                    isGroundPounding = false;
                    if (slamTrigger != null) slamTrigger.gameObject.SetActive(false);
                    
                    var impulse = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
                    if (impulse != null)
                    {
                        impulse.GenerateImpulse();
                    }

                    if (slamEffectPrefab != null)
                    {
                        Instantiate(slamEffectPrefab, transform.position, Quaternion.identity);
                    }

                    if (slamSound != null && sfxSource != null)
                    {
                        sfxSource.PlayOneShot(slamSound);
                    }

                    isRecovering = true;
                    recoveryTimer = groundPoundRecoveryTime;
                    velocity = Vector3.zero;
                }
            }
        }

        if (isRecovering)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                isRecovering = false;
            }
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        bool isGameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        bool canAction = !isGameOver && !isRecovering && !isOnSteepSlope && Time.timeScale > 0;

        if (canAction)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }
        else
        {
            moveInput = Vector2.zero;
        }

        float targetSpeed = moveSpeed;
        bool isCrouching = isGrounded && canAction && crouchAction.IsPressed();
        if (isCrouching)
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        if (canAction)
        {
            if (!isGrounded && crouchAction.WasPressedThisFrame() && !isGroundPounding)
            {
                isGroundPounding = true;
                groundPoundDelayTimer = groundPoundDelay;
                if (slamTrigger != null) slamTrigger.gameObject.SetActive(true);
                velocity = Vector3.zero; 
            }

            if (attackAction.WasPressedThisFrame() && !isSpinning)
            {
                StartSpin();
            }

            if (jumpAction.WasPressedThisFrame() && !isGroundPounding)
            {
                if (coyoteTimeCounter > 0f)
                {
                    PerformJump(jumpHeight);
                    coyoteTimeCounter = 0f; 
                    if (animator != null) animator.SetTrigger("JumpTrigger");
                }
                else if (canDoubleJump)
                {
                    PerformJump(doubleJumpHeight);
                    canDoubleJump = false;
                    if (animator != null) animator.SetTrigger("DoubleJump");
                }
            }
        }

        if (isSpinning)
        {
            spinTimer -= Time.deltaTime;
            CheckForCrates(); 
            if (spinTimer <= 0)
            {
                StopSpin();
            }
        }

        // --- MOVEMENT CALCULATION ---
        Vector3 move = Vector3.zero;
        if (!isGameOver && !isGroundPounding && !isRecovering)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            move = (camForward * moveInput.y + camRight * moveInput.x);
        }

        if (isGrounded)
        {
            Vector3 projectedMove = Vector3.ProjectOnPlane(move, groundNormal).normalized * move.magnitude;
            if (projectedMove.y < 0)
            {
                move = projectedMove;
            }
        }
        
        float actualHorizontalSpeed = move.magnitude * targetSpeed;

        // --- IDLE TIMER LOGIC ---
        if (actualHorizontalSpeed < 0.1f && isGrounded)
        {
            idleTimer += Time.deltaTime;
        }
        else
        {
            idleTimer = 0f;
        }

        if (move.magnitude > 0.1f && !isGroundPounding && !isRecovering)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (!isGroundPounding)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (groundPoundDelayTimer <= 0)
        {
            velocity.y = -groundPoundForce;
        }
        else
        {
            velocity.y = 0;
        }

        bool wasGrounded = isGrounded;

        if (wasGrounded && !controller.isGrounded && velocity.y <= 0)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundSnapDistance))
            {
                controller.Move(new Vector3(0, -hit.distance, 0));
                velocity.y = -2f; 
            }
        }

        Vector3 slopeSlide = Vector3.zero;
        if (isOnSteepSlope)
        {
            slopeSlide = new Vector3(groundNormal.x, 0, groundNormal.z).normalized * slideSpeed;
            float dot = Vector3.Dot(move, groundNormal);
            if (dot < 0)
            {
                Vector3 tangent = Vector3.Cross(groundNormal, Vector3.up).normalized;
                move = Vector3.Dot(move, tangent) * tangent;
            }
        }

        Vector3 finalMove = (move * targetSpeed + slopeSlide + new Vector3(0, velocity.y, 0)) * Time.deltaTime;
        CollisionFlags flags = controller.Move(finalMove);

        bool isWalkingOnValidSlope = ((flags & CollisionFlags.Below) != 0 || controller.isGrounded) && (Vector3.Angle(Vector3.up, groundNormal) <= controller.slopeLimit);
        isGrounded = isWalkingOnValidSlope;

        if (!isGrounded)
        {
            groundNormal = Vector3.up;
        }
        
        if (!isGrounded && isGroundPounding && groundPoundDelayTimer <= 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.3f))
            {
                if (!hit.collider.CompareTag("Breakable"))
                {
                    isGrounded = true;
                }
            }
        }

        // --- FINAL ANIMATOR UPDATES ---
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("Speed", actualHorizontalSpeed); 
            animator.SetBool("IsCrouching", isCrouching);
            animator.SetFloat("VerticalVelocity", velocity.y);
            animator.SetBool("IsGroundPounding", isGroundPounding);
            animator.SetBool("IsRecovering", isRecovering);
            animator.SetFloat("IdleTime", idleTimer);
        }
    } 

    void PerformJump(float height)
    {
        velocity.y = Mathf.Sqrt(height * -2f * gravity);
        if (jumpSound != null) 
        {
            sfxSource.Stop();
            sfxSource.clip = jumpSound;
            sfxSource.loop = false;
            sfxSource.Play();
        }
    }

    void StartSpin()
    {
        isSpinning = true;
        spinTimer = spinDuration;
        if (spinEffect != null) spinEffect.SetActive(true);
        if (animator != null) animator.SetTrigger("Spin");
        
        if (spinSound != null)
        {
            sfxSource.clip = spinSound;
            sfxSource.loop = true; 
            sfxSource.Play();
        }
    }

    void CheckForCrates()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, spinRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Breakable"))
            {
                BreakableCrate crate = hitCollider.GetComponentInParent<BreakableCrate>();
                if (crate != null) crate.Break();
                
                SmallMonsterAI smallBoss = hitCollider.GetComponentInParent<SmallMonsterAI>();
                if (smallBoss != null) smallBoss.OnHit();

                BigMonsterAI bigBoss = hitCollider.GetComponentInParent<BigMonsterAI>();
                if (bigBoss != null) bigBoss.OnHit();
                
                BearEnemyAI bear = hitCollider.GetComponentInParent<BearEnemyAI>();
                if (bear != null) bear.Break();

                TreeMonsterAI tree = hitCollider.GetComponentInParent<TreeMonsterAI>();
                if (tree != null) tree.Break();

                BossButton button = hitCollider.GetComponentInParent<BossButton>();
                if (button != null) button.OnPlayerHit();
            }
        }
    }

    void StopSpin()
    {
        isSpinning = false;
        if (spinEffect != null) spinEffect.SetActive(false);
        if (sfxSource.clip == spinSound) sfxSource.Stop();
    }

    public void PlayFootstep()
    {
        if (!enabled) return;
        if (runFootstepSource != null && isGrounded && !isRecovering && moveInput.magnitude > 0.1f)
        {
            runFootstepSource.Play();
        }
    }

    public void PlayFootstepSneak()
    {
        if (!enabled) return;
        if (sneakFootstepSource != null && isGrounded && !isRecovering && moveInput.magnitude > 0.1f)
        {
            sneakFootstepSource.Play();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.1f) groundNormal = hit.normal;
        hitNormal = hit.normal;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spinRadius);
    }

    public void Teleport(Vector3 targetPosition)
    {
    
    if (controller != null) controller.enabled = false;
    transform.position = targetPosition;
    ResetState();
    if (controller != null) controller.enabled = true;
    }

    public void Die()
    {
        enabled = false;
        velocity = Vector3.zero;
        if (spinEffect != null) spinEffect.SetActive(false);
        if (slamTrigger != null) slamTrigger.gameObject.SetActive(false);
    
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("IsGroundPounding", false);
            animator.SetBool("IsRecovering", false);
            animator.SetTrigger("Dead");
        }
    }

    public void EnableControls()
    {
    enabled = true;
    }
}
