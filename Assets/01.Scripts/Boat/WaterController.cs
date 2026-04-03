using UnityEngine;

public class WaterController : MonoBehaviour
{
    public static WaterController current;

    [Header("Water Level")]
    public float baseHeight = 0f; // 물 기준 높이 오프셋 ( transform.position.y + baseHeight)
    public bool isMoving = true;

    [Header("Wave")]
    public float scale = 0.1f;
    public float speed = 1.0f;
    public float waveDistance = 1f;

    [Header("Noise")]
    public float noiseStrength = 0.3f;
    public float noiseWalk = 1f;

    void Awake()
    {
        if (current != null && current != this)
        {
            Debug.LogWarning("Multiple WaterController instances found. Replacing previous instance.");
        }

        current = this;
    }

    void OnDestroy()
    {
        if (current == this)
        {
            current = null;
        }
    }

    // World-space water surface height.
    public float GetWaveYPos(Vector3 position, float timeSinceStart)
    {
        float staticLevel = transform.position.y + baseHeight;

        if (!isMoving)
        {
            return staticLevel;
        }

        float waveOffset = WaveTypes.SinXWave(
            position,
            speed,
            scale,
            waveDistance,
            noiseStrength,
            noiseWalk,
            timeSinceStart);

        return staticLevel + waveOffset;
    }

    // Positive above water, negative below water.
    public float DistanceToWater(Vector3 position, float timeSinceStart)
    {
        float waterHeight = GetWaveYPos(position, timeSinceStart);
        return position.y - waterHeight;
    }
}
