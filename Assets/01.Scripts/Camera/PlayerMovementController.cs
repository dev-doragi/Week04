using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
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

    [Header("오디오")]
    public AudioSource AudioFootsteps;
    public AudioSource LandingAudio;
    public AudioSource AudioFoley;
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    // 카메라
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // 이동
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;

    // 점프/중력
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // 애니메이션 ID
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private PlayerInput _playerInput;
    private Animator _animator;
    private Rigidbody _rb;
    private PlayerInputAction _input;
    private GameObject _mainCamera;
    private CinemachineThirdPersonFollow _follow;
    private bool _hasAnimator;

    private bool IsFirstPerson => CameraManager.instance.isFirstPerson;
    private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";
    private const float _threshold = 0.01f;

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _follow = GameObject.Find("FirstPersonCamera").GetComponent<CinemachineThirdPersonFollow>();

        _hasAnimator = TryGetComponent(out _animator);
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<PlayerInputAction>();
        _playerInput = GetComponent<PlayerInput>();

        AssignAnimationIDs();

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

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    // ── 카메라 ────────────────────────────────────────────

    private void CameraRotation()
    {
        if (IsFirstPerson)
            FirstPersonCameraRotation();
        else
            ThirdPersonCameraRotation();
    }

    private void FirstPersonCameraRotation()
    {
        if (_input.look.sqrMagnitude < _threshold) return;

        float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

        _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
        _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.localRotation =
            Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

        // transform.Rotate(Vector3.up * _rotationVelocity);
        Quaternion deltaRotation = Quaternion.Euler(0f, _rotationVelocity, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRotation);
    }

    private void ThirdPersonCameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    // ── 이동 ────────────────────────────────────────────

    private void Move()
    {
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
        //float targetZ = _input.sprint ? 0.2f : 0f;
        //_follow.ShoulderOffset.z = Mathf.Lerp(_follow.ShoulderOffset.z, targetZ, Time.deltaTime * 5f);

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

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        if (IsFirstPerson)
            FirstPersonMove();
        else
            ThirdPersonMove();

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void FirstPersonMove()
    {
        Vector3 inputDirection = Vector3.zero;

        if (_input.move != Vector2.zero)
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

        ApplyHorizontalVelocity(inputDirection.normalized * _speed);
    }

    private void ThirdPersonMove()
    {
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation,
                ref _rotationVelocity, RotationSmoothTime);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        ApplyHorizontalVelocity(targetDirection.normalized * _speed);
    }

    // Y축(중력/점프) 속도는 건드리지 않고 수평 속도만 적용
    private void ApplyHorizontalVelocity(Vector3 horizontalVelocity)
    {
        _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
    }

    // ── 점프/중력 ────────────────────────────────────────

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                float jumpVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, jumpVelocity, _rb.linearVelocity.z);

                if (_hasAnimator) _animator.SetBool(_animIDJump, true);

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
            else if (_hasAnimator)
                _animator.SetBool(_animIDFreeFall, true);

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

        if (_hasAnimator)
            _animator.SetBool(_animIDGrounded, Grounded);
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

    // ── 오디오 이벤트 ────────────────────────────────────

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioFootsteps?.Play();
            AudioFoley?.Play();
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
            LandingAudio?.Play();
    }
}