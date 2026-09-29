using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    [Header("Cameras")]
    public CinemachineCamera introCamera;  
    public CinemachineCamera dollyCamera;  
    public CinemachineSplineDolly splineDolly;

    [Header("Targets")]
    public Transform doorTarget;
    public Transform leverTarget;

    [Header("Settings")]
    public float standDuration = 1.5f;   
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

        var controller = player.GetComponent<CrashPlayerController>();
        if (controller != null) 
        {
            controller.ResetState(); 
            yield return new WaitForEndOfFrame();
            controller.enabled = false;
        }

        if (doorTarget != null) introCamera.LookAt = doorTarget;
        introCamera.Priority = 100;

        yield return new WaitForSeconds(standDuration);

        dollyCamera.Priority = 110;
        if (leverTarget != null) dollyCamera.LookAt = leverTarget;
        if (splineDolly != null) splineDolly.CameraPosition = 0;

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            if (splineDolly != null)
                splineDolly.CameraPosition = elapsed / travelDuration;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        introCamera.Priority = 0;
        dollyCamera.Priority = 0;
        
        if (controller != null) controller.enabled = true;
    }
}
