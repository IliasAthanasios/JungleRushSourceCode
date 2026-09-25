using UnityEngine;

public class FallZone : MonoBehaviour
{
    public Vector3 spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
            }

            Debug.Log("Player fell! GameManager is handling the death animation...");
        }
    }
}
