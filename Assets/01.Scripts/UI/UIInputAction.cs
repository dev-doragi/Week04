using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputAction : MonoBehaviour
{
    public bool pause;   

    public void OnPause(InputValue value)
    {
        PauseInput(value.isPressed);
    }

    public void PauseInput(bool newPauseState)
    {
        pause = newPauseState;
    }

}
