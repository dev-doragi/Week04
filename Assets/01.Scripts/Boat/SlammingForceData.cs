using UnityEngine;

public class SlammingForceData
{
    // Original triangle data
    public float originalArea;
    public Vector3 triangleCenter; // local-space center of original triangle

    // Submerged state (current vs previous frame)
    public float submergedArea;
    public float previousSubmergedArea;

    // Velocity at original triangle center (current vs previous frame)
    public Vector3 velocity;
    public Vector3 previousVelocity;
}
