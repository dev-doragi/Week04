using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleBuoyantObject : MonoBehaviour
{
    [Header("Water")]
    public Transform waterReference;
    public float waterLevelOffset = 0f;

    [Header("Buoyancy")]
    public Vector3[] localFloatPoints = new Vector3[]
    {
        new Vector3(-0.7f, 0f,  1.2f),
        new Vector3( 0.7f, 0f,  1.2f),
        new Vector3(-0.7f, 0f, -1.2f),
        new Vector3( 0.7f, 0f, -1.2f),
    };
    public float pointDepth = 1f;
    public float buoyancyForce = 15f;

    [Header("Damping")]
    public float airLinearDamping = 0.05f;
    public float waterLinearDamping = 1.5f;
    public float airAngularDamping = 0.05f;
    public float waterAngularDamping = 0.8f;

    [Header("Stability")]
    public float uprightTorque = 2f;

    public float SubmergedFraction { get; private set; }

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (localFloatPoints == null || localFloatPoints.Length == 0) return;

        float submergedSum = 0f;
        float safeDepth = Mathf.Max(0.01f, pointDepth);
        int pointCount = localFloatPoints.Length;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(localFloatPoints[i]);
            float waterY = GetWaterHeightAt(worldPoint);
            float depth = waterY - worldPoint.y;

            if (depth <= 0f) continue;

            float k = Mathf.Clamp01(depth / safeDepth);
            submergedSum += k;
            rb.AddForceAtPosition(Vector3.up * (buoyancyForce * k), worldPoint, ForceMode.Acceleration);
        }

        float submerged = submergedSum / pointCount;
        SubmergedFraction = Mathf.Lerp(SubmergedFraction, submerged, 0.25f);

        rb.linearDamping = Mathf.Lerp(airLinearDamping, waterLinearDamping, SubmergedFraction);
        rb.angularDamping = Mathf.Lerp(airAngularDamping, waterAngularDamping, SubmergedFraction);

        if (SubmergedFraction > 0f && uprightTorque > 0f)
        {
            Vector3 correction = Vector3.Cross(transform.up, Vector3.up);
            rb.AddTorque(correction * uprightTorque, ForceMode.Acceleration);
        }
    }

    public float GetWaterHeightAt(Vector3 worldPoint)
    {
        float baseY = 0f;
        if (waterReference != null) baseY = waterReference.position.y;
        return baseY + waterLevelOffset;
    }

    void OnDrawGizmosSelected()
    {
        if (localFloatPoints == null) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(localFloatPoints[i]);
            float waterY = GetWaterHeightAt(worldPoint);
            Vector3 waterPoint = new Vector3(worldPoint.x, waterY, worldPoint.z);

            Gizmos.DrawSphere(worldPoint, 0.06f);
            Gizmos.DrawLine(worldPoint, waterPoint);
        }
    }
}
