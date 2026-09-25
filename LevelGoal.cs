using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public bool isFinalGameGoal = false; // Check this ONLY on the boss level goal

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                if (isFinalGameGoal)
                {
                    MainMenuController.showCreditsOnLoad = true;
                }
                GameManager.Instance.WinGame();
            }
        }
    }
}