using System.Collections.Generic;
using UnityEngine;

public class ModifyBoatMesh
{
    private Transform boatTrans;
    private Vector3[] boatVertices;
    private int[] boatTriangles;
    private Rigidbody boatRB;

    public Vector3[] boatVerticesGlobal;
    private float[] allDistancesToWater;

    private Mesh underWaterMesh;
    public List<TriangleData> underWaterTriangleData = new List<TriangleData>();
    public List<TriangleData> aboveWaterTriangleData = new List<TriangleData>();

    public List<SlammingForceData> slammingForceData = new List<SlammingForceData>();
    public List<int> indexOfOriginalTriangle = new List<int>();
    public float boatArea;

    private float timeSinceStart;

    public ModifyBoatMesh(GameObject boatObj, GameObject underWaterObj, GameObject aboveWaterObj, Rigidbody boatRB)
    {
        this.boatRB = boatRB;

        MeshFilter boatMeshFilter = boatObj.GetComponent<MeshFilter>();
        MeshFilter underMeshFilter = underWaterObj.GetComponent<MeshFilter>();

        boatTrans = boatObj.transform;
        boatVertices = boatMeshFilter.mesh.vertices;
        boatTriangles = boatMeshFilter.mesh.triangles;
        underWaterMesh = underMeshFilter.mesh;

        boatVerticesGlobal = new Vector3[boatVertices.Length];
        allDistancesToWater = new float[boatVertices.Length];

        int originalTriangleCount = boatTriangles.Length / 3;
        for (int i = 0; i < originalTriangleCount; i++)
        {
            slammingForceData.Add(new SlammingForceData());
        }

        CalculateOriginalTrianglesArea();
    }

    public void GenerateUnderwaterMesh()
    {
        aboveWaterTriangleData.Clear();
        underWaterTriangleData.Clear();
        indexOfOriginalTriangle.Clear();

        for (int i = 0; i < slammingForceData.Count; i++)
        {
            slammingForceData[i].previousSubmergedArea = slammingForceData[i].submergedArea;
            slammingForceData[i].submergedArea = 0f;
        }

        WaterController water = WaterController.current;
        if (water == null)
        {
            return;
        }

        timeSinceStart = Time.time;

        for (int i = 0; i < boatVertices.Length; i++)
        {
            Vector3 globalPos = boatTrans.TransformPoint(boatVertices[i]);
            boatVerticesGlobal[i] = globalPos;
            allDistancesToWater[i] = water.DistanceToWater(globalPos, timeSinceStart);
        }

        AddTriangles();
    }

    private void AddTriangles()
    {
        List<VertexData> vertexData = new List<VertexData>(3);
        vertexData.Add(new VertexData());
        vertexData.Add(new VertexData());
        vertexData.Add(new VertexData());

        int triangleCounter = 0;
        for (int i = 0; i < boatTriangles.Length; i += 3)
        {
            if (triangleCounter >= slammingForceData.Count)
            {
                break;
            }

            for (int x = 0; x < 3; x++)
            {
                int vertexIndex = boatTriangles[i + x];
                vertexData[x].distance = allDistancesToWater[vertexIndex];
                vertexData[x].index = x;
                vertexData[x].globalVertexPos = boatVerticesGlobal[vertexIndex];
            }

            bool allAbove = vertexData[0].distance > 0f &&
                            vertexData[1].distance > 0f &&
                            vertexData[2].distance > 0f;

            bool allUnder = vertexData[0].distance < 0f &&
                            vertexData[1].distance < 0f &&
                            vertexData[2].distance < 0f;

            if (allAbove)
            {
                Vector3 p1 = vertexData[0].globalVertexPos;
                Vector3 p2 = vertexData[1].globalVertexPos;
                Vector3 p3 = vertexData[2].globalVertexPos;

                aboveWaterTriangleData.Add(new TriangleData(p1, p2, p3, boatRB, timeSinceStart));
                slammingForceData[triangleCounter].submergedArea = 0f;

                triangleCounter += 1;
                continue;
            }

            if (allUnder)
            {
                Vector3 p1 = vertexData[0].globalVertexPos;
                Vector3 p2 = vertexData[1].globalVertexPos;
                Vector3 p3 = vertexData[2].globalVertexPos;

                underWaterTriangleData.Add(new TriangleData(p1, p2, p3, boatRB, timeSinceStart));
                slammingForceData[triangleCounter].submergedArea = slammingForceData[triangleCounter].originalArea;
                indexOfOriginalTriangle.Add(triangleCounter);

                triangleCounter += 1;
                continue;
            }

            vertexData.Sort(CompareVertexDistanceAscending);
            vertexData.Reverse();

            bool oneAbove = vertexData[0].distance > 0f &&
                            vertexData[1].distance < 0f &&
                            vertexData[2].distance < 0f;

            bool twoAbove = vertexData[0].distance > 0f &&
                            vertexData[1].distance > 0f &&
                            vertexData[2].distance < 0f;

            if (oneAbove)
            {
                AddTrianglesOneAboveWater(vertexData, triangleCounter);
            }
            else if (twoAbove)
            {
                AddTrianglesTwoAboveWater(vertexData, triangleCounter);
            }

            triangleCounter += 1;
        }
    }

