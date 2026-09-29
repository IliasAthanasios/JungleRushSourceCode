using UnityEngine;
using Unity.Cinemachine;

public class CameraZoneTrigger : MonoBehaviour
{
    public CinemachineCamera zoneCamera;
    public int activePriority = 20;
    public int inactivePriority = 5;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            zoneCamera.Priority = activePriority;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            zoneCamera.Priority = inactivePriority;
        }
    }
}