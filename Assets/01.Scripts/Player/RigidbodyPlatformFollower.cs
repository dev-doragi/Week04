using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPlatformFollower : MonoBehaviour
{
    [Header("Platform Detect (Collision)")]
    [SerializeField] private LayerMask movingPlatformMask;
    [SerializeField] private float minGroundNormalY = 0.35f;

    [Header("Follow")]
    [SerializeField] private bool followVertical = true;
    [SerializeField] private bool followYaw = true;
    [SerializeField] private float yawFollow = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private float debugLogInterval = 0.2f;

    [SerializeField] private bool lockHorizontalToPlatform = true;
    [SerializeField] private float idleSnapDamping = 14f;
    [SerializeField] private float maxRelativeSpeedOnPlatform = 3.5f;
    [SerializeField] private PlayerInputAction inputAction;


    private Rigidbody rb;
    private Rigidbody currentPlatformRb;

    private Vector3 lastPlatformPos;
    private Quaternion lastPlatformRot;
    private bool hasPlatformState;

    private int fixedTick;
    private int lastContactTick = -9999;
    private float nextLogTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        LogImmediate("[PF][Awake] rb=" + (rb != null));
    }

    private void FixedUpdate()
    {
        fixedTick++;

        if (currentPlatformRb == null)
        {
            return;
        }

        if (fixedTick - lastContactTick > 1)
        {
            LogRate("[PF][Lost] contact timeout");
            currentPlatformRb = null;
            hasPlatformState = false;
            return;
        }

        Vector3 platformPos = currentPlatformRb.position;
        Quaternion platformRot = currentPlatformRb.rotation;

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

        Vector3 totalDelta = posDelta + rotMoveDelta;
        rb.MovePosition(rb.position + totalDelta);

        if (followYaw)
        {
            float yaw = Mathf.DeltaAngle(0f, rotDelta.eulerAngles.y) * Mathf.Clamp01(yawFollow);
            Quaternion yawDelta = Quaternion.Euler(0f, yaw, 0f);
            rb.MoveRotation(yawDelta * rb.rotation);
        }
        ApplyPlatformVelocityCompensation();


        lastPlatformPos = platformPos;
        lastPlatformRot = platformRot;

        LogRate(
            "[PF][Move] platform=" + currentPlatformRb.name +
            " totalDelta=" + totalDelta.ToString("F3"));
    }

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody hitRb = collision.rigidbody;
        if (hitRb == null)
        {
            return;
        }

        if (!IsInLayerMask(hitRb.gameObject.layer, movingPlatformMask))
        {
            return;
        }

        bool validGroundContact = false;
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            ContactPoint cp = collision.GetContact(i);
            if (cp.normal.y > minGroundNormalY)
            {
                validGroundContact = true;
                break;
            }
        }

        if (!validGroundContact)
        {
            return;
        }

        if (currentPlatformRb != hitRb)
        {
            currentPlatformRb = hitRb;
            hasPlatformState = false;
            LogImmediate("[PF][Enter] platform=" + hitRb.name);
        }

        lastContactTick = fixedTick;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody == currentPlatformRb)
        {
            LogRate("[PF][Exit] platform=" + currentPlatformRb.name);
            currentPlatformRb = null;
            hasPlatformState = false;
        }
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void LogRate(string message)
    {
        if (!debugLogs)
        {
            return;
        }

        if (Time.unscaledTime < nextLogTime)
        {
            return;
        }

        nextLogTime = Time.unscaledTime + Mathf.Max(0.02f, debugLogInterval);
        Debug.Log(message, this);
    }

    private void LogImmediate(string message)
    {
        if (!debugLogs)
        {
            return;
        }

        Debug.Log(message, this);
    }
    private void ApplyPlatformVelocityCompensation()
    {
        if (!lockHorizontalToPlatform || currentPlatformRb == null || rb == null)
        {
            return;
        }

        Vector3 platformVel = currentPlatformRb.GetPointVelocity(rb.position);
        Vector3 playerVel = rb.linearVelocity;

        Vector3 rel = new Vector3(
            playerVel.x - platformVel.x,
            0f,
            playerVel.z - platformVel.z
        );

        bool idle = true;
        if (inputAction != null)
        {
            idle = inputAction.move.sqrMagnitude < 0.0001f;
        }

        if (idle)
        {
            float t = 1f - Mathf.Exp(-idleSnapDamping * Time.fixedDeltaTime);
            rel = Vector3.Lerp(rel, Vector3.zero, t);
        }
        else
        {
            float relMag = rel.magnitude;
            if (relMag > maxRelativeSpeedOnPlatform && relMag > 0.0001f)
            {
                rel = rel / relMag * maxRelativeSpeedOnPlatform;
            }
        }

        Vector3 finalHorizontal = new Vector3(platformVel.x + rel.x, 0f, platformVel.z + rel.z);
        rb.linearVelocity = new Vector3(finalHorizontal.x, playerVel.y, finalHorizontal.z);
    }

}
