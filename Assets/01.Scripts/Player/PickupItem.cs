using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    public void Interact(PlayerMovement player)
    {
        Debug.Log($"{gameObject.name} 아이템 획득");
        Destroy(gameObject);
    }
}