    private static int CompareVertexDistanceAscending(VertexData a, VertexData b)
    {
        return a.distance.CompareTo(b.distance);
    }

    private void AddTrianglesOneAboveWater(List<VertexData> vertexData, int triangleCounter)
    {
        Vector3 H = vertexData[0].globalVertexPos;

        int mIndex = vertexData[0].index - 1;
        if (mIndex < 0)
        {
            mIndex = 2;
        }

        float hH = vertexData[0].distance;
        float hM;
        float hL;

        Vector3 M;
        Vector3 L;

        if (vertexData[1].index == mIndex)
        {
            M = vertexData[1].globalVertexPos;
            L = vertexData[2].globalVertexPos;
            hM = vertexData[1].distance;
            hL = vertexData[2].distance;
        }
        else
        {
            M = vertexData[2].globalVertexPos;
            L = vertexData[1].globalVertexPos;
            hM = vertexData[2].distance;
            hL = vertexData[1].distance;
        }

        Vector3 MH = H - M;
        float tM = -hM / (hH - hM);
        Vector3 IM = M + tM * MH;

        Vector3 LH = H - L;
        float tL = -hL / (hH - hL);
        Vector3 IL = L + tL * LH;

        // 2 submerged + 1 above
        underWaterTriangleData.Add(new TriangleData(M, IM, IL, boatRB, timeSinceStart));
        underWaterTriangleData.Add(new TriangleData(M, IL, L, boatRB, timeSinceStart));
        aboveWaterTriangleData.Add(new TriangleData(IM, H, IL, boatRB, timeSinceStart));

        float submergedArea =
            BoatPhysicsMath.GetTriangleArea(M, IM, IL) +
            BoatPhysicsMath.GetTriangleArea(M, IL, L);

        slammingForceData[triangleCounter].submergedArea = submergedArea;

        indexOfOriginalTriangle.Add(triangleCounter);
        indexOfOriginalTriangle.Add(triangleCounter);
    }

    private void AddTrianglesTwoAboveWater(List<VertexData> vertexData, int triangleCounter)
    {
        Vector3 L = vertexData[2].globalVertexPos;

        int hIndex = vertexData[2].index + 1;
        if (hIndex > 2)
        {
            hIndex = 0;
        }

        float hL = vertexData[2].distance;
        float hH;
        float hM;

        Vector3 H;
        Vector3 M;

        if (vertexData[1].index == hIndex)
        {
            H = vertexData[1].globalVertexPos;
            M = vertexData[0].globalVertexPos;
            hH = vertexData[1].distance;
            hM = vertexData[0].distance;
        }
        else
        {
            H = vertexData[0].globalVertexPos;
            M = vertexData[1].globalVertexPos;
            hH = vertexData[0].distance;
            hM = vertexData[1].distance;
        }

        Vector3 LM = M - L;
        float tM = -hL / (hM - hL);
        Vector3 JM = L + tM * LM;

        Vector3 LH = H - L;
        float tH = -hL / (hH - hL);
        Vector3 JH = L + tH * LH;

        // 1 submerged + 2 above
        underWaterTriangleData.Add(new TriangleData(L, JH, JM, boatRB, timeSinceStart));
        aboveWaterTriangleData.Add(new TriangleData(JH, H, JM, boatRB, timeSinceStart));
        aboveWaterTriangleData.Add(new TriangleData(JM, H, M, boatRB, timeSinceStart));

        slammingForceData[triangleCounter].submergedArea = BoatPhysicsMath.GetTriangleArea(L, JH, JM);
        indexOfOriginalTriangle.Add(triangleCounter);
    }

    private class VertexData
    {
        public float distance;
        public int index;
        public Vector3 globalVertexPos;
    }

    public void DisplayMesh(Mesh mesh, string name, List<TriangleData> trianglesData)
    {
        if (mesh == null)
        {
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < trianglesData.Count; i++)
        {
            Vector3 p1 = boatTrans.InverseTransformPoint(trianglesData[i].p1);
            Vector3 p2 = boatTrans.InverseTransformPoint(trianglesData[i].p2);
            Vector3 p3 = boatTrans.InverseTransformPoint(trianglesData[i].p3);

            vertices.Add(p1);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p2);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p3);
            triangles.Add(vertices.Count - 1);
        }

        mesh.Clear();
        mesh.name = name;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public float CalculateUnderWaterLength()
    {
        if (underWaterMesh == null || underWaterMesh.vertexCount == 0)
        {
            return 0f;
        }

        return underWaterMesh.bounds.size.z;
    }

    private void CalculateOriginalTrianglesArea()
    {
        boatArea = 0f;

        int triangleCounter = 0;
        for (int i = 0; i < boatTriangles.Length; i += 3)
        {
            if (triangleCounter >= slammingForceData.Count)
            {
                break;
            }

            Vector3 p1 = boatVertices[boatTriangles[i]];
            Vector3 p2 = boatVertices[boatTriangles[i + 1]];
            Vector3 p3 = boatVertices[boatTriangles[i + 2]];

            float area = BoatPhysicsMath.GetTriangleArea(p1, p2, p3);
            Vector3 center = (p1 + p2 + p3) / 3f;

            slammingForceData[triangleCounter].originalArea = area;
            slammingForceData[triangleCounter].triangleCenter = center;
            boatArea += area;

            triangleCounter += 1;
        }
    }
}
