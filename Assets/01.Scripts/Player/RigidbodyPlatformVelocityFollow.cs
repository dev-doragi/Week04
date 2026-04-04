using UnityEngine;

[DefaultExecutionOrder(1100)]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPlatformVelocityFollow : MonoBehaviour
{
    [Header("Detect")]
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private float minGroundNormalY = 0.35f;

    [Header("Follow")]
    [SerializeField] private float idleSnap = 12f;
    [SerializeField] private float maxRelativeSpeed = 3.5f;
    [SerializeField] private float maxAssistAccel = 35f;
    [SerializeField] private PlayerInputAction inputAction;

    private Rigidbody rb;
    private Rigidbody platformRb;
    private int fixedTick;
    private int lastContactTick = -9999;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (inputAction == null)
        {
            inputAction = GetComponent<PlayerInputAction>();
        }
    }

    private void FixedUpdate()
    {
        fixedTick++;

        if (platformRb == null)
        {
            return;
        }

        if (fixedTick - lastContactTick > 1)
        {
            platformRb = null;
            return;
        }

        Vector3 platformVel = platformRb.GetPointVelocity(rb.position);
        Vector3 playerVel = rb.linearVelocity;

        Vector3 rel = new Vector3(
            playerVel.x - platformVel.x,
            0f,
            playerVel.z - platformVel.z
        );

        bool idle = inputAction == null || inputAction.move.sqrMagnitude < 0.0001f;

        if (idle)
        {
            float t = 1f - Mathf.Exp(-idleSnap * Time.fixedDeltaTime);
            rel = Vector3.Lerp(rel, Vector3.zero, t);
        }
        else
        {
            float relMag = rel.magnitude;
            if (relMag > maxRelativeSpeed && relMag > 0.0001f)
            {
                rel = rel / relMag * maxRelativeSpeed;
            }
        }

        Vector3 targetHorizontal = new Vector3(platformVel.x + rel.x, 0f, platformVel.z + rel.z);
        Vector3 currentHorizontal = new Vector3(playerVel.x, 0f, playerVel.z);

        Vector3 need = targetHorizontal - currentHorizontal;
        Vector3 accel = need / Mathf.Max(0.0001f, Time.fixedDeltaTime);

        float aMag = accel.magnitude;
        if (aMag > maxAssistAccel && aMag > 0.0001f)
        {
            accel = accel / aMag * maxAssistAccel;
        }

        rb.AddForce(accel, ForceMode.Acceleration);
    }

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody hitRb = collision.rigidbody;
        if (hitRb == null)
        {
            return;
        }

        if (!IsInMask(hitRb.gameObject.layer, platformMask))
        {
            return;
        }

        bool validGround = false;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            ContactPoint cp = collision.GetContact(i);
            if (cp.normal.y > minGroundNormalY)
            {
                validGround = true;
                break;
            }
        }

        if (!validGround)
        {
            return;
        }

        platformRb = hitRb;
        lastContactTick = fixedTick;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody == platformRb)
        {
            platformRb = null;
        }
    }

    private bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
