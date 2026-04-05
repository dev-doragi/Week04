using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BoatSteeringController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody boatRb;
    [SerializeField] private Transform wheelObject;

    [Header("Enable")]
    [SerializeField] public bool ControllSteer = false;
    [SerializeField] private bool enableHeadingCorrection = true;

    [Header("Input")]
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private bool invertSteer = false; // 방향 반대

    [Header("Wheel")]
    [SerializeField] private float inputResponseSpeed = 6f; // AD 입력시 조타 입력값이 목표값을 따라가게
    [SerializeField] private float inputReturnSpeed = 4f; // 키를 뗐을때 조타가 0으로 돌아오는 속도
    [SerializeField] private float yawTorqueAccel = 20f; // 보트 Y 축 회전에 가하는 가속도 세기. 클 수록 빠르게 회전
    [SerializeField] private float maxYawSpeedDeg = 45f; // 보트 회전속도 상한 제한
    [SerializeField] private float yawDampingDegPerSec = 30f; // 크면 회전이 빨리죽고 무거워짐

    [Header("Ship Direction")]
    [SerializeField] private float targetWorldYaw = 0f; // 자동보정이 맞추는 월드 기준 Y 각도
    [SerializeField] private float headingDeadZoneDeg = 0.5f; // 목표 각도에서 보정 안하는 허용오차
    [SerializeField] private float headingToSteerGain = 0.03f; // 각도 오자 -> 보정조타 클수록 오차에 민감
    [SerializeField] private float maxAssistSteer = 0.6f; // 자동보정 조타랑 최대치
    [SerializeField] private float assistResponseSpeed = 3f; // 보정 반응 빠름

    [Header("Wheel Visual")]
    [SerializeField] private Vector3 helmEulerCenter = Vector3.zero;
    [SerializeField] private float helmLerpSpeed = 10f;

    private float inputSteer;
    private float assistSteer;
    private float totalSteer;
    [SerializeField] private float helmSpinSpeed = 120f;
    [SerializeField] private float helmReturnSpeed = 90f;
    private float helmCurrentX;


    private void Awake()
    {
        if (boatRb == null)
        {
            boatRb = GetComponent<Rigidbody>();
        }
        helmCurrentX = helmEulerCenter.x;

    }

    private void Update()
    {
        float rawInput = 0f;
        Keyboard keyboard = Keyboard.current;

        if (ControllSteer && keyboard != null)
        {
            if (keyboard[rightKey].isPressed)
            {
                rawInput += 1f;
            }

            if (keyboard[leftKey].isPressed)
            {
                rawInput -= 1f;
            }
        }

        float inputSpeed = Mathf.Abs(rawInput) > 0.001f ? inputResponseSpeed : inputReturnSpeed;
        inputSteer = Mathf.MoveTowards(inputSteer, rawInput, inputSpeed * Time.deltaTime);
        inputSteer = Mathf.Clamp(inputSteer, -1f, 1f);

        float assistTarget = 0f;

        if (enableHeadingCorrection && boatRb != null)
        {
            float currentYaw = Normalize180(boatRb.rotation.eulerAngles.y);
            float yawError = Mathf.DeltaAngle(currentYaw, targetWorldYaw);

            if (Mathf.Abs(yawError) >= headingDeadZoneDeg)
            {
                assistTarget = Mathf.Clamp(yawError * headingToSteerGain, -maxAssistSteer, maxAssistSteer);
            }
        }

        assistSteer = Mathf.MoveTowards(assistSteer, assistTarget, assistResponseSpeed * Time.deltaTime);

        totalSteer = Mathf.Clamp(inputSteer + assistSteer, -1f, 1f);

        if (wheelObject != null)
        {
            if (Mathf.Abs(inputSteer) > 0.001f)
            {
                helmCurrentX += inputSteer * helmSpinSpeed * Time.deltaTime;
            }
            else
            {
                helmCurrentX = Mathf.MoveTowards(
                    helmCurrentX,
                    helmEulerCenter.x,
                    helmReturnSpeed * Time.deltaTime
                );
            }

            wheelObject.localRotation = Quaternion.Euler(
                helmCurrentX,
                helmEulerCenter.y,
                helmEulerCenter.z
            );
        }
    }

    private void FixedUpdate()
    {
        if (boatRb == null)
        {
            return;
        }

        float signedSteer = invertSteer ? -totalSteer : totalSteer;

        if (Mathf.Abs(signedSteer) > 0.0001f)
        {
            boatRb.AddTorque(Vector3.up * (signedSteer * yawTorqueAccel), ForceMode.Acceleration);
        }

        Vector3 angVel = boatRb.angularVelocity;

        float maxYawRad = maxYawSpeedDeg * Mathf.Deg2Rad;
        float clampedYaw = Mathf.Clamp(angVel.y, -maxYawRad, maxYawRad);

        float dampStep = yawDampingDegPerSec * Mathf.Deg2Rad * Time.fixedDeltaTime;
        float dampedYaw = Mathf.MoveTowards(clampedYaw, 0f, dampStep);

        boatRb.angularVelocity = new Vector3(angVel.x, dampedYaw, angVel.z);
    }

    private float Normalize180(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
