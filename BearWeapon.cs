using UnityEngine;

public class BearWeapon : MonoBehaviour
{
    public bool isAttacking = false; 
    private void OnTriggerEnter(Collider other)
    {
        if (isAttacking && other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
                isAttacking = false; 
            }
        }
    }
}
