using System.Collections.Generic;
using UnityEngine;

public class EndlessWaterSquare : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject boatObj;
    [SerializeField] private GameObject waterSqrObj;

    [Header("Grid")]
    [SerializeField] private float squareWidth = 800f;
    [SerializeField] private float innerSquareResolution = 5f;
    [SerializeField] private float outerSquareResolution = 25f;

    [Header("Update")]
    [SerializeField] private float meshUpdateInterval = 0.05f;

    private readonly List<WaterSquare> waterSquares = new List<WaterSquare>();
    private Vector3 oceanPos;
    private float updateTimer;

    void Start()
    {
        if (boatObj == null || waterSqrObj == null)
        {
            Debug.LogError("EndlessWaterSquare: boatObj / waterSqrObj is not assigned.");
            enabled = false;
            return;
        }

        CreateEndlessSea();

        Vector3 boatPos = boatObj.transform.position;
        oceanPos = GetSnappedOceanPos(boatPos);
        transform.position = oceanPos;

        UpdateAllSquares(Time.time);
    }

    void Update()
    {
        if (boatObj == null || WaterController.current == null)
        {
            return;
        }

        updateTimer += Time.deltaTime;
        if (updateTimer < meshUpdateInterval)
        {
            return;
        }

        updateTimer = 0f;

        oceanPos = GetSnappedOceanPos(boatObj.transform.position);
        transform.position = oceanPos;

        UpdateAllSquares(Time.time);
    }

    private void UpdateAllSquares(float timeSinceStart)
    {
        for (int i = 0; i < waterSquares.Count; i++)
        {
            WaterSquare square = waterSquares[i];
            square.UpdateVertices(oceanPos, timeSinceStart);
            square.ApplyUpdatedVerticesToMesh();
        }
    }

    private Vector3 GetSnappedOceanPos(Vector3 boatPos)
    {
        float safeRes = Mathf.Max(0.01f, innerSquareResolution);
        float x = safeRes * Mathf.Round(boatPos.x / safeRes);
        float z = safeRes * Mathf.Round(boatPos.z / safeRes);

        return new Vector3(x, transform.position.y, z);
    }

    private void CreateEndlessSea()
    {
        AddWaterPlane(0f, 0f, 0f, squareWidth, innerSquareResolution);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                {
                    continue;
                }

                float yPos = -0.5f;
                AddWaterPlane(x * squareWidth, z * squareWidth, yPos, squareWidth, outerSquareResolution);
            }
        }
    }

    private void AddWaterPlane(float xCoord, float zCoord, float yPos, float planeSize, float spacing)
    {
        GameObject waterPlane = Instantiate(waterSqrObj, transform);
        waterPlane.SetActive(true);
        waterPlane.transform.localPosition = new Vector3(xCoord, yPos, zCoord);
        waterPlane.transform.localRotation = Quaternion.identity;
        waterPlane.transform.localScale = Vector3.one;

        WaterSquare newWaterSquare = new WaterSquare(waterPlane, planeSize, spacing);
        waterSquares.Add(newWaterSquare);
    }
}
