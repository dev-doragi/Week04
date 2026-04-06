using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBuoyancy : MonoBehaviour
{
    [Header("Refs")]
    private WaterController waterController;

    [Header("Float Points (Local)")]
    private Vector3[] localFloatPoints =
    {
        new Vector3(-0.2f, -0.15f, -0.2f),
        new Vector3( 0.2f, -0.15f, -0.2f),
        new Vector3(-0.2f, -0.15f,  0.2f),
        new Vector3( 0.2f, -0.15f,  0.2f)
    };

    [Header("Buoyancy")]
    [SerializeField] private float buoyancyAcceleration = 6f; //튀어오르는세기
    private float maxSubmergeDepth = 0.35f; // 잠김깊이

    [Header("Damping")]
    private float airLinearDamping = 0.05f; //공기저항
    private float waterLinearDamping = 2.5f; // 물저항
    private float airAngularDamping = 0.05f; //공기회전저항
    private float waterAngularDamping = 1.8f; //물속회전저항

    private Rigidbody rb;
    private float submergedFraction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (waterController == null)
        {
            waterController = WaterController.current;
        }
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic)
        {
            return;
        }

        if (waterController == null)
        {
            waterController = WaterController.current;
            if (waterController == null || !waterController.isActiveAndEnabled)
            {
                return;
            }
        }

        if (localFloatPoints == null || localFloatPoints.Length == 0)
        {
            return;
        }

        float safeDepth = Mathf.Max(0.001f, maxSubmergeDepth);
        float submerged = 0f;

        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(localFloatPoints[i]);
            float waterY = waterController.GetWaveYPos(worldPoint, Time.time);
            float depth = waterY - worldPoint.y;

            if (depth <= 0f)
            {
                continue;
            }

            float k = Mathf.Clamp01(depth / safeDepth);
            submerged += k;

            rb.AddForceAtPosition(Vector3.up * (buoyancyAcceleration * k), worldPoint, ForceMode.Acceleration);
        }

        submerged = submerged / localFloatPoints.Length;
        submergedFraction = Mathf.Lerp(submergedFraction, submerged, 0.25f);

        rb.linearDamping = Mathf.Lerp(airLinearDamping, waterLinearDamping, submergedFraction);
        rb.angularDamping = Mathf.Lerp(airAngularDamping, waterAngularDamping, submergedFraction);
    }

    private void OnDrawGizmosSelected()
    {
        if (localFloatPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 0; i < localFloatPoints.Length; i++)
        {
            Vector3 p = transform.TransformPoint(localFloatPoints[i]);
            Gizmos.DrawSphere(p, 0.04f);
        }
    }
}
