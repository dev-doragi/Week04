using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class GameEndingManager : MonoBehaviour
{
    [Header("Timelines")]
    public PlayableDirector clearTimeline;
    public PlayableDirector drownOverTimeline;
    public PlayableDirector faildOverTimeline;

    [Header("Timeline Cameras")]
    public GameObject clearSceneCamera;
    public GameObject drownSceneCamera;
    public GameObject faildSceneCamera;

    [Header("Remove Objects")]
    public GameObject inGameUI;
    public GameObject inGameCameras;

    [Header("In Game Objects")]
    public GameObject boat;
    public GameObject player;

    [Header("Timeline Objects")]
    public GameObject islandCopy;
    public GameObject boatCopy;
    public Transform boatPos;

    private void Start()
    {
        clearSceneCamera.SetActive(false);
        drownSceneCamera.SetActive(false);
        faildSceneCamera.SetActive(false);

        // 테스트용
        //WinGame();
    }

    private void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            WinGame(); 
        }
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DieByDrowning();
        }
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            FaildGame();
        }
    }

    public void WinGame()
    {
        SetEndingCutscene();

        clearSceneCamera.SetActive(true);

        var rb = boat.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        rb = player.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        boat.transform.position = boatPos.position;
        islandCopy.SetActive(true);
        clearTimeline.Play();
    }

    public void DieByDrowning()
    {
        SetEndingCutscene();

        boat.SetActive(false);
        boatCopy.SetActive(true);

        drownSceneCamera.SetActive(true);
        drownOverTimeline.Play();
    }

    public void FaildGame()
    {
        SetEndingCutscene();
        faildSceneCamera.SetActive(true);

        var rb = boat.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        rb = player.GetComponent<Rigidbody>();
        //rb.isKinematic = true;

        boat.transform.position = boatPos.position;

        faildOverTimeline.Play();
    }

    private void SetEndingCutscene()
    {
        inGameUI.SetActive(false);
        inGameCameras.SetActive(false);
    }
}
