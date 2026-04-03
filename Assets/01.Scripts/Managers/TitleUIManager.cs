using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUIManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string _gameSceneName = "02.TestMainScene";

    // 게임 시작
    public void OnClickStart()
    {
        SceneManager.LoadScene(_gameSceneName);
    }

    // 게임 종료
    public void OnClickQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}