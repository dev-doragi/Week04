using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public float slowdownFactor = 0.25f;
    public float slowdownLength = 2f;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.isPressed)
        {
            Time.timeScale = slowdownFactor;
            Time.fixedDeltaTime = Time.timeScale * 0.02f;
        }
        else
        {
            Time.timeScale += (1f / slowdownLength) * Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);

            if (Time.timeScale >= 1f)
            {
                Time.fixedDeltaTime = 0.02f;
            }
            else
            {
                Time.fixedDeltaTime = Time.timeScale * 0.02f;
            }
        }
    }
}