using UnityEngine;

public class ArenaTile : MonoBehaviour
{
    private bool hasDropped = false;

    public void Drop()
    {
        if (hasDropped) return; 
        hasDropped = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        
        Destroy(gameObject, 3f);
    }
}
