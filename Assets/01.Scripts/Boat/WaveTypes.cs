using UnityEngine;

public static class WaveTypes
{
    // Traveling sine wave + optional noise.
    public static float SinXWave(
        Vector3 position,
        float speed,
        float scale,
        float waveDistance,
        float noiseStrength,
        float noiseWalk,
        float timeSinceStart)
    {
        float safeWaveDistance = Mathf.Max(0.01f, waveDistance);
        float wave = Mathf.Sin((timeSinceStart * speed + position.z) / safeWaveDistance) * scale;

        float noiseTime = timeSinceStart * Mathf.Max(0f, noiseWalk);
        float noiseSample = Mathf.PerlinNoise(position.x * 0.05f + noiseTime, position.z * 0.05f + noiseTime);
        float noise = (noiseSample - 0.5f) * 2f * noiseStrength;

        return wave + noise;
    }
}
