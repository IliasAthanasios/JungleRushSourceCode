using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public bool playOnStart = true; 
    public CinemachineCamera cutsceneCamera;
    public float cutsceneDuration = 5f;

    [Header("Boss Integration")]
    public bool showBossHealth = false;
    public float bossMaxHealth = 3f;

    private bool hasPlayed = false;

    private void Start()
    {
        if (playOnStart && !hasPlayed)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) StartCoroutine(PlayCutscene(player));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasPlayed && other.CompareTag("Player"))
        {
            StartCoroutine(PlayCutscene(other.gameObject));
        }
    }

    public IEnumerator PlayCutscene(GameObject player)
    {
        if (hasPlayed) yield break; 
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
        
        gameObject.SetActive(false);
    }
}
