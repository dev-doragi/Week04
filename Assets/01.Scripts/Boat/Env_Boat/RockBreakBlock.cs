using UnityEngine;

public class RockBreakBlock : ObjectPoolBase
{
    [Header("Block")]
    [SerializeField] private string blockTag = "Block";
    [SerializeField] private string buildingTag = "Building";

    [Header("Boat Turn On Break")]
    [SerializeField] private float turnPerBreakDegree = 10f;
    [SerializeField] private float turnPower = 1f;
    [SerializeField] private float maxTurnSpeedDegree = 35f;
    [SerializeField] private float centerIgnoreX = 0.05f;

    private void OnCollisionEnter(Collision collision)
    {
        Transform blockRoot = FindBlockRoot(collision.collider.transform);
        if (blockRoot == null)
        {
            return;
        }

        Rigidbody boatRigidbody = collision.rigidbody;
        if (boatRigidbody != null)
        {
            ApplyTurnKick(boatRigidbody, blockRoot.position);
        }

        InGameManager.Instance.boatCollUpdateAction?.Invoke();
        Destroy(blockRoot.gameObject);
    }

    private Transform FindBlockRoot(Transform hitTransform)
    {
        Transform currentTransform = hitTransform;

        while (currentTransform != null)
        {
            if (currentTransform.CompareTag(blockTag) || currentTransform.CompareTag(buildingTag))
            {
                return currentTransform;
            }

            currentTransform = currentTransform.parent;
        }

        return null;
    }

    private void ApplyTurnKick(Rigidbody boatRigidbody, Vector3 blockWorldPosition)
    {
        Vector3 localBlockPosition = boatRigidbody.transform.InverseTransformPoint(blockWorldPosition);

        float side = 0f;
        if (localBlockPosition.x > centerIgnoreX)
        {
            side = 1f;
        }
        else if (localBlockPosition.x < -centerIgnoreX)
        {
            side = -1f;
        }

        if (side == 0f)
        {
            return;
        }

        float targetTurnRadian = turnPerBreakDegree * Mathf.Deg2Rad;
        float damping = Mathf.Max(0.01f, boatRigidbody.angularDamping);
        float turnSpeedKick = targetTurnRadian * damping * turnPower;

        Vector3 turnAxis = Vector3.up;
        boatRigidbody.AddTorque(turnAxis * (-side * turnSpeedKick), ForceMode.VelocityChange);

        Vector3 angularVelocity = boatRigidbody.angularVelocity;
        float maxTurnSpeedRadian = maxTurnSpeedDegree * Mathf.Deg2Rad;
        angularVelocity.y = Mathf.Clamp(angularVelocity.y, -maxTurnSpeedRadian, maxTurnSpeedRadian);
        boatRigidbody.angularVelocity = angularVelocity;
    }


}
