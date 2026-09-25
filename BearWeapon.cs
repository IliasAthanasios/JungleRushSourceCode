using UnityEngine;

public class BearWeapon : MonoBehaviour
{
    public bool isAttacking = false; // Controlled by the main Bear script

    private void OnTriggerEnter(Collider other)
    {
        // Only damage if we are currently in an attack animation
        if (isAttacking && other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
                isAttacking = false; // Prevent double hits in one swing
            }
        }
    }
}