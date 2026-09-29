using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    [Header("Cameras")]
    public CinemachineCamera introCamera;  // The "Standing Point" camera
    public CinemachineCamera dollyCamera;  // The camera on the Spline
    public CinemachineSplineDolly splineDolly;

    [Header("Targets")]
    public Transform doorTarget;
    public Transform leverTarget;

    [Header("Settings")]
    public float standDuration = 1.5f;   // How long to stay at the standing point
    public float travelDuration = 5f;
    public string saveKey = "TutorialCutscenePlayed";
    public bool resetOnStart = true;

    private bool hasPlayed = false;

    void Awake()
    {
        Instance = this;
        if (resetOnStart) PlayerPrefs.SetInt(saveKey, 0);
        if (PlayerPrefs.GetInt(saveKey, 0) == 1) hasPlayed = true;
    }

    public void DeactivateCutscene()
    {
    hasPlayed = true;
    // Save to PlayerPrefs so it stays deactivated even if they reload the level
    PlayerPrefs.SetInt(saveKey, 1);
    PlayerPrefs.Save();
    }

    public void TriggerCutscene(GameObject player)
    {
        if (hasPlayed) return;
        StartCoroutine(PlayRoutine(player));
    }

    private IEnumerator PlayRoutine(GameObject player)
    {
        hasPlayed = true;
        PlayerPrefs.SetInt(saveKey, 1);
        PlayerPrefs.Save();

        // 1. Stop Player and Footsteps
        var controller = player.GetComponent<CrashPlayerController>();
        if (controller != null) 
        {
            controller.ResetState(); 
            yield return new WaitForEndOfFrame();
            controller.enabled = false;
        }

        // 2. PHASE 1: The Standing Point (Intro Camera)
        // Look at the door from the static standing position
        if (doorTarget != null) introCamera.LookAt = doorTarget;
        introCamera.Priority = 100;

        yield return new WaitForSeconds(standDuration);

        // 3. PHASE 2: Transition to Spline (Dolly Camera)
        // Setting a higher priority triggers a smooth blend to the path
        dollyCamera.Priority = 110;
        if (leverTarget != null) dollyCamera.LookAt = leverTarget;
        if (splineDolly != null) splineDolly.CameraPosition = 0;

        // Small delay to allow the blend to start before moving
        yield return new WaitForSeconds(0.5f);

        // 4. PHASE 3: Move along the Spline
        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            if (splineDolly != null)
                splineDolly.CameraPosition = elapsed / travelDuration;
            yield return null;
        }

        // 5. Show the Lever at the end
        yield return new WaitForSeconds(1.5f);

        // 6. Return Control
        introCamera.Priority = 0;
        dollyCamera.Priority = 0;
        
        if (controller != null) controller.enabled = true;
    }
}