using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("캐릭터의 이동 속도 (m/s)")]
    public float MoveSpeed = 2.0f;

    [Tooltip("캐릭터의 달리기 속도 (m/s)")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("가속 및 감속")]
    public float SpeedChangeRate = 10.0f;

    public AudioSource AudioFootsteps;
    public AudioSource LandingAudio;
    public AudioSource AudioFoley;
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("플레이어가 점프할 수 있는 높이")]
    public float JumpHeight = 1.2f;

    [Tooltip("캐릭터는 자체 중력 값을 사용합니다. 엔진 기본값은 -9.81f입니다")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("다시 점프할 수 있기까지 필요한 대기 시간. 0f로 설정하면 즉시 재점프 가능")]
    public float JumpTimeout = 0.50f;

    [Tooltip("낙하 상태로 진입하기까지 필요한 대기 시간. 계단을 내려갈 때 유용함")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("캐릭터의 접지 여부. CharacterController 내장 접지 체크와는 별개입니다")]
    public bool Grounded = true;

    [Tooltip("거친 지면에서 유용한 오프셋")]
    public float GroundedOffset = -0.14f;

    [Tooltip("접지 체크의 반경. CharacterController의 반경과 일치해야 합니다")]
    public float GroundedRadius = 0.28f;

    [Tooltip("캐릭터가 지면으로 사용하는 레이어")]
    public LayerMask GroundLayers;

    // 플레이어
    private float _speed;
    private float _targetRotation = 0.0f;
    private float _animationBlend;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private float _rotationVelocity;

    // 타임아웃 델타타임
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // 애니메이션 ID
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private StarterAssetsInputs _input;
    private GameObject _mainCamera;

    // 카메라 스크립트에서 회전 값을 받아오기 위한 참조
    // PlayerCameraController가 같은 오브젝트에 붙어있어야 합니다
    private PlayerCameraController _cameraController;

    private const float _threshold = 0.01f;
    private bool _hasAnimator;

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();
        _cameraController = GetComponent<PlayerCameraController>();

#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets 패키지의 종속성이 누락되었습니다. Tools/Starter Assets/Reinstall Dependencies를 사용하여 수정하세요");
#endif

        AssignAnimationIDs();

        // 시작 시 타임아웃 초기화
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        // 오프셋을 적용하여 구체 위치 설정
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // 캐릭터를 사용하는 경우 애니메이터 업데이트
        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void Move()
    {
        // 이동 속도, 달리기 속도, 스프린트 입력 여부에 따라 목표 속도 설정
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // 입력이 없으면 목표 속도를 0으로 설정
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // 플레이어의 현재 수평 속도 참조
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // 목표 속도로 가속 또는 감속
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // 선형이 아닌 곡선형 결과를 생성하여 더 자연스러운 속도 변화를 제공
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);

            // 속도를 소수점 셋째 자리로 반올림
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // 여기에 move()함수 넣을 것
        if (CameraManager.instance.isFirstPerson)
        {
            FirstPersonMove();
        }
        else
        {
            ThirdPersonMove();
        }

        // 캐릭터를 사용하는 경우 애니메이터 업데이트
        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void FirstPersonMove()
    {
        // 입력 방향 정규화
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // 이동 입력이 있을 경우 플레이어 기준으로 이동 방향 계산 (1인칭)
        if (_input.move != Vector2.zero)
        {
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        // 플레이어 이동
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void ThirdPersonMove()
    {
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                RotationSmoothTime);

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // 낙하 타임아웃 타이머 초기화
            _fallTimeoutDelta = FallTimeout;

            // 캐릭터를 사용하는 경우 애니메이터 업데이트
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // 접지 상태에서 수직 속도가 무한히 감소하지 않도록 방지
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // 점프
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // 원하는 높이에 도달하기 위해 필요한 속도 = sqrt(H * -2 * G)
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                // 캐릭터를 사용하는 경우 애니메이터 업데이트
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // 점프 타임아웃
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // 점프 타임아웃 타이머 초기화
            _jumpTimeoutDelta = JumpTimeout;

            // 낙하 타임아웃
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                // 캐릭터를 사용하는 경우 애니메이터 업데이트
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // 접지되지 않은 상태에서는 점프 불가
            _input.jump = false;
        }

        // 터미널 속도 미만일 경우 시간에 따라 중력 적용
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // 선택 시 접지 콜라이더의 위치와 반경에 맞는 기즈모를 그림
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {

            if (AudioFootsteps != null)
                AudioFootsteps.Play();
            if (AudioFoley != null)
                AudioFoley.Play();
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (LandingAudio != null)
                LandingAudio.Play();

        }
    }

}
