using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoatBuildController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boatRoot;
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject wetBlockPrefab;
    [SerializeField] private GameObject previewPrefab;
    [SerializeField] private GameObject removePreviewPrefab;

    [Header("Grid")]
    [SerializeField] private Vector3 cellSize = new Vector3(3f, 1f, 3f);
    [SerializeField] private Vector3 cellOriginOffset = Vector3.zero;

    [Header("Raycast")]
    [SerializeField] private LayerMask blockLayerMask;
    [SerializeField] private float rayDistance = 300f;
    [SerializeField] private bool useScreenCenterRay = true;

    [Header("Input")]
    [SerializeField] private Key buildHoldKey = Key.R;
    [SerializeField] private Key wetBuildKey = Key.G;
    [SerializeField] private Key removeKey = Key.T;
    

    [Header("Placement")]
    [SerializeField] private bool applyCellSizeToPlacedBlock = true; //나무 블록 크기 조정

    private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private GameObject previewInstance;
    private GameObject removePreviewInstance;

    private void Awake()
    {
        
        if (previewPrefab != null)
        {
            previewInstance = Instantiate(previewPrefab);
            previewInstance.SetActive(false);
            DisableAllColliders(previewInstance);
        }
        if (removePreviewPrefab != null)
        {
            removePreviewInstance = Instantiate(removePreviewPrefab);
            removePreviewInstance.SetActive(false);
            DisableAllColliders(removePreviewInstance);
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
        
        bool removeHeld = Keyboard.current[removeKey] != null && Keyboard.current[removeKey].isPressed;
        bool wetBuildHeld = Keyboard.current[wetBuildKey] != null && Keyboard.current[wetBuildKey].isPressed;
        bool buildHeld = Keyboard.current[buildHoldKey] != null && Keyboard.current[buildHoldKey].isPressed;

        if (removeHeld)
        {
            HandleRemoveMode();
            return;
        }

        if (wetBuildHeld)
        {
            HandleWetBuildMode();
            return;
        }

        if (buildHeld)
        {
            HandleBuildMode();
            return;
        }
        HideAllPreview();


        
    }
    private void HandleBuildMode()
    {
        HideRemovePreview();

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

    private void HandleWetBuildMode()
    {
        HideRemovePreview();

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
            PlaceWetBlock(candidateCell);
        }
    }

    private void HandleRemoveMode()
    {
        HidePreview();

        Transform targetBlock;
        Vector3Int targetCell;
        bool found = TryFindRemoveTarget(out targetBlock, out targetCell);
        if (!found)
        {
            HideRemovePreview();
            return;
        }

        ShowRemovePreview(targetCell);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RemoveBlock(targetBlock, targetCell);
        }
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
    private bool TryFindRemoveTarget(out Transform targetBlock, out Vector3Int targetCell)
    {
        targetBlock = null;
        targetCell = Vector3Int.zero;

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

        targetBlock = hitBlock;
        targetCell = WorldToCell(hitBlock.position);
        return true;
    }

    private Transform ResolveBlockTransform(Transform hitTransform) //blockLayerMask 에 포함된 오브젝트(부모) 를 반환
    {
        Transform t = hitTransform;

        while (t != null)
        {
            if (t == blocksRoot)
            {
                return null;
            }

            if (IsInLayerMask(t.gameObject.layer, blockLayerMask))
            {
                return t;
            }

            t = t.parent;
        }

        return null;
    }
    private Vector3Int NormalToAllowedDirection(Vector3 localNormal)
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
    //T키를 누르고있으면, 프리뷰오브젝트처럼(이번엔 빨간색 프리펩)그게 블록자리에 생김.
    //클릭을 하면, 그 자리에있는 걔를 파괴함.


    private Vector3Int LocalNormalToCellOffset(Vector3 localNormal) // 레이의 맞은면에 그 방향으로 오프셋바꿔줌(연두색)
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

        Vector2 mousePos = Mouse.current.position.ReadValue();
        return targetCamera.ScreenPointToRay(mousePos);
    }

    private Vector3Int WorldToCell(Vector3 worldPos) // 월드좌표 로컬로 변환
    {
        Vector3 local = boatRoot.InverseTransformPoint(worldPos) - cellOriginOffset;

        int x = Mathf.RoundToInt(local.x / Mathf.Max(0.0001f, cellSize.x));
        int y = Mathf.RoundToInt(local.y / Mathf.Max(0.0001f, cellSize.y));
        int z = Mathf.RoundToInt(local.z / Mathf.Max(0.0001f, cellSize.z));

        return new Vector3Int(x, y, z);
    }

    private Vector3 CellToWorld(Vector3Int cell) // 격자 -> world
    {
        Vector3 local = new Vector3(
            cell.x * cellSize.x,
            cell.y * cellSize.y,
            cell.z * cellSize.z
        ) + cellOriginOffset;

        return boatRoot.TransformPoint(local);
    }

    private void ShowPreview(Vector3Int cell) // 해당 셀 위치에 프리뷰 오브젝트 옮기고 키기
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
    private void ShowRemovePreview(Vector3Int cell)
    {
        if (removePreviewInstance == null)
        {
            return;
        }

        removePreviewInstance.transform.position = CellToWorld(cell);
        removePreviewInstance.transform.rotation = boatRoot.rotation;
        removePreviewInstance.transform.localScale = cellSize;

        if (!removePreviewInstance.activeSelf)
        {
            removePreviewInstance.SetActive(true);
        }
    }

    private void HidePreview() // 프리뷰 오브젝트 끔
    {
        if (previewInstance != null && previewInstance.activeSelf)
        {
            previewInstance.SetActive(false);
        }
    }
    private void HideRemovePreview()
    {
        if (removePreviewInstance != null && removePreviewInstance.activeSelf)
        {
            removePreviewInstance.SetActive(false);
        }
    }
    private void RemoveBlock(Transform targetBlock, Vector3Int cell)
        {
            if (targetBlock == null)
            {
                return;
            }

            Destroy(targetBlock.gameObject);
            occupiedCells.Remove(cell);
        }
    private void PlaceBlock(Vector3Int cell) // 블록 생성
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
        Quaternion worldRot = boatRoot.rotation;

        GameObject newBlock = Instantiate(blockPrefab, worldPos, worldRot, blocksRoot);
        newBlock.SetActive(true);

        if (applyCellSizeToPlacedBlock)
        {
            newBlock.transform.localScale = cellSize;
        }

        occupiedCells.Add(cell);
    } 
    private void PlaceWetBlock(Vector3Int cell)
    {
        if (wetBlockPrefab == null || blocksRoot == null)
        {
            return;
        }

        if (occupiedCells.Contains(cell))
        {
            return;
        }

        Vector3 worldPos = CellToWorld(cell);
        Quaternion worldRot = boatRoot.rotation;

        GameObject newBlock = Instantiate(wetBlockPrefab, worldPos, worldRot, blocksRoot);
        newBlock.SetActive(true);

        if (applyCellSizeToPlacedBlock)
        {
            newBlock.transform.localScale = cellSize;
        }

        occupiedCells.Add(cell);
    }
    private void HideAllPreview()
    {
        HidePreview();
        HideRemovePreview();
    }

    private bool IsInLayerMask(int layer, LayerMask mask) //레이어 체크
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void DisableAllColliders(GameObject tempobj) // 프리뷰오브젝트 콜라이더 제거
    {
        Collider[] colliders = tempobj.GetComponentsInChildren<Collider>(true);
        int count = colliders.Length;

        for (int i = 0; i < count; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
