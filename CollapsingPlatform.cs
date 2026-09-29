using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CollapsingPlatform : MonoBehaviour
{
    [Header("Collapse Settings")]
    [Tooltip("How long the platform shakes before falling")]
    public float shakeDuration = 1.0f;
    [Tooltip("How violent the shaking is")]
    public float shakeIntensity = 0.1f;
    [Tooltip("Time before the platform reappears after falling")]
    public float resetDelay = 3.0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Rigidbody rb;
    private bool isCollapsing = false;
    
    private Renderer[] childRenderers;
    private Collider[] childColliders;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        
        rb.isKinematic = true;
        rb.useGravity = false;

        childRenderers = GetComponentsInChildren<Renderer>();
        childColliders = GetComponentsInChildren<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isCollapsing && collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(CollapseSequence());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isCollapsing && other.CompareTag("Player"))
        {
            StartCoroutine(CollapseSequence());
        }
    }

    private IEnumerator CollapseSequence()
    {
        isCollapsing = true;

        float elapsed = 0;
        while (elapsed < shakeDuration)
        {
            transform.position = originalPosition + Random.insideUnitSphere * shakeIntensity;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        yield return new WaitForSeconds(2.0f);

        SetPlatformVisibility(false);
        
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return new WaitForSeconds(resetDelay);
        
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        SetPlatformVisibility(true);
        
        isCollapsing = false;
    }

    private void SetPlatformVisibility(bool visible)
    {
        foreach (var r in childRenderers) r.enabled = visible;
        foreach (var c in childColliders) 
        {
            if (!c.isTrigger) c.enabled = visible;
        }
    }
}
