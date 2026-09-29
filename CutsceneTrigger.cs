using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public bool playOnStart = true; // Set this to TRUE in the Inspector
    public CinemachineCamera cutsceneCamera;
    public float cutsceneDuration = 5f;

    [Header("Boss Integration")]
    public bool showBossHealth = false;
    public float bossMaxHealth = 3f;

    private bool hasPlayed = false;

    private void Start()
    {
        // Removed PlayerPrefs check so it plays every time the scene loads
        if (playOnStart && !hasPlayed)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) StartCoroutine(PlayCutscene(player));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // hasPlayed prevents it from triggering again if the player walks back into it
        if (!hasPlayed && other.CompareTag("Player"))
        {
            StartCoroutine(PlayCutscene(other.gameObject));
        }
    }

    public IEnumerator PlayCutscene(GameObject player)
    {
        if (hasPlayed) yield break; // Safety check
        hasPlayed = true;

        var controller = player.GetComponent<CrashPlayerController>();
        if (controller != null) {
            controller.ResetState();
            controller.enabled = false;
        }

        if (showBossHealth && BossHealthUI.Instance != null)
        {
            BossHealthUI.Instance.Setup(bossMaxHealth);
        }

        cutsceneCamera.Priority = 100;
        yield return new WaitForSeconds(cutsceneDuration);
        cutsceneCamera.Priority = 0;

        if (controller != null) controller.enabled = true;
        
        // Deactivate the trigger object so it can't be hit again
        gameObject.SetActive(false);
    }
}