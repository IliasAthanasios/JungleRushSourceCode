using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LandingShadowCaster : MonoBehaviour
{
    [Header("Decal Settings")]
    public Material decalMaterial;
    public Vector3 baseDecalSize = new Vector3(1.2f, 1.2f, 1.0f);
    public float maxDistance = 20f;
    public LayerMask groundLayer = ~0; 
    
    [Header("Dynamic Scaling")]
    public float maxScaleMultiplier = 1.3f; 
    public float minScaleMultiplier = 0.4f;
    
    [Header("Visuals")]
    [Range(0, 1)] public float maxOpacity = 1.0f; // Increased to 1.0 for more visibility
    [Range(0, 1)] public float minOpacity = 0.05f;
    public float groundedThreshold = 0.1f; // The distance at which the shadow disappears

    private GameObject decalObject;
    private DecalProjector projector;

    void Start()
    {
        decalObject = new GameObject("LandingDecalMarker");
        projector = decalObject.AddComponent<DecalProjector>();
        projector.material = decalMaterial;
        projector.size = baseDecalSize;
        decalObject.SetActive(false);
    }

    void LateUpdate()
    {
        // 1. We start the raycast from above the character's pivot to avoid starting inside the floor
        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        
        // 2. Find ground hits
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, maxDistance, groundLayer);
        RaycastHit bestHit = new RaycastHit();
        bool foundGround = false;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // Ignore if we hit ourselves
            if (hit.collider.transform.root == transform.root) continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                bestHit = hit;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            // Calculate actual distance from the feet to the ground
            // (Subtracting the 1.0m offset we added to the ray start)
            float actualDistance = Mathf.Max(0, bestHit.distance - 1.0f);

            // NEW: Deactivate when touching the ground or very close to it
            if (actualDistance < groundedThreshold)
            {
                decalObject.SetActive(false);
                return;
            }

            decalObject.SetActive(true);
            
            // Adjust position and rotation to match ground surface
            decalObject.transform.position = bestHit.point + bestHit.normal * 0.5f;
            decalObject.transform.rotation = Quaternion.LookRotation(-bestHit.normal);
            
            float distanceRatio = Mathf.Clamp01(actualDistance / maxDistance);
            
            // Update Opacity based on distance
            projector.fadeFactor = Mathf.Lerp(maxOpacity, minOpacity, distanceRatio);
            
            // Update Size based on distance
            float currentScale = Mathf.Lerp(maxScaleMultiplier, minScaleMultiplier, distanceRatio);
            projector.size = new Vector3(
                baseDecalSize.x * currentScale, 
                baseDecalSize.y * currentScale, 
                baseDecalSize.z
            );
        }
        else
        {
            decalObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (decalObject != null) Destroy(decalObject);
    }
}