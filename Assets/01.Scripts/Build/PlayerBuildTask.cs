using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBuildTask : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject previewPrefab;

    [Header("Grid")]
    [SerializeField] private Vector3 cellSize = new Vector3(3f, 1f, 3f);
    [SerializeField] private Vector3 cellOriginOffset = Vector3.zero;
    [SerializeField] private bool applyCellSizeToPlacedBlock = true;

    [Header("Raycast")]
    [SerializeField] private LayerMask blockLayerMask;
    [SerializeField] private float rayDistance = 300f;
    [SerializeField] private bool useScreenCenterRay = true;

    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private GameObject previewInstance;

    private void Awake()
    {
        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
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

        AutoSetCellOriginFromFirstBlock();
    }

    private void Update()
    {
        if (!CanBuildNow())
        {
            HidePreview();
            return;
        }

        RebuildOccupiedCells();

        Vector3Int candidateCell;
        bool found = TryFindCandidateCell(out candidateCell);
        if (!found)
        {
            HidePreview();
            return;
        }

        ShowPreview(candidateCell);

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool placed = PlaceBlock(candidateCell);
            if (!placed)
            {
                return;
            }

            playerInteraction.TryConsumeHeldBuildWoodBlockForBuild();
            HidePreview();
        }
    }

    private bool CanBuildNow()
    {
        if (playerInteraction == null)
        {
            return false;
        }

        return playerInteraction.IsHoldingBuildWoodBlock();
    }
    

    private void RebuildOccupiedCells()
    {
        occupiedCells.Clear();

        if (blocksRoot == null)
        {
            return;
        }

        Transform[] all = blocksRoot.GetComponentsInChildren<Transform>(true);
        int count = all.Length;

        for (int i = 0; i < count; i++)
        {
            Transform t = all[i];

            if (t == blocksRoot || !t.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!IsInLayerMask(t.gameObject.layer, blockLayerMask))
            {
                continue;
            }

            Vector3Int cell = WorldToCell(t.position);

            if (!occupiedCells.Contains(cell))
            {
                occupiedCells.Add(cell);
            }
        }
    }

    private bool TryFindCandidateCell(out Vector3Int cell)
    {
        cell = Vector3Int.zero;

        if (boatRoot == null || blocksRoot == null || targetCamera == null)
        {
            return false;
        }

        Ray ray = GetBuildRay();

        RaycastHit hit;
        bool didHit = Physics.Raycast(ray, out hit, rayDistance, blockLayerMask, QueryTriggerInteraction.Ignore);
        if (!didHit)
        {
            return false;
        }

        Transform hitBlock = ResolveBlockTransform(hit.collider.transform);
        if (hitBlock == null)
        {
            return false;
        }

        Vector3Int baseCell = WorldToCell(hitBlock.position);
        Vector3 localNormal = boatRoot.InverseTransformDirection(hit.normal);
        Vector3Int dir = LocalNormalToCellOffset(localNormal);

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

    private Transform ResolveBlockTransform(Transform hitTransform)
    {
        Transform currentTransform = hitTransform;

        while (currentTransform != null)
        {
            if (currentTransform == blocksRoot)
            {
                return null;
            }

            if (IsInLayerMask(currentTransform.gameObject.layer, blockLayerMask))
            {
                return currentTransform;
            }

            currentTransform = currentTransform.parent;
        }

        return null;
    }

    private Vector3Int LocalNormalToCellOffset(Vector3 localNormal)
    {
        float ax = Mathf.Abs(localNormal.x);
        float ay = Mathf.Abs(localNormal.y);
        float az = Mathf.Abs(localNormal.z);

        if (ax >= ay && ax >= az)
        {
            return localNormal.x >= 0f ? Vector3Int.right : Vector3Int.left;
        }

        if (ay >= ax && ay >= az)
        {
            return localNormal.y >= 0f ? Vector3Int.up : Vector3Int.down;
        }

        return localNormal.z >= 0f ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    private Ray GetBuildRay()
    {
        if (useScreenCenterRay)
        {
            return targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        if (Mouse.current == null)
        {
            return targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        return targetCamera.ScreenPointToRay(mousePos);
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

    private bool PlaceBlock(Vector3Int cell)
    {
        if (blockPrefab == null || blocksRoot == null || boatRoot == null)
        {
            return false;
        }

        if (occupiedCells.Contains(cell))
        {
            return false;
        }

        Vector3 worldPos = CellToWorld(cell);
        Quaternion worldRot = boatRoot.rotation;

        GameObject newBlock = Instantiate(blockPrefab, worldPos, worldRot, blocksRoot);
        newBlock.SetActive(true);

        if (applyCellSizeToPlacedBlock)
        {
            newBlock.transform.localScale = cellSize;
        }

        occupiedCells.Add(cell);

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.OnSpawnPool(ePoolType.PoofRealistic.ToString(), worldPos);
        }

        return true;
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void DisableAllColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        int count = colliders.Length;

        for (int i = 0; i < count; i++)
        {
            colliders[i].enabled = false;
        }
    }
    private void AutoSetCellOriginFromFirstBlock()
    {
        if (boatRoot == null || blocksRoot == null || blocksRoot.childCount == 0)
        {
            return;
        }

        Transform firstBlock = blocksRoot.GetChild(0);
        cellOriginOffset = boatRoot.InverseTransformPoint(firstBlock.position);
        Debug.Log("[Build] cellOriginOffset auto = " + cellOriginOffset, this);
    }
}
