using UnityEngine;

public class ShipResourceDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BaseResource>(out var res))
        {
            if (!res.IsCollected) return;
            RepoManager.Instance.Register(res);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<BaseResource>(out var res))
        {
            if (res.IsEquipped) return;
            RepoManager.Instance.Unregister(res);
        }
    }
}