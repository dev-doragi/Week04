using UnityEngine;

public class ShipResourceDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BaseResource>(out var res))
        {
            res.IsCollected = true;
            RepoManager.Instance.Register(res);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<BaseResource>(out var res))
        {
            res.IsCollected = false;
            RepoManager.Instance.Unregister(res);
        }
    }
}