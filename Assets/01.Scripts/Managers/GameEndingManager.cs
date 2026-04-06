using UnityEngine;
using UnityEngine.Playables;

public class GameEndingManager : MonoBehaviour
{
    public PlayableDirector clearTimeline;
    public PlayableDirector drownOverTimeline;
    public PlayableDirector faildOverTimeline;

    public GameObject inGameUI;
    public GameObject inGameCameras;

    private void Start()
    {
        // 테스트용
        WinGame();
    }

    public void WinGame()
    {
        SetEndingCutscene();
        clearTimeline.Play();
    }

    public void DieByDrowning()
    {
        SetEndingCutscene();
        drownOverTimeline.Play();
    }

    public void FaildGame()
    {
        SetEndingCutscene();
        faildOverTimeline.Play();
    }

    private void SetEndingCutscene()
    {
        inGameUI.SetActive(false);
        inGameCameras.SetActive(false);
    }
}
