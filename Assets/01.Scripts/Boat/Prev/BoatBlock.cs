using System.Collections.Generic;
using UnityEngine;

public class BoatBlock : MonoBehaviour
{
    [SerializeField] private float mass = 25f;
    [SerializeField] private bool inHull = true;

    public float Mass
    {
        get { return mass; }
    }

    public bool InHull
    {
        get { return inHull; }
        set { inHull = value; }
    }
}
