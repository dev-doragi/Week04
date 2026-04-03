using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatStabilityByBlocks : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private Rigidbody boatRb;
    [SerializeField] private LayerMask blockLayerMask = ~0;

    [Header("Layout (Boat Local)")]
    [SerializeField] private float layoutHalfWidth = 6f;  
    [SerializeField] private float layoutHalfLength = 9f;

    [Header("Persistent Imbalance")]
    [SerializeField] private float rollAccel = 8f;
    [SerializeField] private float pitchAccel = 6f;
    [SerializeField] private float sinkAccel = 20f;
    [SerializeField] private float deadZone = 0.05f;
    [SerializeField] private float flipStart = 0.45f;
    [SerializeField] private float flipBoost = 3f;
    [SerializeField] private float biasLerp = 0.2f;

    [Header("Build Kick")]
    [SerializeField] private float buildKickRoll = 0.5f;
    [SerializeField] private float buildKickDown = 8f;

    private Vector2 smoothedBias;

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

    private void FixedUpdate()
    {
        if (boatRb == null || blocksRoot == null)
        {
            return;
        }

        Vector2 targetBias = ComputeBias01();
        smoothedBias = Vector2.Lerp(smoothedBias, targetBias, Mathf.Clamp01(biasLerp));

        float bx = Mathf.Abs(smoothedBias.x) < deadZone ? 0f : smoothedBias.x;
        float bz = Mathf.Abs(smoothedBias.y) < deadZone ? 0f : smoothedBias.y;

        if (bx == 0f && bz == 0f)
        {
            return;
        }

        float severity = Mathf.Max(Mathf.Abs(bx), Mathf.Abs(bz));
        float unstableMul = 1f;

        if (severity > flipStart)
        {
            float t = Mathf.InverseLerp(flipStart, 1f, severity);
            unstableMul = Mathf.Lerp(1f, flipBoost, t * t);
        }

        Vector3 rollTorque = -transform.forward * (bx * rollAccel * unstableMul);
        Vector3 pitchTorque = transform.right * (bz * pitchAccel * unstableMul);
        Vector3 totalTorque = rollTorque + pitchTorque;

        boatRb.AddTorque(totalTorque, ForceMode.Acceleration);

        Vector3 localPressPoint = new Vector3(bx * layoutHalfWidth, 0f, bz * layoutHalfLength);
        Vector3 worldPressPoint = transform.TransformPoint(localPressPoint);
        float press = ((Mathf.Abs(bx) + Mathf.Abs(bz)) * 0.5f) * sinkAccel * unstableMul;

        boatRb.AddForceAtPosition(Vector3.down * press, worldPressPoint, ForceMode.Acceleration);
    }

    public void NotifyBlockPlaced(Vector3 placedWorldPos)
    {
        if (boatRb == null)
        {
            return;
        }

        Vector3 localPos = transform.InverseTransformPoint(placedWorldPos);

        if (Mathf.Abs(localPos.x) > 0.01f)
        {
            float side = localPos.x > 0f ? 1f : -1f;
            float roll = -side * buildKickRoll;
            boatRb.AddTorque(transform.forward * roll, ForceMode.VelocityChange);
        }

        boatRb.AddForceAtPosition(Vector3.down * buildKickDown, placedWorldPos, ForceMode.Acceleration);
    }

    private Vector2 ComputeBias01()
    {
        int count = 0;
        float sumX = 0f;
        float sumZ = 0f;

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
            sumX += local.x;
            sumZ += local.z;
            count++;
        }

        if (count == 0)
        {
            return Vector2.zero;
        }

        float avgX = sumX / count;
        float avgZ = sumZ / count;

        float bx = Mathf.Clamp(avgX / Mathf.Max(0.001f, layoutHalfWidth), -1f, 1f);
        float bz = Mathf.Clamp(avgZ / Mathf.Max(0.001f, layoutHalfLength), -1f, 1f);

        return new Vector2(bx, bz);
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
