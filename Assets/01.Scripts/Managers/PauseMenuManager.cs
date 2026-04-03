using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _pausePanel;

    [Header("Scene")]
    [SerializeField] private string _titleSceneName = "01.TestTitleScene";

    private bool _isPaused;

    private void Start()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TogglePause();
    }

    public void TogglePause()
    {
        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        if (_pausePanel != null)
            _pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_titleSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}