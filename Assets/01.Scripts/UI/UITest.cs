using UnityEngine;
using UnityEngine.InputSystem;

public class UITest : MonoBehaviour
{
    private UIInputAction _input;
    public PlayerInput _playerInput;

    private void Start()
    {
        _playerInput.SwitchCurrentActionMap("UI");
        _input = GetComponent<UIInputAction>();
    }

    private void Update()
    {
        if (_input.pause)
        {
            Debug.Log("ESC 클릭");
            _playerInput.SwitchCurrentActionMap("Player");
            _input.pause = false;
        }
    }
}
