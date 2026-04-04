using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatStabilityByBlocks : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private Rigidbody boatRb;
    [SerializeField] private LayerMask blockLayerMask = ~0;

    [Header("Center")]
    [SerializeField] private Vector3 stabilityCenterLocal = Vector3.zero;
    [SerializeField] private bool invertPitch = false;
    [SerializeField] private bool autoCenterAtStart = true;

    [Header("Block Grid")]
    [SerializeField] private float blockSizeX = 3f;
    [SerializeField] private float blockSizeZ = 3f;
    [SerializeField] private bool captureBaselineAtStart = true;

    [Header("Per Block Tilt")]
    [SerializeField] private float rollPerBlock = 0.01f;
    [SerializeField] private float pitchPerBlock = 0.01f;
    [SerializeField] private float sinkPerBlock = 0.005f;
    [SerializeField] private float biasLerp = 0.05f;
    [SerializeField] private float deadZoneBlocks = 0.15f;
    [SerializeField] private float maxImbalanceBlocks = 5f;
    [SerializeField] private float maxTorqueAccel = 0.15f;
    [SerializeField] private float pressPointX = 4.5f;
    [SerializeField] private float pressPointZ = 6f;

    [Header("Build Kick (Optional)")]
    [SerializeField] private float buildKickRoll = 0f;
    [SerializeField] private float buildKickDown = 0f;


    [SerializeField] private bool useManualCenterAnchor = true;
    [SerializeField] private Transform manualCenterAnchor;







    
    private Vector2 baseBiasBlocks = Vector2.zero;
    private Vector2 smoothedBiasBlocks = Vector2.zero;

    private void Awake()
    {
        if (boatRb == null)
        {
            boatRb = GetComponent<Rigidbody>();
        }

        if (blocksRoot == null)
        {
            blocksRoot = transform;
        }
    }

    private void Start()
    {
        if (useManualCenterAnchor && manualCenterAnchor != null)
        {
            SetCenterFromAnchor();
        }
        else if (autoCenterAtStart)
        {
            AutoSetCenterFromBlocks();
        }

        if (captureBaselineAtStart)
        {
            Rebaseline();
        }
        else
        {
            smoothedBiasBlocks = Vector2.zero;
        }
    }


    [ContextMenu("Set Center From Anchor")]
    public void SetCenterFromAnchor()
    {
        if (manualCenterAnchor == null)
        {
            return;
        }

        stabilityCenterLocal = transform.InverseTransformPoint(manualCenterAnchor.position);
    }

    [ContextMenu("Log Stability Center")]
    public void LogStabilityCenter()
    {
        Debug.Log("stabilityCenterLocal = " + stabilityCenterLocal);
    }
    [ContextMenu("Rebaseline")]
    public void Rebaseline()
    {
        baseBiasBlocks = ComputeRawBiasBlocks();
        smoothedBiasBlocks = Vector2.zero;
    }

    [ContextMenu("Auto Set Center From Blocks")]
    public void AutoSetCenterFromBlocks()
    {
        if (blocksRoot == null)
        {
            return;
        }

        bool hasAny = false;
        Vector3 minLocal = Vector3.zero;
        Vector3 maxLocal = Vector3.zero;

        int childCount = blocksRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = blocksRoot.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!IsInLayerMask(child.gameObject.layer, blockLayerMask))
            {
                continue;
            }

            Vector3 local = transform.InverseTransformPoint(child.position);

            if (!hasAny)
            {
                minLocal = local;
                maxLocal = local;
                hasAny = true;
            }
            else
            {
                minLocal = Vector3.Min(minLocal, local);
                maxLocal = Vector3.Max(maxLocal, local);
            }
        }

        if (!hasAny)
        {
            return;
        }

        stabilityCenterLocal.x = (minLocal.x + maxLocal.x) * 0.5f;
        stabilityCenterLocal.z = (minLocal.z + maxLocal.z) * 0.5f;
    }

    private void FixedUpdate()
    {
        if (boatRb == null || blocksRoot == null)
        {
            return;
        }

        Vector2 rawBias = ComputeRawBiasBlocks();
        Vector2 targetBias = rawBias - baseBiasBlocks;

        targetBias.x = Mathf.Clamp(targetBias.x, -maxImbalanceBlocks, maxImbalanceBlocks);
        targetBias.y = Mathf.Clamp(targetBias.y, -maxImbalanceBlocks, maxImbalanceBlocks);

        smoothedBiasBlocks = Vector2.Lerp(smoothedBiasBlocks, targetBias, Mathf.Clamp01(biasLerp));

        float bx = Mathf.Abs(smoothedBiasBlocks.x) < deadZoneBlocks ? 0f : smoothedBiasBlocks.x;
        float bz = Mathf.Abs(smoothedBiasBlocks.y) < deadZoneBlocks ? 0f : smoothedBiasBlocks.y;

        if (bx == 0f && bz == 0f)
        {
            return;
        }

        float pitchDir = invertPitch ? -1f : 1f;

        Vector3 rollTorque = -transform.forward * (bx * rollPerBlock);
        Vector3 pitchTorque = transform.right * (bz * pitchPerBlock * pitchDir);
        Vector3 totalTorque = rollTorque + pitchTorque;

        float torqueMag = totalTorque.magnitude;
        if (torqueMag > maxTorqueAccel && torqueMag > 0.0001f)
        {
            totalTorque = totalTorque / torqueMag * maxTorqueAccel;
        }

        boatRb.AddTorque(totalTorque, ForceMode.Acceleration);

        float press = (Mathf.Abs(bx) + Mathf.Abs(bz)) * sinkPerBlock;

        float localPressX = Mathf.Abs(bx) > 0.001f ? Mathf.Sign(bx) * pressPointX : 0f;
        float localPressZ = Mathf.Abs(bz) > 0.001f ? Mathf.Sign(bz) * pressPointZ * pitchDir : 0f;

        Vector3 localPressPoint = stabilityCenterLocal + new Vector3(localPressX, 0f, localPressZ);
        Vector3 worldPressPoint = transform.TransformPoint(localPressPoint);

        boatRb.AddForceAtPosition(Vector3.down * press, worldPressPoint, ForceMode.Acceleration);
    }

    public void NotifyBlockPlaced(Vector3 placedWorldPos)
    {
        if (boatRb == null)
        {
            return;
        }

        if (buildKickRoll > 0f)
        {
            Vector3 localPos = transform.InverseTransformPoint(placedWorldPos) - stabilityCenterLocal;

            if (Mathf.Abs(localPos.x) > 0.01f)
            {
                float side = localPos.x > 0f ? 1f : -1f;
                float roll = -side * buildKickRoll;
                boatRb.AddTorque(transform.forward * roll, ForceMode.VelocityChange);
            }
        }

        if (buildKickDown > 0f)
        {
            boatRb.AddForceAtPosition(Vector3.down * buildKickDown, placedWorldPos, ForceMode.Acceleration);
        }
    }

    private Vector2 ComputeRawBiasBlocks()
    {
        int rightCount = 0;
        int leftCount = 0;
        int frontCount = 0;
        int backCount = 0;

        float epsX = blockSizeX * 0.25f;
        float epsZ = blockSizeZ * 0.25f;

        int childCount = blocksRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = blocksRoot.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!IsInLayerMask(child.gameObject.layer, blockLayerMask))
            {
                continue;
            }

            Vector3 local = transform.InverseTransformPoint(child.position) - stabilityCenterLocal;

            if (local.x > epsX)
            {
                rightCount++;
            }
            else if (local.x < -epsX)
            {
                leftCount++;
            }

            if (local.z > epsZ)
            {
                frontCount++;
            }
            else if (local.z < -epsZ)
            {
                backCount++;
            }
        }

        float bx = rightCount - leftCount;
        float bz = frontCount - backCount;

        return new Vector2(bx, bz);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
