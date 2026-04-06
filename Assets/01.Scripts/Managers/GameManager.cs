using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameEndingManager ending;
    public bool isGameActive = true;

    protected override void Init()
    {
        
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void GameClear()
    {
        GameEndSet();
        ending.WinGame();
    }

    public void DrownGameOver()
    {
        GameEndSet();
        ending.DieByDrowning();
    }

    public void FaildGameOver()
    {
        // 불 꺼져서 게임엔딩?
        GameEndSet();
        ending.FaildGame();
    }

    private void GameEndSet()
    {
        isGameActive = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
