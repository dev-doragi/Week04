using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName = "MainScene";
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag))
            return;

        SceneManager.LoadScene(sceneName);
    }
}