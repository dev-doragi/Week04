using UnityEngine;

public class WaterSquare
{
    public Transform squareTransform;
    public MeshFilter terrainMeshFilter;
    public Vector3 centerPos;
    public Vector3[] vertices;

    private float size;
    public float spacing;
    private int width;
    private Mesh terrainMesh;

    public WaterSquare(GameObject waterSquareObj, float size, float spacing)
    {
        this.squareTransform = waterSquareObj.transform;
        this.size = size;
        this.spacing = Mathf.Max(0.01f, spacing);

        terrainMeshFilter = squareTransform.GetComponent<MeshFilter>();
        if (terrainMeshFilter == null)
        {
            Debug.LogError("WaterSquare: MeshFilter is missing.");
            return;
        }

        width = Mathf.Max(2, Mathf.RoundToInt(this.size / this.spacing) + 1);

        float offset = -((width - 1) * this.spacing) * 0.5f;
        squareTransform.localPosition += new Vector3(offset, 0f, offset);

        centerPos = squareTransform.localPosition;

        GenerateMesh();

        if (terrainMesh != null)
        {
            vertices = terrainMesh.vertices;
        }
    }

    public void UpdateVertices(Vector3 oceanPos, float timeSinceStart)
    {
        if (vertices == null || vertices.Length == 0)
        {
            return;
        }

        WaterController water = WaterController.current;
        if (water == null)
        {
            return;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 localVertex = vertices[i];

            Vector3 worldPoint = new Vector3(
                localVertex.x + centerPos.x + oceanPos.x,
                localVertex.y + centerPos.y + oceanPos.y,
                localVertex.z + centerPos.z + oceanPos.z
            );

            float waterY = water.GetWaveYPos(worldPoint, timeSinceStart);
            localVertex.y = waterY - (centerPos.y + oceanPos.y);

            vertices[i] = localVertex;
        }
    }

    public void ApplyUpdatedVerticesToMesh()
    {
        if (terrainMesh == null || vertices == null)
        {
            return;
        }

        terrainMesh.vertices = vertices;
        terrainMesh.RecalculateNormals();
        terrainMesh.RecalculateBounds();
    }

    private void GenerateMesh()
    {
        Vector3[] newVertices = new Vector3[width * width];
        int[] triangles = new int[(width - 1) * (width - 1) * 6];

        int v = 0;
        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < width; x++)
            {
                newVertices[v] = new Vector3(x * spacing, 0f, z * spacing);
                v++;
            }
        }

        int t = 0;
        for (int z = 1; z < width; z++)
        {
            for (int x = 1; x < width; x++)
            {
                int i0 = x + z * width;
                int i1 = x + (z - 1) * width;
                int i2 = (x - 1) + (z - 1) * width;
                int i3 = (x - 1) + z * width;

                triangles[t++] = i0;
                triangles[t++] = i1;
                triangles[t++] = i2;

                triangles[t++] = i0;
                triangles[t++] = i2;
                triangles[t++] = i3;
            }
        }

        terrainMesh = new Mesh();
        terrainMesh.name = "Water Mesh";
        terrainMesh.vertices = newVertices;
        terrainMesh.triangles = triangles;
        terrainMesh.RecalculateBounds();
        terrainMesh.RecalculateNormals();

        terrainMeshFilter.mesh = terrainMesh;
    }
}
