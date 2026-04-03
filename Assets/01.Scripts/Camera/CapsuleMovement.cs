using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CapsuleMovement : MonoBehaviour
{
    [Header("이동")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float RotationSpeed = 1.0f;
    public float SpeedChangeRate = 10.0f;

    [Header("점프")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;

    [Header("접지 체크")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("카메라")]
    public GameObject CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    // 카메라
    private float _cinemachineTargetPitch;

    // 이동
    private float _speed;
    private float _rotationVelocity;

    // 점프/중력
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    private PlayerInput _playerInput;
    private Rigidbody _rb;
    private PlayerInputAction _input;
    private GameObject _mainCamera;

    private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";
    private const float _threshold = 0.01f;

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerInputAction>();
        _playerInput = GetComponent<PlayerInput>();

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void FixedUpdate()
    {
        GroundedCheck();
        JumpAndGravity();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    // ── 카메라 ────────────────────────────────────────────

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude < _threshold) return;

        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

        _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
        _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.localRotation =
            Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

        //transform.Rotate(Vector3.up * _rotationVelocity);
        Quaternion deltaRotation = Quaternion.Euler(0f, _rotationVelocity, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRotation);
    }

    // ── 이동 ────────────────────────────────────────────

    private void Move()
    {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_rb.linearVelocity.x, 0.0f, _rb.linearVelocity.z).magnitude;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // 목표 속도로 부드럽게 가속/감속
        float speedOffset = 0.1f;
        bool needsSpeedChange = currentHorizontalSpeed < targetSpeed - speedOffset ||
                                 currentHorizontalSpeed > targetSpeed + speedOffset;

        _speed = needsSpeedChange
            ? Mathf.Round(Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate) * 1000f) / 1000f
            : targetSpeed;

        Vector3 inputDirection = Vector3.zero;

        if (_input.move != Vector2.zero)
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

        Vector3 horizontalVelocity = inputDirection.normalized * _speed;
        _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
    }

    // ── 점프/중력 ────────────────────────────────────────

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout; 
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                float jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVelocity, _rb.linearVelocity.z);

                _input.jump = false;
                _jumpTimeoutDelta = JumpTimeout;
            }

            if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0.0f)
                _fallTimeoutDelta -= Time.deltaTime;

            _input.jump = false;
        }
    }

    // ── 접지 체크 ────────────────────────────────────────

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x,
            transform.position.y - GroundedOffset, transform.position.z);

        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);
    }

    // ── 유틸 ────────────────────────────────────────────

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Grounded
            ? new Color(0.0f, 1.0f, 0.0f, 0.35f)
            : new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }
}

