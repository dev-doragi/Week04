using UnityEngine;

public class Island : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance.isGameActive)
            {
                GameManager.Instance.GameClear();
            }
        }
    }
}
