using UnityEngine;

public class MonsterProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 4f;
    
    [Header("Explosion")]
    public GameObject explosionEffect;
    
    private Vector3 moveDirection;
    private bool initialized = false;
    private GameObject owner;

    public void Launch(Vector3 targetDirection, GameObject shooter)
    {
        moveDirection = targetDirection;
        owner = shooter;
        initialized = true;
        
        transform.rotation = Quaternion.LookRotation(moveDirection);
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized) return;
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            return;

        if (other.GetComponent<MonsterProjectile>() != null)
            return;

        if (other.isTrigger) 
            return;

        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.LoseLife();
            Explode();
        }
        else
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
