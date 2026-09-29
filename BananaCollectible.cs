using UnityEngine;

public class BananaCollectible : MonoBehaviour, IResettable
{
    [SerializeField] private int scoreValue = 1;
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;

    private bool isCollected = false;

    public object SaveState() => gameObject.activeSelf;
    public void LoadState(object state)
    {
        bool wasActive = (bool)state;
        gameObject.SetActive(wasActive);
        isCollected = !wasActive;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (other.CompareTag("Player")) Collect();
    }

    private void Collect()
    {
        isCollected = true;
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(scoreValue, transform.position, collectSound);

        if (collectEffect != null) Instantiate(collectEffect, transform.position, Quaternion.identity);
        
        gameObject.SetActive(false); 
    }
}
