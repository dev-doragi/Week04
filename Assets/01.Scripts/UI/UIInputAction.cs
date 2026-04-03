using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputAction : MonoBehaviour
{
    public PlayerInput _playerInput;
    public bool pause;

    private void Start()
    {
        _playerInput.SwitchCurrentActionMap("UI");
    }

    public void OnPause(InputValue value)
    {
        PauseInput(value.isPressed);
    }

    public void PauseInput(bool newPauseState)
    {
        pause = newPauseState;
    }

}
