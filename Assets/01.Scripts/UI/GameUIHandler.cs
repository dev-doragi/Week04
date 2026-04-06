using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIHandler : MonoBehaviour
{
    public void ClickRestartButton()
    {
        SceneManager.LoadScene(1);
    }

    public void ClickMainMenuButton()
    {
        SceneManager.LoadScene(0);
    }
}
