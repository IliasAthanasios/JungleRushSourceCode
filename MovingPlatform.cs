using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform[] waypoints;
    public float speed = 3.0f;
    public float waitTime = 1.0f;

    private int currentWaypointIndex = 0;
    private float timer = 0f;
    private Rigidbody rb;
    private Vector3 previousPosition;

    private List<CharacterController> riders = new List<CharacterController>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            previousPosition = transform.position;
        }
    }

    void FixedUpdate()
    {
        if (waypoints.Length < 2) return;

        if (timer > 0)
        {
            timer -= Time.fixedDeltaTime;
            previousPosition = rb.position; 
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        Vector3 newPos = Vector3.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime);
        
        Vector3 platformDelta = newPos - rb.position;

        rb.MovePosition(newPos);

        foreach (var rider in riders)
        {
            if (rider != null && rider.enabled)
            {
                rider.Move(platformDelta);
            }
        }

        if (Vector3.Distance(rb.position, target.position) < 0.05f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            timer = waitTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponent<CharacterController>();
        if (controller != null && !riders.Contains(controller))
        {
            riders.Add(controller);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponent<CharacterController>();
        if (controller != null)
        {
            riders.Remove(controller);
        }
    }
}
