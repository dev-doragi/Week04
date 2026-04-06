using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatPhysics : MonoBehaviour
{
    [Header("References")]
    public GameObject boatMeshObj;
    public GameObject underWaterObj;
    public GameObject aboveWaterObj;

    [Header("Mass / Balance")]
    public Vector3 centerOfMass;
    [SerializeField] private bool overrideCenterOfMass = true;

    [Header("Fluid")]
    [SerializeField] private float rhoWater = BoatPhysicsMath.RHO_OCEAN_WATER;
    [SerializeField] private float rhoAir = BoatPhysicsMath.RHO_AIR;
    [SerializeField] private float airResistanceC_r = 0.8f;
    [SerializeField] private float buoyancyForceMultiplier = 1f;

    [Header("Debug Mesh")]
    [SerializeField] private bool drawUnderWaterMesh = true;
    [SerializeField] private bool drawAboveWaterMesh = false;

    private ModifyBoatMesh modifyBoatMesh;
    private Mesh underWaterMesh;
    private Mesh aboveWaterMesh;
    private Rigidbody boatRB;

    void Awake()
    {
        boatRB = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (boatMeshObj == null)
        {
            boatMeshObj = gameObject;
        }

        if (underWaterObj == null)
        {
            Debug.LogError("BoatPhysics: underWaterObj is not assigned.");
            enabled = false;
            return;
        }

        MeshFilter underFilter = underWaterObj.GetComponent<MeshFilter>();
        if (underFilter == null)
        {
            Debug.LogError("BoatPhysics: underWaterObj needs MeshFilter.");
            enabled = false;
            return;
        }

        underWaterMesh = underFilter.mesh;

        if (aboveWaterObj != null)
        {
            MeshFilter aboveFilter = aboveWaterObj.GetComponent<MeshFilter>();
            if (aboveFilter != null)
            {
                aboveWaterMesh = aboveFilter.mesh;
            }
        }

        modifyBoatMesh = new ModifyBoatMesh(boatMeshObj, underWaterObj, aboveWaterObj, boatRB);
    }

    void Update()
    {
        if (modifyBoatMesh == null)
        {
            return;
        }

        if (underWaterMesh != null)
        {
            modifyBoatMesh.DisplayMesh(underWaterMesh, "UnderWater Mesh", modifyBoatMesh.underWaterTriangleData);
        }

        if (drawAboveWaterMesh && aboveWaterMesh != null)
        {
            modifyBoatMesh.DisplayMesh(aboveWaterMesh, "AboveWater Mesh", modifyBoatMesh.aboveWaterTriangleData);
        }
    }

    void FixedUpdate()
    {
        if (modifyBoatMesh == null)
        {
            return;
        }
        modifyBoatMesh.GenerateUnderwaterMesh();

        if (overrideCenterOfMass)
        {
            boatRB.centerOfMass = centerOfMass;
        }

        if (modifyBoatMesh.underWaterTriangleData.Count > 0)
        {
            AddUnderWaterForces();
        }

        if (modifyBoatMesh.aboveWaterTriangleData.Count > 0)
        {
            AddAboveWaterForces();
        }
    }

    private void AddUnderWaterForces()
    {
        float underWaterLength = Mathf.Max(0.01f, modifyBoatMesh.CalculateUnderWaterLength());
        float Cf = BoatPhysicsMath.ResistanceCoefficient(rhoWater, boatRB.linearVelocity.magnitude, underWaterLength);

        List<SlammingForceData> slammingForceData = modifyBoatMesh.slammingForceData;
        CalculateSlammingVelocities(slammingForceData);

        float boatArea = Mathf.Max(0.01f, modifyBoatMesh.boatArea);
        float boatMass = Mathf.Max(0.01f, boatRB.mass);

        List<int> indexOfOriginalTriangle = modifyBoatMesh.indexOfOriginalTriangle;
        List<TriangleData> underWaterTriangleData = modifyBoatMesh.underWaterTriangleData;

        int submergedTriangleCount = underWaterTriangleData.Count;

        for (int i = 0; i < submergedTriangleCount; i++)
        {
            TriangleData triangleData = underWaterTriangleData[i];

            Vector3 forceToAdd = Vector3.zero;
            forceToAdd += BoatPhysicsMath.BuoyancyForce(rhoWater, triangleData) * buoyancyForceMultiplier;
            forceToAdd += BoatPhysicsMath.ViscousWaterResistanceForce(rhoWater, triangleData, Cf);
            forceToAdd += BoatPhysicsMath.PressureDragForce(triangleData);

            if (i < indexOfOriginalTriangle.Count)
            {
                int originalTriangleIndex = indexOfOriginalTriangle[i];

                if (originalTriangleIndex >= 0 && originalTriangleIndex < slammingForceData.Count)
                {
                    SlammingForceData slammingData = slammingForceData[originalTriangleIndex];
                    forceToAdd += BoatPhysicsMath.SlammingForce(slammingData, triangleData, boatArea, boatMass);
                }
            }

            boatRB.AddForceAtPosition(forceToAdd, triangleData.center, ForceMode.Force);
        }
    }

    private void AddAboveWaterForces()
    {
        List<TriangleData> aboveWaterTriangleData = modifyBoatMesh.aboveWaterTriangleData;

        for (int i = 0; i < aboveWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = aboveWaterTriangleData[i];
            Vector3 forceToAdd = BoatPhysicsMath.AirResistanceForce(rhoAir, triangleData, airResistanceC_r);
            boatRB.AddForceAtPosition(forceToAdd, triangleData.center, ForceMode.Force);
        }
    }

    private void CalculateSlammingVelocities(List<SlammingForceData> slammingForceData)
    {
        for (int i = 0; i < slammingForceData.Count; i++)
        {
            SlammingForceData data = slammingForceData[i];

            data.previousVelocity = data.velocity;

            Vector3 centerWorld = transform.TransformPoint(data.triangleCenter);
            data.velocity = BoatPhysicsMath.GetTriangleVelocity(boatRB, centerWorld);
        }
    }
}
