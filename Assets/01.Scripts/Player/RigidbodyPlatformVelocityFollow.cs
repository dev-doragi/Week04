using UnityEngine;

[DefaultExecutionOrder(5000)]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPlatformVelocityFollow : MonoBehaviour
{
    [Header("Detect")]
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private bool allowBoatTagFallback = true;
    [SerializeField] private float minGroundNormalY = 0.05f;
    [SerializeField] private float probeRadius = 0.28f;
    [SerializeField] private float probeDistance = 1.2f;

    [Header("Follow")]
    [SerializeField] private bool followVertical = true;
    [SerializeField] private bool followYaw = true;
    [SerializeField] private float yawFollow = 1f;

    [SerializeField] private int detachGraceTicks = 3;
    private int missTicks;


    private Rigidbody rb;
    private Rigidbody currentPlatformRb;

    private Vector3 lastPlatformPos;
    private Quaternion lastPlatformRot;
    private bool hasPlatformState;
    [SerializeField] private PlayerInputAction inputAction;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (inputAction == null) inputAction = GetComponent<PlayerInputAction>();
        rb.sleepThreshold = 0f;
    }

    private void FixedUpdate()
    {
        Rigidbody detectedPlatform;
        bool found = TryDetectPlatform(out detectedPlatform);

        if (found)
        {
            missTicks = 0;

            if (currentPlatformRb != detectedPlatform)
            {
                currentPlatformRb = detectedPlatform;
                hasPlatformState = false;
            }
        }
        else
        {
            missTicks++;

            if (currentPlatformRb == null || missTicks > detachGraceTicks)
            {
                currentPlatformRb = null;
                hasPlatformState = false;
                return;
            }
        }

        Vector3 platformPos = currentPlatformRb.position;
        Quaternion platformRot = currentPlatformRb.rotation;
        bool idle = inputAction == null || inputAction.move.sqrMagnitude < 0.0001f;
        if (idle)
        {
            Vector3 platformVel = currentPlatformRb.GetPointVelocity(rb.position);
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(platformVel.x, v.y, platformVel.z);

            lastPlatformPos = platformPos;
            lastPlatformRot = platformRot;
            hasPlatformState = true;
            return;
        }
        if (!hasPlatformState)
        {
            lastPlatformPos = platformPos;
            lastPlatformRot = platformRot;
            hasPlatformState = true;
            return;
        }

        Vector3 posDelta = platformPos - lastPlatformPos;
        Quaternion rotDelta = platformRot * Quaternion.Inverse(lastPlatformRot);

        Vector3 pivotToPlayer = rb.position - platformPos;
        Vector3 rotatedPivot = rotDelta * pivotToPlayer;
        Vector3 rotMoveDelta = rotatedPivot - pivotToPlayer;

        if (!followVertical)
        {
            posDelta.y = 0f;
            rotMoveDelta.y = 0f;
        }

        rb.MovePosition(rb.position + posDelta + rotMoveDelta);

        if (followYaw)
        {
            float yaw = Mathf.DeltaAngle(0f, rotDelta.eulerAngles.y) * Mathf.Clamp01(yawFollow);
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f) * rb.rotation);
        }

        lastPlatformPos = platformPos;
        lastPlatformRot = platformRot;
    }

    private bool TryDetectPlatform(out Rigidbody platform)
    {
        platform = null;

        Vector3 origin = rb.position + Vector3.up * 0.05f;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            probeRadius,
            Vector3.down,
            probeDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            Rigidbody hitRb = hit.rigidbody;
            if (hitRb == null)
            {
                hitRb = hit.collider.attachedRigidbody;
            }

            if (hitRb == null || hitRb == rb)
            {
                continue;
            }

            bool inMask = (platformMask.value & (1 << hitRb.gameObject.layer)) != 0;
            bool tagOk = allowBoatTagFallback && hitRb.CompareTag("Boat");
            if (!inMask && !tagOk)
            {
                continue;
            }

            if (hit.normal.y < minGroundNormalY)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                platform = hitRb;
            }
        }

        return platform != null;
    }
}
