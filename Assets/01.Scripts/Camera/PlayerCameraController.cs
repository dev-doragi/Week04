using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

public class PlayerCameraController : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("카메라가 따라갈 Cinemachine 가상 카메라에 설정된 추적 대상")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("카메라를 위로 이동할 수 있는 최대 각도 (도 단위)")]
    public float TopClamp = 70.0f;

    [Tooltip("카메라를 아래로 이동할 수 있는 최대 각도 (도 단위)")]
    public float BottomClamp = -30.0f;

    [Tooltip("카메라를 재정의하기 위한 추가 각도. 잠금 시 카메라 위치 미세 조정에 유용함")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("카메라의 회전 감도")]
    public float RotationSpeed = 1.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // 시네머신 피치(상하) 회전값
    private float _cinemachineTargetPitch;
    [HideInInspector]public float _cinemachineTargetYaw;

    // 좌우 회전 속도
    private float _rotationVelocity;

    private StarterAssetsInputs _input;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;

    private bool IsCurrentDeviceMouse =>
        _playerInput.currentControlScheme == "KeyboardMouse";
#else
        private bool IsCurrentDeviceMouse => false;
#endif

    private const float _threshold = 0.01f;

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#endif
    }

    private void LateUpdate()
    {
        if (CameraManager.instance.isFirstPerson)   
        {
            FirstPersonCameraRotation();    
        }
        else
        {
            ThirdPersonCameraRotation();
        }
    }

    private void FirstPersonCameraRotation()
    {
        // 입력이 있는 경우에만 카메라 회전 처리
        if (_input.look.sqrMagnitude >= _threshold)
        {
            // 마우스 입력에는 Time.deltaTime을 곱하지 않음
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            // 상하 피치 회전값 누적
            _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;

            // 좌우 요 회전속도 계산
            _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

            // 피치 회전값을 상하 한계 내로 클램프
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // 시네머신 카메라 타겟의 로컬 피치(상하) 회전 적용
            CinemachineCameraTarget.transform.localRotation =
                Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, 0.0f, 0.0f);

            // 플레이어 본체를 좌우(요)로 회전
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void ThirdPersonCameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}
