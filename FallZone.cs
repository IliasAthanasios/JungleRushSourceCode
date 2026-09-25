using UnityEngine;

public class FallZone : MonoBehaviour
{
    // You can keep this variable if you want to use it for checkpoints, 
    // otherwise the player will just respawn at the last saved checkpoint.
    public Vector3 spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                // Trigger the death animation and respawn sequence
                GameManager.Instance.LoseLife();
            }

            Debug.Log("Player fell! GameManager is handling the death animation...");
        }
    }
}