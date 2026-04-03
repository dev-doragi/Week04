using UnityEngine;

public class DebugPhysics : MonoBehaviour
{
    public static DebugPhysics current;

    [Header("Pressure Drag")]
    public float velocityReference = 5f;
    public float C_PD1 = 10f;
    public float C_PD2 = 10f;
    public float f_P = 0.5f;

    [Header("Suction Drag")]
    public float C_SD1 = 10f;
    public float C_SD2 = 10f;
    public float f_S = 0.5f;

    [Header("Slamming")]
    public float p = 2f;
    public float acc_max = 30f;
    public float slammingCheat = 1f;

    void Awake()
    {
        current = this;
    }

    void OnDestroy()
    {
        if (current == this)
        {
            current = null;
        }
    }
}
