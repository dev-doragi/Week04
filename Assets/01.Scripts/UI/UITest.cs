using UnityEngine;
using UnityEngine.InputSystem;

public class UITest : MonoBehaviour
{
    private UIInputAction _input;

    private void Start()
    {
        _input = GetComponent<UIInputAction>();
    }

    private void Update()
    {
        if (_input.pause)
        {
            Debug.Log("ESC 클릭");
            _input.pause = false;
        }
    }
}
