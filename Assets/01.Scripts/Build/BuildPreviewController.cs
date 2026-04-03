using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BoatBuildPreviewAndPlace : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject previewPrefab;

    [Header("Grid")]
    [SerializeField] private Vector3 cellSize = new Vector3(3f, 1f, 3f);
    [SerializeField] private Vector3 cellOriginOffset = Vector3.zero;

    [Header("Raycast")]
    [SerializeField] private LayerMask blockLayerMask;
    [SerializeField] private float rayDistance = 300f;

    [Header("Input")]
    [SerializeField] private Key buildKey = Key.A;

    [Header("Build Wobble")]
    [SerializeField] private Rigidbody boatRb;
    [SerializeField] private float wobbleTorqueImpulse = 35f;
    [SerializeField] private float wobbleDownImpulse = 8f;
    [SerializeField] private float wobbleRandomYaw = 2f;
    [SerializeField] private float maxBoatAngularSpeed = 1.2f;

    [SerializeField] private BoatStabilityByBlocks stability;



    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private GameObject previewInstance;

    private void Awake()
    {
        if (boatRoot == null)
        {
            boatRoot = transform;
        }
        if (blocksRoot == null)
        {
            blocksRoot = boatRoot;
        }
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewInstance.SetActive(false);
            DisableAllColliders(previewInstance);
        }
        if (boatRb == null)
        {
            boatRb = GetComponent<Rigidbody>();
        }
        if (boatRb == null && boatRoot != null)
        {
            boatRb = boatRoot.GetComponent<Rigidbody>();
        }
    }


    private void Update()
    {
        RebuildOccupiedCells();

        if (Keyboard.current == null || Mouse.current == null || targetCamera == null)
        {
            HidePreview();
            return;
        }

        KeyControl buildKeyControl = Keyboard.current[buildKey];
        if (buildKeyControl == null || !buildKeyControl.isPressed)
        {
            HidePreview();
            return;
        }

        Vector3Int candidateCell;
        bool found = TryFindCandidateCell(out candidateCell);

        if (!found)
        {
            HidePreview();
            return;
        }

        ShowPreview(candidateCell);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceBlock(candidateCell);

        }
    }
    private void ApplyBuildWobble(Vector3 placedWorldPos)
    {
        if (boatRb == null || boatRoot == null)
        {
            return;
        }

        Vector3 localPos = boatRoot.InverseTransformPoint(placedWorldPos);

        float side = 0f;
        if (localPos.x > 0.001f)
        {
            side = 1f;   // 오른쪽
        }
        else if (localPos.x < -0.001f)
        {
            side = -1f;  // 왼쪽
        }

        // 오른쪽(+x)에 설치하면 오른쪽으로 기울게: -forward 축 토크
        float rollImpulse = -side * wobbleTorqueImpulse;
        boatRb.AddTorque(boatRoot.forward * rollImpulse, ForceMode.Impulse);

        // 설치 지점 눌림 느낌(선택)
        boatRb.AddForceAtPosition(Vector3.down * wobbleDownImpulse, placedWorldPos, ForceMode.Impulse);

        Vector3 omega = boatRb.angularVelocity;
        if (omega.magnitude > maxBoatAngularSpeed)
        {
            boatRb.angularVelocity = omega.normalized * maxBoatAngularSpeed;
        }
    }



    private void RebuildOccupiedCells()
    {
        occupiedCells.Clear();

        if (blocksRoot == null)
        {
            return;
        }

        int childCount = blocksRoot.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = blocksRoot.GetChild(i);
            if (!child.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3Int cell = WorldToCell(child.position);
            if (!occupiedCells.Contains(cell))
            {
                occupiedCells.Add(cell);
            }
        }
    }

    private bool TryFindCandidateCell(out Vector3Int cell)
    {
        cell = Vector3Int.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        RaycastHit hit;
        bool didHit = Physics.Raycast(ray, out hit, rayDistance, blockLayerMask, QueryTriggerInteraction.Ignore);
        if (!didHit)
        {
            return false;
        }

        Vector3Int baseCell = WorldToCell(hit.collider.transform.position);
        Vector3 localNormal = boatRoot.InverseTransformDirection(hit.normal);
        Vector3Int dir = NormalToAllowedDirection(localNormal);

        if (dir == Vector3Int.zero)
        {
            return false;
        }

        Vector3Int targetCell = baseCell + dir;
        if (occupiedCells.Contains(targetCell))
        {
            return false;
        }

        cell = targetCell;
        return true;
    }

    private Vector3Int NormalToAllowedDirection(Vector3 localNormal)
    {
        float ax = Mathf.Abs(localNormal.x);
        float ay = Mathf.Abs(localNormal.y);
        float az = Mathf.Abs(localNormal.z);

        if (ax >= ay && ax >= az)
        {
            if (localNormal.x > 0f)
            {
                return Vector3Int.right;
            }
            return Vector3Int.left;
        }

        if (ay >= ax && ay >= az)
        {
            if (localNormal.y > 0f)
            {
                return Vector3Int.up;
            }
            return Vector3Int.zero;
        }

        return Vector3Int.zero;
    }

    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = boatRoot.InverseTransformPoint(worldPos) - cellOriginOffset;

        int x = Mathf.RoundToInt(local.x / Mathf.Max(0.0001f, cellSize.x));
        int y = Mathf.RoundToInt(local.y / Mathf.Max(0.0001f, cellSize.y));
        int z = Mathf.RoundToInt(local.z / Mathf.Max(0.0001f, cellSize.z));

        return new Vector3Int(x, y, z);
    }

    private Vector3 CellToWorld(Vector3Int cell)
    {
        Vector3 local = new Vector3(
            cell.x * cellSize.x,
            cell.y * cellSize.y,
            cell.z * cellSize.z
        ) + cellOriginOffset;

        return boatRoot.TransformPoint(local);
    }

    private void ShowPreview(Vector3Int cell)
    {
        if (previewInstance == null)
        {
            return;
        }

        previewInstance.transform.position = CellToWorld(cell);
        previewInstance.transform.rotation = boatRoot.rotation;
        previewInstance.transform.localScale = cellSize;

        if (!previewInstance.activeSelf)
        {
            previewInstance.SetActive(true);
        }
    }

    private void HidePreview()
    {
        if (previewInstance != null && previewInstance.activeSelf)
        {
            previewInstance.SetActive(false);
        }
    }

    private void PlaceBlock(Vector3Int cell)
    {
        if (blockPrefab == null || blocksRoot == null)
        {
            return;
        }
        if (occupiedCells.Contains(cell))
        {
            return;
        }

        Vector3 worldPos = CellToWorld(cell);
        Quaternion rot = boatRoot.rotation;

        GameObject newBlock = Instantiate(blockPrefab, worldPos, rot, blocksRoot);
        newBlock.transform.localScale = cellSize;
        newBlock.SetActive(true);

        occupiedCells.Add(cell);

        ApplyBuildWobble(worldPos);
        if (stability != null)
        {
            stability.NotifyBlockPlaced(newBlock.transform.position);
        }
    }


    private void DisableAllColliders(GameObject go)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        int count = colliders.Length;

        for (int i = 0; i < count; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
