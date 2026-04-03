using UnityEngine;

public struct TriangleData
{
    // World-space triangle points
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;

    public Vector3 center;
    public float distanceToSurface;
    public Vector3 normal;
    public float area;

    public Vector3 velocity;
    public Vector3 velocityDir;
    public float cosTheta;

    public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3, Rigidbody boatRB, float timeSinceStart)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;

        this.center = (p1 + p2 + p3) / 3f;

        WaterController water = WaterController.current;
        if (water != null)
        {
            this.distanceToSurface = Mathf.Abs(water.DistanceToWater(this.center, timeSinceStart));
        }
        else
        {
            this.distanceToSurface = 0f;
        }

        Vector3 rawNormal = Vector3.Cross(p2 - p1, p3 - p1);
        if (rawNormal.sqrMagnitude > 0.000001f)
        {
            this.normal = rawNormal.normalized;
        }
        else
        {
            this.normal = Vector3.up;
        }

        this.area = BoatPhysicsMath.GetTriangleArea(p1, p2, p3);

        if (boatRB != null)
        {
            this.velocity = BoatPhysicsMath.GetTriangleVelocity(boatRB, this.center);
        }
        else
        {
            this.velocity = Vector3.zero;
        }

        float speed = this.velocity.magnitude;
        if (speed > 0.0001f)
        {
            this.velocityDir = this.velocity / speed;
            this.cosTheta = Vector3.Dot(this.velocityDir, this.normal);
        }
        else
        {
            this.velocityDir = Vector3.zero;
            this.cosTheta = 0f;
        }
    }
}
