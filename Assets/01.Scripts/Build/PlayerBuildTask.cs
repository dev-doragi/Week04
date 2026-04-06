using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
    [SerializeField] private float rayDistance = 8f;
    [SerializeField] private bool useScreenCenterRay = true;

    [Header("Empty Space Assist")]
    [SerializeField] private bool allowEmptySpaceAssist = true;
    [SerializeField] private float emptyProbeStep = 0.5f;
    [SerializeField] private bool requireNeighborBlock = true;

    [Header("WetWood")]
    [SerializeField] private Key wetWoodPlaceKey = Key.E;
    [SerializeField] private float wetWoodPlaceHeightOffset = 0.08f;
    [SerializeField] private float wetWoodPlaceSurfaceMinNormalY = 0.6f;

    private bool wasHoldingWetWood = false;
    private bool wetWoodPlaceArmed = false;

    public CinemachineTargetGroup TargetGroup;

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
    }

    private void Update()
{
    bool holdingWetWood = playerInteraction != null && playerInteraction.IsHoldingWetWood();

    if (holdingWetWood && !wasHoldingWetWood)
    {
        if (Keyboard.current == null) wetWoodPlaceArmed = true;
        else wetWoodPlaceArmed = !Keyboard.current[wetWoodPlaceKey].isPressed;
    }
    wasHoldingWetWood = holdingWetWood;

    if (holdingWetWood)
    {
        HidePreview();

        if (!wetWoodPlaceArmed)
        {
            if (Keyboard.current != null && !Keyboard.current[wetWoodPlaceKey].isPressed)
            {
                wetWoodPlaceArmed = true;
            }
            return;
        }

        HandleWetWoodQuickPlace();
        return;
    }

    bool holdingNetBlock = playerInteraction != null && playerInteraction.IsHoldingNetBlock();

    if (holdingNetBlock)
    {
        RebuildOccupiedCells();

        Vector3 netWorldPos;
        Quaternion netWorldRot;
        Transform anchorBlock;
        bool foundNet = TryFindNetPlacementPose(out netWorldPos, out netWorldRot, out anchorBlock);

        if (!foundNet)
        {
            HidePreview();
            return;
        }

        ShowPreviewWorld(netWorldPos, netWorldRot, cellSize);

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool placedNet = playerInteraction.TryPlaceHeldNetBlock(netWorldPos, netWorldRot, anchorBlock);
            if (placedNet)
            {
                HidePreview();
                RebuildOccupiedCells();
            }
        }

        return;
    }


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
        if (!placed) return;

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

        Transform[] allTransforms = blocksRoot.GetComponentsInChildren<Transform>(true);
        int count = allTransforms.Length;
        

        for (int i = 0; i < count; i++)
        {
            Transform currentTransform = allTransforms[i];

        if (currentTransform == blocksRoot || !currentTransform.gameObject.activeInHierarchy)
        {
            continue;
        }

        bool isBlockLayer = IsInLayerMask(currentTransform.gameObject.layer, blockLayerMask);

        NetBlock netBlock;
        bool isNetBlock = currentTransform.TryGetComponent<NetBlock>(out netBlock);

        if (!isBlockLayer && !isNetBlock)
        {
            continue;
        }

        Vector3 centerWorld = GetBlockCenterWorld(currentTransform);
        Vector3Int cell = WorldToCell(centerWorld);

        if (!occupiedCells.Contains(cell))
        {
            occupiedCells.Add(cell);
        }


        }
    }

    private bool TryFindCandidateCell(out Vector3Int cell)
    {
        cell = Vector3Int.zero;

        bool foundFromBlock = TryFindCandidateFromBlockHit(out cell);
        if (foundFromBlock)
        {
            return true;
        }

        if (allowEmptySpaceAssist)
        {
            return TryFindCandidateFromEmptySpace(out cell);
        }

        return false;
    }

    private bool TryFindCandidateFromBlockHit(out Vector3Int cell)
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

        Vector3 blockCenterWorld = GetBlockCenterWorld(hitBlock);
        Vector3Int baseCell = WorldToCell(blockCenterWorld);

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

    private bool TryFindCandidateFromEmptySpace(out Vector3Int cell)
    {
        cell = Vector3Int.zero;

        if (boatRoot == null || targetCamera == null)
        {
            return false;
        }

        Ray ray = GetBuildRay();

        float minCell = Mathf.Min(cellSize.x, Mathf.Min(cellSize.y, cellSize.z));
        float minStep = Mathf.Max(0.1f, minCell * 0.2f);
        float step = Mathf.Max(minStep, emptyProbeStep);

        for (float dist = step; dist <= rayDistance; dist += step)
        {
            Vector3 worldPoint = ray.origin + ray.direction * dist;
            Vector3Int probeCell = WorldToCell(worldPoint);

            if (occupiedCells.Contains(probeCell))
            {
                continue;
            }

            if (requireNeighborBlock && !HasAnyNeighbor(probeCell))
            {
                continue;
            }

            cell = probeCell;
            return true;
        }

        return false;
    }

    private bool HasAnyNeighbor(Vector3Int cell)
    {
        Vector3Int right = new Vector3Int(cell.x + 1, cell.y, cell.z);
        Vector3Int left = new Vector3Int(cell.x - 1, cell.y, cell.z);
        Vector3Int up = new Vector3Int(cell.x, cell.y + 1, cell.z);
        Vector3Int down = new Vector3Int(cell.x, cell.y - 1, cell.z);
        Vector3Int forward = new Vector3Int(cell.x, cell.y, cell.z + 1);
        Vector3Int back = new Vector3Int(cell.x, cell.y, cell.z - 1);

        return occupiedCells.Contains(right) ||
               occupiedCells.Contains(left) ||
               occupiedCells.Contains(up) ||
               occupiedCells.Contains(down) ||
               occupiedCells.Contains(forward) ||
               occupiedCells.Contains(back);
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

    private int RoundCell(float value)
    {
        if (value >= 0f)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        return Mathf.CeilToInt(value - 0.5f);
    }

    private Vector3Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = boatRoot.InverseTransformPoint(worldPos) - cellOriginOffset;

        float safeX = Mathf.Max(0.0001f, cellSize.x);
        float safeY = Mathf.Max(0.0001f, cellSize.y);
        float safeZ = Mathf.Max(0.0001f, cellSize.z);

        int x = RoundCell(local.x / safeX);
        int y = RoundCell(local.y / safeY);
        int z = RoundCell(local.z / safeZ);

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

    private Vector3 GetBlockCenterWorld(Transform blockTransform)
    {
        Collider ownCollider = blockTransform.GetComponent<Collider>();
        if (ownCollider != null)
        {
            return ownCollider.bounds.center;
        }

        Collider childCollider = blockTransform.GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }

        return blockTransform.position;
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
    //젖은 나무를
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

        TargetGroup.AddMember(newBlock.transform, 1f, 2f);

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
    private void HandleWetWoodQuickPlace()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current[wetWoodPlaceKey].wasPressedThisFrame)
        {
            return;
        }

        Vector3 placeWorldPos;
        Quaternion placeWorldRot;
        bool found = TryGetWetWoodPlacePose(out placeWorldPos, out placeWorldRot);

        if (!found)
        {
            return;
        }

        bool placed = playerInteraction.TryPlaceHeldWetWood(placeWorldPos, placeWorldRot, boatRoot);
        if (placed)
        {
            wetWoodPlaceArmed = false;
        }
    }

    private bool TryGetWetWoodPlacePose(out Vector3 worldPos, out Quaternion worldRot)
    {
        worldPos = Vector3.zero;
        worldRot = Quaternion.identity;

        if (boatRoot == null || targetCamera == null)
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

        if (hit.normal.y < wetWoodPlaceSurfaceMinNormalY)
        {
            return false;
        }

        Vector3 hitCenterWorld = GetBlockCenterWorld(hit.collider.transform);
        Vector3Int baseCell = WorldToCell(hitCenterWorld);

        Vector3 hitLocal = boatRoot.InverseTransformPoint(hit.point) - cellOriginOffset;
        int x = RoundCell(hitLocal.x / Mathf.Max(0.0001f, cellSize.x));
        int z = RoundCell(hitLocal.z / Mathf.Max(0.0001f, cellSize.z));

        Vector3Int topCell = new Vector3Int(x, baseCell.y + 1, z);
        worldPos = CellToWorld(topCell) + Vector3.up * wetWoodPlaceHeightOffset;
        worldRot = boatRoot.rotation;

        return true;
    }
    private static readonly Vector3Int[] HorizontalDirs =
    {
        Vector3Int.right, Vector3Int.left,
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };
    private bool TryFindNetPlacementPose(out Vector3 worldPos, out Quaternion worldRot, out Transform anchorBlock)
    {
        worldPos = Vector3.zero;
        worldRot = Quaternion.identity;
        anchorBlock = null;

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

        Vector3 blockCenterWorld = GetBlockCenterWorld(hitBlock);
        Vector3Int baseCell = WorldToCell(blockCenterWorld);
        Vector3Int centerCell = GetOccupiedCenterCell();

        Vector3 fromCenter = new Vector3(baseCell.x - centerCell.x, 0f, baseCell.z - centerCell.z);
        if (fromCenter.sqrMagnitude < 0.0001f)
        {
            Vector3 lookLocal = boatRoot.InverseTransformDirection(ray.direction);
            fromCenter = new Vector3(lookLocal.x, 0f, lookLocal.z);
        }
        if (fromCenter.sqrMagnitude < 0.0001f)
        {
            fromCenter = Vector3.forward;
        }
        fromCenter.Normalize();

        Vector3Int bestDir = Vector3Int.zero;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < HorizontalDirs.Length; i++)
        {
            Vector3Int dir = HorizontalDirs[i];
            Vector3Int candidateCell = baseCell + dir;

            if (occupiedCells.Contains(candidateCell))
            {
                continue;
            }

            Vector3 dirVec = new Vector3(dir.x, 0f, dir.z);
            dirVec.Normalize();

            float score = Vector3.Dot(dirVec, fromCenter);
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = dir;
            }
        }

        if (bestDir == Vector3Int.zero)
        {
            return false;
        }

        Vector3Int targetCell = baseCell + bestDir;
        worldPos = CellToWorld(targetCell);

        Vector3 outwardWorld = boatRoot.TransformDirection(new Vector3(bestDir.x, 0f, bestDir.z));
        outwardWorld.y = 0f;
        if (outwardWorld.sqrMagnitude < 0.0001f)
        {
            outwardWorld = boatRoot.forward;
            outwardWorld.y = 0f;
        }
        outwardWorld.Normalize();

        worldRot = Quaternion.LookRotation(outwardWorld, Vector3.up);
        anchorBlock = hitBlock;
        return true;
    }
    private Vector3Int GetOccupiedCenterCell()
    {
        if (occupiedCells.Count == 0)
        {
            return Vector3Int.zero;
        }

        float sumX = 0f;
        float sumY = 0f;
        float sumZ = 0f;
        int count = 0;

        foreach (Vector3Int cell in occupiedCells)
        {
            sumX += cell.x;
            sumY += cell.y;
            sumZ += cell.z;
            count++;
        }

        return new Vector3Int(
            Mathf.RoundToInt(sumX / Mathf.Max(1, count)),
            Mathf.RoundToInt(sumY / Mathf.Max(1, count)),
            Mathf.RoundToInt(sumZ / Mathf.Max(1, count))
        );
    }

    private Vector3Int LocalNormalToHorizontalCellOffset(Vector3 localNormal)
    {
        if (Mathf.Abs(localNormal.y) > 0.5f)
        {
            return Vector3Int.zero;
        }

        float ax = Mathf.Abs(localNormal.x);
        float az = Mathf.Abs(localNormal.z);

        if (ax >= az)
        {
            return localNormal.x >= 0f ? Vector3Int.right : Vector3Int.left;
        }

        return localNormal.z >= 0f ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    private void ShowPreviewWorld(Vector3 worldPos, Quaternion worldRot, Vector3 previewScale)
    {
        if (previewInstance == null)
        {
            return;
        }

        previewInstance.transform.position = worldPos;
        previewInstance.transform.rotation = worldRot;
        previewInstance.transform.localScale = previewScale;

        if (!previewInstance.activeSelf)
        {
            previewInstance.SetActive(true);
        }
    }

}
