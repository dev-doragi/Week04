using UnityEngine;

public static class BoatPhysicsMath
{
    // Densities [kg/m^3]
    public const float RHO_WATER = 1000f;
    public const float RHO_OCEAN_WATER = 1027f;
    public const float RHO_SUNFLOWER_OIL = 920f;
    public const float RHO_MILK = 1035f;
    public const float RHO_AIR = 1.225f;
    public const float RHO_HELIUM = 0.164f;
    public const float RHO_GOLD = 19300f;

    // Drag coefficients
    public const float C_d_flat_plate_perpendicular_to_flow = 1.28f;

    private const float EPSILON = 0.0001f;

    public static Vector3 GetTriangleVelocity(Rigidbody boatRB, Vector3 triangleCenter)
    {
        if (boatRB == null)
        {
            return Vector3.zero;
        }

        return boatRB.GetPointVelocity(triangleCenter);
    }

    public static float GetTriangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 cross = Vector3.Cross(p2 - p1, p3 - p1);
        return cross.magnitude * 0.5f;
    }

    public static Vector3 BuoyancyForce(float rho, TriangleData triangleData)
    {
        Vector3 buoyancyForce = rho * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;
        buoyancyForce.x = 0f;
        buoyancyForce.z = 0f;

        return CheckForceIsValid(buoyancyForce, "Buoyancy");
    }

    public static Vector3 ViscousWaterResistanceForce(float rho, TriangleData triangleData, float Cf)
    {
        if (Cf <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 normal = triangleData.normal;
        Vector3 velocity = triangleData.velocity;

        float normalSpeed = Vector3.Dot(velocity, normal);
        Vector3 velocityTangent = velocity - normalSpeed * normal;

        float tangentSpeed = velocityTangent.magnitude;
        if (tangentSpeed <= EPSILON)
        {
            return Vector3.zero;
        }

        Vector3 tangentDirection = velocityTangent / tangentSpeed;
        Vector3 force = -0.5f * rho * tangentSpeed * tangentSpeed * triangleData.area * Cf * tangentDirection;

        return CheckForceIsValid(force, "Viscous Water Resistance");
    }

    public static float ResistanceCoefficient(float rho, float velocity, float length)
    {
        if (velocity <= EPSILON || length <= EPSILON)
        {
            return 0f;
        }

        float nu = 0.000001f;
        float reynolds = (velocity * length) / nu;

        if (reynolds <= 1f)
        {
            return 0f;
        }

        float denominator = Mathf.Log10(reynolds) - 2f;
        if (Mathf.Abs(denominator) <= EPSILON)
        {
            return 0f;
        }

        float Cf = 0.075f / (denominator * denominator);
        return Mathf.Max(0f, Cf);
    }

    public static Vector3 PressureDragForce(TriangleData triangleData)
    {
        float speed = triangleData.velocity.magnitude;
        if (speed <= EPSILON)
        {
            return Vector3.zero;
        }

        float velocityReference = 1f;
        float C_PD1 = 10f;
        float C_PD2 = 10f;
        float f_P = 0.5f;
        float C_SD1 = 10f;
        float C_SD2 = 10f;
        float f_S = 0.5f;

        if (DebugPhysics.current != null)
        {
            velocityReference = Mathf.Max(0.01f, DebugPhysics.current.velocityReference);
            C_PD1 = DebugPhysics.current.C_PD1;
            C_PD2 = DebugPhysics.current.C_PD2;
            f_P = Mathf.Max(0.01f, DebugPhysics.current.f_P);
            C_SD1 = DebugPhysics.current.C_SD1;
            C_SD2 = DebugPhysics.current.C_SD2;
            f_S = Mathf.Max(0.01f, DebugPhysics.current.f_S);
        }

        float normalizedSpeed = speed / velocityReference;
        float speedTerm = normalizedSpeed + normalizedSpeed * normalizedSpeed;
        Vector3 force;

        if (triangleData.cosTheta > 0f)
        {
            float angleTerm = Mathf.Pow(triangleData.cosTheta, f_P);
            force = -(C_PD1 + C_PD2 * speedTerm) * triangleData.area * angleTerm * triangleData.normal;
        }
        else
        {
            float angleTerm = Mathf.Pow(Mathf.Abs(triangleData.cosTheta), f_S);
            force = (C_SD1 + C_SD2 * speedTerm) * triangleData.area * angleTerm * triangleData.normal;
        }

        return CheckForceIsValid(force, "Pressure Drag");
    }

    public static Vector3 SlammingForce(SlammingForceData slammingData, TriangleData triangleData, float boatArea, float boatMass)
    {
        if (slammingData == null)
        {
            return Vector3.zero;
        }

        if (triangleData.cosTheta < 0f || slammingData.originalArea <= EPSILON)
        {
            return Vector3.zero;
        }

        if (boatArea <= EPSILON || boatMass <= EPSILON)
        {
            return Vector3.zero;
        }

        float dt = Mathf.Max(0.0001f, Time.fixedDeltaTime);

        Vector3 dV = slammingData.submergedArea * slammingData.velocity;
        Vector3 dVPrevious = slammingData.previousSubmergedArea * slammingData.previousVelocity;
        Vector3 accVec = (dV - dVPrevious) / (slammingData.originalArea * dt);

        float acc = accVec.magnitude;
        if (acc <= EPSILON)
        {
            return Vector3.zero;
        }

        float p = 2f;
        float accMax = acc;
        float slammingScale = 1f;

        if (DebugPhysics.current != null)
        {
            p = Mathf.Max(0.1f, DebugPhysics.current.p);
            slammingScale = Mathf.Max(0f, DebugPhysics.current.slammingCheat);

            if (DebugPhysics.current.acc_max > EPSILON)
            {
                accMax = DebugPhysics.current.acc_max;
            }
        }

        Vector3 fStop = boatMass * triangleData.velocity * ((2f * triangleData.area) / boatArea);
        float ratio = Mathf.Clamp01(acc / Mathf.Max(EPSILON, accMax));

        Vector3 slammingForce = -Mathf.Pow(ratio, p) * triangleData.cosTheta * slammingScale * fStop;

        return CheckForceIsValid(slammingForce, "Slamming");
    }

    public static float ResidualResistanceForce()
    {
        return 0f;
    }

    public static Vector3 AirResistanceForce(float rho, TriangleData triangleData, float C_air)
    {
        if (triangleData.cosTheta < 0f)
        {
            return Vector3.zero;
        }

        float speed = triangleData.velocity.magnitude;
        if (speed <= EPSILON)
        {
            return Vector3.zero;
        }

        Vector3 force = -0.5f * rho * speed * triangleData.velocity * triangleData.area * Mathf.Max(0f, C_air);

        return CheckForceIsValid(force, "Air Resistance");
    }

    private static Vector3 CheckForceIsValid(Vector3 force, string forceName)
    {
        bool invalid = float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z) ||
                       float.IsInfinity(force.x) || float.IsInfinity(force.y) || float.IsInfinity(force.z);

        if (invalid)
        {
            Debug.LogWarning(forceName + " force is invalid. Returning zero.");
            return Vector3.zero;
        }

        return force;
    }
}
