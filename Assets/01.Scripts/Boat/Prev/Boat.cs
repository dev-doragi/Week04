using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Boat : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float forwardAcceleration = 100f;
    [SerializeField] float maxSpeed = 6f;
    Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void FixedUpdate()
{
    Vector3 forward = transform.forward;
    forward.y = 0f;
    forward.Normalize();

    rb.AddForce(forward * forwardAcceleration, ForceMode.Acceleration);

    Vector3 v = rb.linearVelocity;
    Vector3 h = new Vector3(v.x, 0f, v.z);
    if (h.magnitude > maxSpeed)
    {
        Vector3 limited = h.normalized * maxSpeed;
        rb.linearVelocity = new Vector3(limited.x, v.y, limited.z);
    }
}
}
