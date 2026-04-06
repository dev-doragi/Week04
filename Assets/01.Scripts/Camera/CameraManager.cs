using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public PlayerEntity player;

    public int activePriority = 20;
    public int inactivePriority = 10;

    [Space(10)]
    public CinemachineCamera fpCamera;
    public CinemachineCamera tpCamera;

    [Space(10)]
    public bool isFirstPerson = true;

    [SerializeField] private ePlayerState interactionState;

    private void Awake()
    {
        instance = this;

        isFirstPerson = true;
        fpCamera.Priority = activePriority;
        tpCamera.Priority = inactivePriority;
    }

    private void Update()
    {
        if (player.InputLock)
        {
            fpCamera.Priority = inactivePriority;
            tpCamera.Priority = activePriority;
        }
        else
        {
            fpCamera.Priority = activePriority;
            tpCamera.Priority = inactivePriority;
        }
    }
}
