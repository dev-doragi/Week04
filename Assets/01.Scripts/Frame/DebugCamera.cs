using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
public class Debugger : MonoBehaviour
{
    public CinemachineCamera[] cams;
    public int activePriority = 20;
    public int inactivePriority = 0;

    public bool IsSteering = false;

    [Header("Debug Target")]
    public GameObject Boat;

    public int CurrentCameraIndex { get; private set; }


    private void Update()
    {

        if (Keyboard.current.gKey.wasPressedThisFrame) 
        {
            Boat.GetComponent<Boat>().GameStart = true;
        }
        else if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            if(!IsSteering)
            {
                IsControllSteerTrigger(IsSteering);
            }
        }
        else if (Keyboard.current.digit1Key.wasPressedThisFrame) ActivateCamera(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) ActivateCamera(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) ActivateCamera(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) ActivateCamera(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) ActivateCamera(4);
    }
    public void ActivateCamera(int index)
    {
        CurrentCameraIndex = index;

        for (int i = 0; i < cams.Length; i++)
        {
            cams[i].Priority = (i == index) ? activePriority : inactivePriority;
        }
    }
    public void IsControllSteerTrigger(bool iscontrol)
    {
        Boat.GetComponent<BoatSteeringController>().ControllSteer = iscontrol;

    }



}
