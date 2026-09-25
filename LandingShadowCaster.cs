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
    [Range(0, 1)] public float maxOpacity = 1.0f; 
    [Range(0, 1)] public float minOpacity = 0.05f;
    public float groundedThreshold = 0.1f; 

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
        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, maxDistance, groundLayer);
        RaycastHit bestHit = new RaycastHit();
        bool foundGround = false;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
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
            float actualDistance = Mathf.Max(0, bestHit.distance - 1.0f);

            if (actualDistance < groundedThreshold)
            {
                decalObject.SetActive(false);
                return;
            }

            decalObject.SetActive(true);
            
            decalObject.transform.position = bestHit.point + bestHit.normal * 0.5f;
            decalObject.transform.rotation = Quaternion.LookRotation(-bestHit.normal);
            
            float distanceRatio = Mathf.Clamp01(actualDistance / maxDistance);
            
            projector.fadeFactor = Mathf.Lerp(maxOpacity, minOpacity, distanceRatio);
            
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
