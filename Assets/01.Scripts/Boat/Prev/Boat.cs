using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Boat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FireIntensityController fireController; // 화력 컨트롤러 참조 추가

    [Header("Move")]
    [SerializeField] float maxForwardAcceleration = 100f; // 최대 가속도
    [SerializeField] float maxSpeedLimit = 6f;            // 최고 속도
    [SerializeField] public bool GameStart = false;
    Rigidbody rb;

    public float CurrentSpeed
    {
        get
        {
            if (rb == null) return 0f;
            Vector3 v = rb.linearVelocity;
            Vector3 h = new Vector3(v.x, 0f, v.z);
            return h.magnitude;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (GameStart && fireController != null)
        {
            // 화력의 정규화된 값 (0.0 ~ 1.0)
            float intensity = fireController.NormalizedIntensity;

            // 화력이 0 이상일 때만 전진 가속도 적용
            if (intensity > 0f)
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                forward.Normalize();

                // 화력에 비례하여 가속도 계산
                float currentAcceleration = maxForwardAcceleration * intensity;
                rb.AddForce(forward * currentAcceleration, ForceMode.Acceleration);
            }

            Vector3 v = rb.linearVelocity;
            Vector3 h = new Vector3(v.x, 0f, v.z);

            // 화력에 비례하여 한계 속도 계산
            float currentMaxSpeed = maxSpeedLimit * intensity;

            // 현재 속도가 계산된 한계 속도를 초과하면 속도 제한
            if (h.magnitude > currentMaxSpeed)
            {
                Vector3 limited = h.normalized * currentMaxSpeed;
                rb.linearVelocity = new Vector3(limited.x, v.y, limited.z);
            }
        }
    }
}