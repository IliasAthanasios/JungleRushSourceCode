using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CheckpointStateManager : MonoBehaviour
{
    public static CheckpointStateManager Instance;

    private Dictionary<MonoBehaviour, object> snapshot = new Dictionary<MonoBehaviour, object>();
    private int snapshotScore;
    private bool snapshotProtection;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void CaptureSnapshot()
    {
        snapshot.Clear();
        var resettables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                            .OfType<IResettable>();

        foreach (var resettable in resettables)
        {
            snapshot[(MonoBehaviour)resettable] = resettable.SaveState();
        }

        snapshotScore = GameManager.GetScore();
        snapshotProtection = GameManager.Instance.isProtected;
        Debug.Log("Snapshot Captured: " + snapshot.Count + " objects saved.");
    }

    public void RestoreSnapshot()
    {
        foreach (var entry in snapshot)
        {
            if (entry.Key != null)
            {
                ((IResettable)entry.Key).LoadState(entry.Value);
            }
        }

        GameManager.SetScore(snapshotScore);
        GameManager.Instance.SetProtection(snapshotProtection);
        Debug.Log("Snapshot Restored!");
    }
}
