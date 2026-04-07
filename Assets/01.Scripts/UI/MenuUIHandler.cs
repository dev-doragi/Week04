using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUIHandler : MonoBehaviour
{
    public GameObject boat;
    private Boat departBoat;

    private void Start()
    {
        departBoat = boat.GetComponent<Boat>();
    }

    public void ClickStartButton()
    {
        departBoat.GameStart = true;

        StartCoroutine(StartAnimation());
    }

    public void ClickTutorialButton()
    {
        SceneManager.LoadScene(2);
    }

    public void ClickExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터용 종료
#else
            Application.Quit(); // 빌드된 앱 종료
#endif
    }

    IEnumerator StartAnimation()
    {
        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene(1);
    }
}
