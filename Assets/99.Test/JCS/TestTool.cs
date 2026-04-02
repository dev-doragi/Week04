using UnityEngine;

public class TestTool : MonoBehaviour, IUsableTool
{
    [SerializeField] private float useDistance = 2f;
    [SerializeField] private LayerMask hitLayerMask = ~0;

    public void UseTool(PlayerMovement player)
    {
        if (player == null)
            return;

        Transform cameraTransform = player.GetCameraTransform();

        if (Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit hit,
            useDistance,
            hitLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"{gameObject.name} 사용 성공 : {hit.collider.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name} 사용");
        }
    }
}