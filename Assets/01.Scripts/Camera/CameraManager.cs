using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public int activePriority = 20;
    public int inactivePriority = 10;

    [Space(10)]
    public CinemachineCamera fpCamera;
    public CinemachineCamera tpCamera;
    public CinemachineCamera ovCamera;

    [Space(10)]
    public bool isFirstPerson = true;

    private PlayerCameraController camControl;

    private void Awake()
    {
        instance = this;

        camControl = GameObject.Find("Player").GetComponent<PlayerCameraController>();
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 280, 200), "카메라 전환");

        if (GUI.Button(new Rect(20, 55, 260, 50), "1인칭"))
        {
            isFirstPerson = true;
            // camControl.CinemachineCameraTarget = fpCamera.gameObject;
            fpCamera.Priority = activePriority;
            tpCamera.Priority = inactivePriority;
            ovCamera.Priority = inactivePriority;
        }

        if (GUI.Button(new Rect(20, 115, 260, 50), "3인칭"))
        {
            isFirstPerson = false;
            //camControl.CinemachineCameraTarget = tpCamera.gameObject;
            fpCamera.Priority = inactivePriority;
            tpCamera.Priority = activePriority;
            ovCamera.Priority = inactivePriority;
        }

        if (GUI.Button(new Rect(20, 175, 260, 50), "Overview"))
        {
            isFirstPerson = false;
            //camControl.CinemachineCameraTarget = ovCamera.gameObject;
            fpCamera.Priority = inactivePriority;
            tpCamera.Priority = inactivePriority;
            ovCamera.Priority = activePriority;
        }
    }
}
