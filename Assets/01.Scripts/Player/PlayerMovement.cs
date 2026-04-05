using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("Look")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Tool")]
    [SerializeField] private MonoBehaviour currentToolBehaviour;

    private Rigidbody _rb;
    private PlayerInput _playerInput;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isRunning;
    private bool _jumpPressed;

    private float _cameraPitch;
    private IUsableTool _currentTool;

    private bool _isGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _playerInput = GetComponent<PlayerInput>();

        if (cameraRoot == null && Camera.main != null)
        {
            cameraRoot = Camera.main.transform;
        }

        if (currentToolBehaviour != null)
        {
            _currentTool = currentToolBehaviour as IUsableTool;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        _playerInput.onActionTriggered += OnActionTriggered;
    }

    private void OnDisable()
    {
        _playerInput.onActionTriggered -= OnActionTriggered;
    }

    private void Update()
    {
        HandleLook();
        UpdateGrounded();
    }

    private void FixedUpdate()
    {
        return;
        HandleMovement();
        HandleJump();
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action.name == "Move")
        {
            if (context.performed || context.canceled)
            {
                _moveInput = context.ReadValue<Vector2>();
            }
        }
        else if (context.action.name == "Look")
        {
            if (context.performed || context.canceled)
            {
                _lookInput = context.ReadValue<Vector2>();
            }
        }
        else if (context.action.name == "Run")
        {
            if (context.performed)
            {
                _isRunning = true;
            }
            else if (context.canceled)
            {
                _isRunning = false;
            }
        }
        else if (context.action.name == "Jump")
        {
            if (context.performed)
            {
                _jumpPressed = true;
            }
        }
        else if (context.action.name == "Interact")
        {
            if (context.performed)
            {
                TryInteract();
            }
        }
        else if (context.action.name == "UseTool")
        {
            if (context.performed)
            {
                UseCurrentTool();
            }
        }
    }

    private void HandleMovement()
    {
        Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        Vector3 worldDirection = transform.TransformDirection(inputDirection);

        float targetSpeed = _isRunning ? runSpeed : walkSpeed;
        Vector3 targetVelocity = worldDirection * targetSpeed;

        Vector3 currentVelocity = _rb.linearVelocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        float currentRate = inputDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            currentRate * Time.fixedDeltaTime
        );

        _rb.linearVelocity = new Vector3(
            newHorizontalVelocity.x,
            _rb.linearVelocity.y,
            newHorizontalVelocity.z
        );
    }

    private void HandleJump()
    {
        if (_jumpPressed == false)
            return;

        _jumpPressed = false;

        if (_isGrounded == false)
            return;

        Vector3 velocity = _rb.linearVelocity;
        velocity.y = 0f;
        _rb.linearVelocity = velocity;

        _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    private void HandleLook()
    {
        float mouseX = _lookInput.x * mouseSensitivity;
        float mouseY = _lookInput.y * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        _cameraPitch -= mouseY;
        _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }
    }

    private void UpdateGrounded()
    {
        if (groundCheck == null)
        {
            _isGrounded = false;
            return;
        }

        _isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void TryInteract()
    {
        Transform rayOrigin = cameraRoot != null ? cameraRoot : transform;

        if (Physics.Raycast(
            rayOrigin.position,
            rayOrigin.forward,
            out RaycastHit hit,
            interactDistance,
            interactLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(this);
            }
        }
    }

    private void UseCurrentTool()
    {
        if (_currentTool == null)
            return;

        _currentTool.UseTool(this);
    }

    public void SetCurrentTool(IUsableTool tool)
    {
        _currentTool = tool;
    }

    public Transform GetCameraTransform()
    {
        return cameraRoot != null ? cameraRoot : transform;
    }

    public bool IsGrounded()
    {
        return _isGrounded;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Transform rayOrigin = cameraRoot != null ? cameraRoot : transform;
        if (rayOrigin != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * interactDistance);
        }
    }
}