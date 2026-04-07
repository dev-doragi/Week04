using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIHandler : MonoBehaviour
{
    public void ClickRestartButton()
    {
        SceneManager.LoadScene(1);
        GameManager.Instance.isGameActive = true;
    }

    public void ClickMainMenuButton()
    {
        SceneManager.LoadScene(0);
        GameManager.Instance.isGameActive = true;
    }
    public void ClickTutorialRestartButton()
    {
        SceneManager.LoadScene(2);
        GameManager.Instance.isGameActive = true;
    }
}
