using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PriorityTest : MonoBehaviour
{
    public CinemachineCamera topdownCamera;

    public int activePriority = 20;
    public int inactivePriority = 10;

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            topdownCamera.Priority = activePriority;
        }
        else if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            topdownCamera.Priority = inactivePriority;
        }
    }
}
