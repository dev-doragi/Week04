using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    public PlayerEntity player;

    public int activePriority = 20;
    public int inactivePriority = 10;

    [Space(10)]
    public CinemachineBrain mainBrain;
    public CinemachineCamera fpCamera;
    public CinemachineCamera tpCamera;
    public GameObject overlayCamera;

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
            overlayCamera.SetActive(false);
        }
        else
        {
            fpCamera.Priority = activePriority;
            tpCamera.Priority = inactivePriority;
            StartCoroutine(WaitAndEnableOverlay());
        }
    }

    IEnumerator WaitAndEnableOverlay()
    {
        yield return null;

        while(mainBrain.IsBlending)
        {
            yield return null;
        }

        overlayCamera.SetActive(true);
    }
}
