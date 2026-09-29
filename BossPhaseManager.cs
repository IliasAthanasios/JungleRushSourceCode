using UnityEngine;
using System.Collections;

public class BossPhaseManager : MonoBehaviour
{
    public static BossPhaseManager Instance;

    public GameObject monster41;
    public GameObject monster42;
    public GameObject victoryPlatform;
    public GameObject lavaPlane; // Place this below the arena floor
    public GameObject rockPrefab;
    public GameObject phase2Buttons;
    
    public int currentPhase = 1;

    void Awake() => Instance = this;

    void Start() => StartPhase1();

    public void StartPhase1()
    {
        currentPhase = 1;
        monster41.SetActive(true);
        monster42.SetActive(false);
        victoryPlatform.SetActive(false);
        lavaPlane.SetActive(true);
        if (phase2Buttons != null) phase2Buttons.SetActive(false);

    }

    public void TransitionToPhase2()
    {
        currentPhase = 2;
        monster41.SetActive(false); 
        
        // Position the boss behind the camera (Y=20 for height, Z-20 to be behind it)
        GameObject entranceCamObj = GameObject.Find("BigBossEntranceCam");
        GameObject buttonCamObj=GameObject.Find("BigBossEntranceButtonCam");
        Vector3 jumpStart = new Vector3(17.5f, 30f, 25f); // Behind and above the camera
        Vector3 landPos = new Vector3(17.5f, 0.25f, 67.5f); // Arena Center
        
        var bigAI = monster42.GetComponent<BigMonsterAI>();
        if (bigAI != null)
        {
            bigAI.StartPhaseIntro(jumpStart, landPos);
        }
        
        if (entranceCamObj != null)
        {
            var entranceCam = entranceCamObj.GetComponent<Unity.Cinemachine.CinemachineCamera>();
            var buttonCam = buttonCamObj.GetComponent<Unity.Cinemachine.CinemachineCamera>();
            StartCoroutine(PlayEntranceCutscene(entranceCam,buttonCam));
        }
        if (phase2Buttons != null) phase2Buttons.SetActive(true);
    }

    private IEnumerator PlayEntranceCutscene(Unity.Cinemachine.CinemachineCamera entranceCam,Unity.Cinemachine.CinemachineCamera buttonCam)
    {
    // Force high priority to take over view
    entranceCam.Priority = 100;
    
    yield return new WaitForSeconds(3f); // Duration of the jump down
    
    // Return control to player camera
    entranceCam.Priority = 0;
    
    buttonCam.Priority=100;
    yield return new WaitForSeconds(2f);
    buttonCam.Priority=0;
    }

    [Header("Phase 3 Cutscene")]
    public Unity.Cinemachine.CinemachineCamera phase3IntroCam;
    public float cutsceneDuration = 4f;

    public void TransitionToPhase3()
    {
        if (currentPhase == 3) return;
            currentPhase = 3;

            // Reset Laser and Buttons for the new phase
            if (ArenaManager.Instance != null) ArenaManager.Instance.ResetSystem();

            //var ai = monster42.GetComponent<BigMonsterAI>();
            //if (ai != null) ai.EnterPhase3();
            
            StartCoroutine(Phase3IntroSequence());
    }
    private IEnumerator Phase3IntroSequence()
    {
        // A. Freeze Characters & Disable Controls
        var player = GameObject.FindGameObjectWithTag("Player");
        var playerCtrl = player.GetComponent<CrashPlayerController>();
        var bossAI = monster42.GetComponent<BigMonsterAI>();
        
        if (playerCtrl != null) playerCtrl.enabled = false;
        if (bossAI != null) 
        {
            bossAI.enabled = false;
            // Also stop the NavMeshAgent to freeze movement
            var agent = bossAI.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.isStopped = true;
        }

        // B. Switch Camera
        if (phase3IntroCam != null) phase3IntroCam.Priority = 50;

        // C. Teleport Player to safe spot
        Vector3 safeSpot = new Vector3(17.5f, 0.25f, 67.5f);
        if (playerCtrl != null) playerCtrl.Teleport(safeSpot);

        // D. Environment Destruction
        ArenaTile[] tiles = Object.FindObjectsByType<ArenaTile>(FindObjectsSortMode.None);
        
        // Start dropping tiles in the background (one-time pass)
        StartCoroutine(DropTilesPass(tiles));

        // E. Continuous Rock Spawning for the duration
        float startTime = Time.time;
        while (Time.time < startTime + cutsceneDuration)
        {
            Vector3 rockPos = new Vector3(Random.Range(5f, 30f), 30f, Random.Range(55f, 80f));
            if (rockPrefab != null) Instantiate(rockPrefab, rockPos, Quaternion.Euler(Random.Range(0, 360), 0, 0));
            
            yield return new WaitForSeconds(0.2f);
        }

        // F. Restore Controls & Start Phase 3
        if (phase3IntroCam != null) phase3IntroCam.Priority = 0;
        if (playerCtrl != null) playerCtrl.enabled = true;
        if (bossAI != null) 
        {
            bossAI.enabled = true;
            bossAI.EnterPhase3();
        }
    }
    private IEnumerator DropTilesPass(ArenaTile[] tiles)
    {
        foreach (var tile in tiles)
        {
            if (tile != null) 
            {
                tile.Drop();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    [Header("Victory Sequence")]
    public Unity.Cinemachine.CinemachineCamera victoryCam;
    public Unity.Cinemachine.CinemachineCamera monsterDeathCam;

    public void Victory()
    {
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
    // 1. Deactivate Laser Immediately
    if (ArenaManager.Instance != null) ArenaManager.Instance.ResetSystem();

    // 2. Freeze Player Controls
    var playerCtrl = GameObject.FindGameObjectWithTag("Player").GetComponent<CrashPlayerController>();
    if (playerCtrl != null) playerCtrl.enabled = false;
    
    if (BossHealthUI.Instance != null) BossHealthUI.Instance.Hide();

    // 3. Show the Monster Death First
    if (monsterDeathCam != null) monsterDeathCam.Priority = 100;
    
    var bigAI = monster42.GetComponent<BigMonsterAI>();
    if (bigAI != null) bigAI.Die(); // Roar and animation triggered here

    yield return new WaitForSeconds(3.0f); // Wait for the roar and fall

    // 4. Pan to the Victory Platform
    if (monsterDeathCam != null) monsterDeathCam.Priority = 0;
    if (victoryCam != null) victoryCam.Priority = 100;

    yield return new WaitForSeconds(1.0f);
    
    if (victoryPlatform != null) victoryPlatform.SetActive(true);

    yield return new WaitForSeconds(3.0f);

    // 5. Restore Control
    if (victoryCam != null) victoryCam.Priority = 0;
    if (playerCtrl != null) playerCtrl.enabled = true;
    }

    
}