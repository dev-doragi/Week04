using UnityEngine;

[DefaultExecutionOrder(5000)]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerEntity))]
[RequireComponent(typeof(PlayerInputAction))]
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
    private Quaternion lastPlatformRot;
    private bool hasPlatformState;

    private PlayerEntity playerEntity;
    [SerializeField] private PlayerInputAction inputAction;

    public Vector3 CurrentPlatformVelocity { get; private set; }

    // 필드 추가
    [SerializeField] private float airCarryTime = 0.25f;
    private float airCarryTimer;
    private Vector3 lastPlatformVelocity;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputAction = GetComponent<PlayerInputAction>();
        playerEntity = GetComponent<PlayerEntity>();
        rb.sleepThreshold = 0f;
        CurrentPlatformVelocity = Vector3.zero;
        airCarryTimer = 0f;
        lastPlatformVelocity = Vector3.zero;
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

            CurrentPlatformVelocity = currentPlatformRb.GetPointVelocity(rb.position);
            lastPlatformVelocity = CurrentPlatformVelocity;
            airCarryTimer = airCarryTime;
        }
        else
        {
            missTicks++;

            bool keepSticky = currentPlatformRb != null && missTicks <= detachGraceTicks;

            if (keepSticky)
            {
                CurrentPlatformVelocity = currentPlatformRb.GetPointVelocity(rb.position);
                lastPlatformVelocity = CurrentPlatformVelocity;
                airCarryTimer = airCarryTime;
            }
            else
            {
                currentPlatformRb = null;
                hasPlatformState = false;

                if (airCarryTimer > 0f)
                {
                    airCarryTimer -= Time.fixedDeltaTime;
                    float ratio = Mathf.Clamp01(airCarryTimer / Mathf.Max(0.0001f, airCarryTime));
                    CurrentPlatformVelocity = lastPlatformVelocity * ratio;
                }
                else
                {
                    CurrentPlatformVelocity = Vector3.zero;
                }
            }
        }

        bool hasMoveInput = inputAction.move.sqrMagnitude > 0.0001f;
        bool idle = playerEntity.InputLock || !hasMoveInput;

        if (idle)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(CurrentPlatformVelocity.x, v.y, CurrentPlatformVelocity.z);
        }

        if (currentPlatformRb == null)
        {
            return;
        }

        Quaternion platformRot = currentPlatformRb.rotation;

        if (!hasPlatformState)
        {
            lastPlatformRot = platformRot;
            hasPlatformState = true;
            return;
        }

        if (followYaw)
        {
            Quaternion rotDelta = platformRot * Quaternion.Inverse(lastPlatformRot);
            float yawDelta = Mathf.DeltaAngle(0f, rotDelta.eulerAngles.y) * Mathf.Clamp01(yawFollow);

            if (Mathf.Abs(yawDelta) > 0.0001f)
            {
                rb.MoveRotation(Quaternion.Euler(0f, yawDelta, 0f) * rb.rotation);
            }
        }

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

        int hitCount = hits.Length;
        for (int i = 0; i < hitCount; i++)
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
