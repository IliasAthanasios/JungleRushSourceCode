using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public bool isFinalGameGoal = false; // Check αυτο MONO στο FinalBoss

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
