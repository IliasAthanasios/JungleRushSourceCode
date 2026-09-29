using UnityEngine;

public class ShieldBanana : MonoBehaviour
{
    [Header("Collection Settings")]
    public int scoreValue = 5;
    public GameObject collectEffect;
    public AudioClip collectSound;
    public AudioClip shieldPowerupSound;

    private bool isCollected = false; // The gate

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetProtection(true);
            GameManager.Instance.AddScore(scoreValue, transform.position);
        }

        if (shieldPowerupSound != null)
        {
            SFXManager.PlaySFXAtPoint(shieldPowerupSound, transform.position);
        }
        else if (collectSound != null)
        {
            SFXManager.PlaySFXAtPoint(collectSound, transform.position);
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}