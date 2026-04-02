using UnityEngine;

public class BoatBlock : MonoBehaviour
{
    public Vector3Int cell;
    public float mass = 1f;
    public bool isCore;

    void OnValidate()
    {
        cell = Vector3Int.RoundToInt(transform.localPosition);
    }
}