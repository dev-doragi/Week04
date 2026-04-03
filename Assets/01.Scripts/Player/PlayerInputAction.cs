using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputAction : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;

    public bool jump;
    public bool sprint;
    public bool interact;
    public bool drop;
    public bool click;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    public PlayerInput _playerInput;

    private void Start()
    {
        _playerInput.SwitchCurrentActionMap("Player");
    }


    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    public void OnInteract(InputValue value)
    {
        InteractInput(value.isPressed);
    }

    public void OnDrop(InputValue value)
    {
        DropInput(value.isPressed);
    }
    
    public void OnClick(InputValue value)
    {
        ClickInput(value.isPressed);
    }

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    public void InteractInput(bool newInteractState)
    {
        interact = newInteractState;
    }

    public void DropInput(bool newDropState)
    {
        drop = newDropState;
    }

    public void ClickInput(bool newClickState)
    {
        click = newClickState;
    }

    //private void OnApplicationFocus(bool hasFocus)
    //{
    //	SetCursorState(cursorLocked);
    //}

    //private void SetCursorState(bool newState)
    //{
    //	Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    //}
}
