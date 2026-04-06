using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public CinemachineTargetGroup targetGroup;

    [Header("좌클릭(도끼)")]
    public float swingAngle = 80f;
    public float swingDuration = 0.2f;
    public float returnDuration = 0.15f;

    [Space(10)]
    public GameObject outlineCursor;
    public LayerMask blockLayer;
    public float chopTimeRequired = 1.5f;   // 부서지는데 필요 시간
    public float reachDistance = 3.0f;      // 손이 닿는 거리

    private float _currentChopTime = 0.0f;  // 현재 도끼질을 한 시간(진척도)
    private Transform _currentTargetBlock;  // 지금 때리고 있는 나무(시선 벗어났는지 확인 용)

    private bool _isSwinging = false;

    [Header("F키_아이템 줍기")]
    public GameObject axeOverlay;
    public GameObject currentItemOverlay;
    public float pickupRange = 7f;
    public LayerMask itemLayer;

    [Header("조타 운전")]
    public GameObject boat;
    public PlayerEntity player;

    [Header("손에 들 오브젝트 목록")]
    public GameObject woodOnePiece;
    public GameObject woodFullSet;
    public Vector3 grabObjectPos = new Vector3(3.62f, -1.8f, -195f);
    public Vector3 grabObjectRot = new Vector3(-60f, 0f, -15f);

    // private GameObject _heldItem;

    private BaseResource _heldItem;
    private Camera _mainCamera;

    private int _overlayLayer;
    private int _defaultLayer;
    private bool isSteering;
    private ParticlePoolObject _hittingParticle;

    [SerializeField] private UI_InteractionPrompt _interactionUI;

    [SerializeField] private Transform boatTr;
    [SerializeField] private ePlayerState interactionState;
    [SerializeField] private bool _isSteering;

    private void Start()
    {
        _mainCamera = Camera.main;
        _overlayLayer = LayerMask.NameToLayer("ViewModel");
        _defaultLayer = LayerMask.NameToLayer("Interact");

        currentItemOverlay = axeOverlay;
        RefreshInteractionUI();
    }

    private void RefreshInteractionUI()
    {
        if (_interactionUI == null)
            return;

        if (interactionState == ePlayerState.None)
        {
            _interactionUI.Hide();
            return;
        }

        if (interactionState == ePlayerState.Steering && _isSteering)
        {
            _interactionUI.Hide();
            return;
        }

        InteractionKeyType keyType = GetInteractionKeyByState(interactionState);
        string actionText = GetInteractionTextByState(interactionState);

        if (keyType == InteractionKeyType.None || string.IsNullOrEmpty(actionText))
        {
            _interactionUI.Hide();
            return;
        }

        _interactionUI.Show($"{GetInteractionKeyString(keyType)}를 눌러 {actionText}");
    }

    private InteractionKeyType GetInteractionKeyByState(ePlayerState state)
    {
        switch (state)
        {
            case ePlayerState.Fueling:
                return InteractionKeyType.F;

            case ePlayerState.Crafting:
                return InteractionKeyType.F;

            case ePlayerState.Steering:
                return InteractionKeyType.F;
        }

        return InteractionKeyType.None;
    }

    private string GetInteractionTextByState(ePlayerState state)
    {
        switch (state)
        {
            case ePlayerState.Fueling:
                return "연료 넣기";

            case ePlayerState.Crafting:
                return "제작하기";

            case ePlayerState.Steering:
                return "조종하기";
        }

        return string.Empty;
    }

    private string GetInteractionKeyString(InteractionKeyType keyType)
    {
        switch (keyType)
        {
            case InteractionKeyType.E:
                return "E";

            case InteractionKeyType.F:
                return "F";
        }

        return string.Empty;
    }

    public void BoatBreaker(Transform axe)
    {
        if (!_isSwinging)
        {
            Swing(axe);
        }

        PerformingChopping();
    }

    public void ResetOutline()
    {
        if(outlineCursor != null)
            outlineCursor.gameObject.SetActive(false);
    }

    public void Interact()
    {
        if (interactionState == ePlayerState.Steering)
        {
            if (_heldItem == null)
            {
                _isSteering = !_isSteering;

                InGameManager.Instance.OnChangedGameMode();

                RefreshInteractionUI();
            }
            return;
        }

        switch (interactionState)
        {
            case ePlayerState.None:
                if (_isSteering || player.InputLock)
                {
                    _isSteering = false;

                    InGameManager.Instance.OnChangedGameMode2();

                    RefreshInteractionUI();
                }
                if (_heldItem == null) return;

                break;

            case ePlayerState.Fueling:
                if (_heldItem != null)
                {
                    OnRefuel();
                }
                break;

            case ePlayerState.Crafting:
                OnCraft();
                break;

            default:
                break;
        }
    }

    public void ApplyWoodPatch()
    {
        // 나무블록 설치 로직 작성
        TryPickUp();
    }

    public void EndSteering()
    {
        _isSteering = false;
        RefreshInteractionUI();
    }

    private void Swing(Transform axe)
    {
        if (_isSwinging) return;

        _isSwinging = true;

        Sequence sequence = DOTween.Sequence();

        // 앞으로 휘두르기
        sequence.Append(axe.DOLocalRotate(
            new Vector3(swingAngle, 0f, 0f), swingDuration)
            .SetEase(Ease.OutQuart));

        // 원래 위치로 복귀
        sequence.Append(axe.DOLocalRotate(
            Vector3.zero, returnDuration)
            .SetEase(Ease.InOutQuad));

        sequence.OnComplete(() => _isSwinging = false);
    }

    public void TryPickUp()
    {
        Ray ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.red, 1f);

        RaycastHit hit;
        bool didHit = Physics.Raycast(ray, out hit, pickupRange, itemLayer, QueryTriggerInteraction.Ignore);
        if (!didHit)
        {
            return;
        }

        BaseResource item = hit.collider.GetComponentInParent<BaseResource>();
        if (item == null)
        {
            return;
        }

        PickUpItem(item.gameObject);
    }

    private void PickUpItem(GameObject item)
    {
        if (_heldItem != null) return;

        if (!item.TryGetComponent<BaseResource>(out BaseResource poolObj))
        {
            return;
        }

        NetBlock selfNet;
        bool isSelfNet = poolObj.TryGetComponent<NetBlock>(out selfNet);

        if (!isSelfNet)
        {
            NetBlock parentNet = poolObj.GetComponentInParent<NetBlock>();
            if (parentNet != null)
            {
                parentNet.ReleaseCaughtItem(poolObj);
            }
        }

        axeOverlay.gameObject.SetActive(false);
        player.isHoldAxe = false;
        poolObj.IsEquipped = true;

        if (isSelfNet && selfNet != null)
        {
            selfNet.SetInstalled(false);
        }

        poolObj.transform.SetParent(null);
        Transform[] allChildren = item.GetComponentsInChildren<Transform>(true);
        int childCount = allChildren.Length;

        for (int i = 0; i < childCount; i++)
        {
            allChildren[i].gameObject.layer = _overlayLayer;
        }

        poolObj.transform.position = grabObjectPos;
        poolObj.transform.eulerAngles = grabObjectRot;

        poolObj.rb.useGravity = false;
        poolObj.rb.isKinematic = true;
        poolObj.rb.linearVelocity = Vector3.zero;
        poolObj.rb.angularVelocity = Vector3.zero;

        poolObj.coll.isTrigger = true;

        if (!poolObj.IsCollected)
        {
            RepoManager.Instance.Register(poolObj);
        }

        poolObj.IsCollected = true;

        currentItemOverlay = item;
        _heldItem = poolObj;
    }


    private void PickUpItem(BaseResource item)
    {

        NetBlock parentNet = item.GetComponentInParent<NetBlock>();
        if (parentNet != null)
        {
            parentNet.ReleaseCaughtItem(item);
        }


        axeOverlay.gameObject.SetActive(false);
        player.isHoldAxe = false;
        item.IsEquipped = true;

        NetBlock netBlockB;
        bool pickedNetB = item.TryGetComponent<NetBlock>(out netBlockB);
        if (pickedNetB && netBlockB != null)
        {
            netBlockB.SetInstalled(false);
        }
        //item.gameObject.layer = _overlayLayer;
        //item.transform.SetParent(playerEntity.transform);
        //item.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
        //item.transform.localRotation = Quaternion.identity; // [추가] 들었을 때 회전값 초기화

        item.transform.SetParent(null);
        Transform[] allChildren = item.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = _overlayLayer;
        }

        item.transform.position = grabObjectPos;
        item.transform.eulerAngles = grabObjectRot;

        item.rb.useGravity = false;
        item.rb.isKinematic = true; // [핵심 추가] 들고 있을 때는 물리 연산 완전 비활성화
        if (!item.IsCollected)
            RepoManager.Instance.Register(item);
        item.IsCollected = true;
        item.coll.isTrigger = true;

        currentItemOverlay = item.gameObject;
        _heldItem = item; // 캐싱된 컴포넌트 할당


    }

    public void DropItem()
    {
        if (_heldItem == null) return;
        if (!_heldItem.TryGetComponent<BaseResource>(out var item)) return;

        Vector3 dropPos = FindSafeDropPosition(item.coll);
        Quaternion dropRot = transform.rotation * Quaternion.Euler(0f, 90f, 0f);

        item.IsEquipped = false;

        Transform[] allChildren = _heldItem.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = _defaultLayer;
        }

        // ★ 보간 비활성화
        item.rb.interpolation = RigidbodyInterpolation.None;
        item.rb.position = dropPos;
        item.rb.rotation = dropRot;
        item.coll.isTrigger = false;

        Physics.SyncTransforms();

        if (IsOverlapping(item.coll))
        {
            dropPos = ResolveOverlap(dropPos, item.coll);
            item.rb.position = dropPos;
            Physics.SyncTransforms();
        }

        item.rb.isKinematic = false;
        item.rb.useGravity = true;
        item.rb.linearVelocity = Vector3.zero;
        item.rb.angularVelocity = Vector3.zero;

        // ★ DropItem에서만 보간 복원
        item.rb.interpolation = RigidbodyInterpolation.Interpolate;

        Collider playerColl = player.GetComponent<Collider>();
        if (playerColl != null)
        {
            Physics.IgnoreCollision(playerColl, item.coll, true);
            StartCoroutine(ReenableCollision(playerColl, item.coll, 0.5f));
        }

        player.isHoldAxe = true;
        if (axeOverlay != null) axeOverlay.SetActive(true);
        currentItemOverlay = axeOverlay;
        _heldItem = null;
    }

    private Vector3 FindSafeDropPosition(Collider itemColl)
    {
        Vector3 halfExtents = itemColl.bounds.extents;
        float itemHeight = halfExtents.y;

        // 바닥 높이 찾기
        float groundY = transform.position.y;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 3f, blockLayer))
        {
            groundY = groundHit.point.y;
        }

        // 아이템이 바닥에 살짝 떠있도록 (pivot 기준)
        float spawnY = groundY + itemHeight + 0.05f;

        // 1. 전방 거리별 체크
        float[] distances = { 1.5f, 2f, 2.5f, 3f };
        foreach (float dist in distances)
        {
            Vector3 candidate = new Vector3(
                transform.position.x + transform.forward.x * dist,
                spawnY,
                transform.position.z + transform.forward.z * dist
            );

            if (IsPositionClear(candidate, halfExtents, itemColl))
            {
                return candidate;
            }
        }

        // 2. 측면 체크
        Vector3[] sideOffsets = { transform.right, -transform.right };
        foreach (Vector3 side in sideOffsets)
        {
            Vector3 candidate = new Vector3(
                transform.position.x + side.x * 1.5f,
                spawnY,
                transform.position.z + side.z * 1.5f
            );

            if (IsPositionClear(candidate, halfExtents, itemColl))
            {
                return candidate;
            }
        }

        // 3. 후방 체크
        Vector3 backCandidate = new Vector3(
            transform.position.x - transform.forward.x * 1.5f,
            spawnY,
            transform.position.z - transform.forward.z * 1.5f
        );

        if (IsPositionClear(backCandidate, halfExtents, itemColl))
        {
            return backCandidate;
        }

        // 4. 최후의 수단: 머리 위
        return transform.position + Vector3.up * 2.5f;
    }

    private bool IsPositionClear(Vector3 pos, Vector3 halfExtents, Collider selfColl)
    {
        // Y축 약간 줄여서 바닥 오탐 방지
        Vector3 checkExtents = new Vector3(halfExtents.x, halfExtents.y * 0.8f, halfExtents.z);

        Collider[] overlaps = Physics.OverlapBox(pos, checkExtents, transform.rotation, blockLayer | itemLayer);

        // 자기 자신 제외
        foreach (Collider col in overlaps)
        {
            if (col != selfColl)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsOverlapping(Collider itemColl)
    {
        Vector3 center = itemColl.bounds.center;
        Vector3 halfExtents = itemColl.bounds.extents * 0.9f; // 약간 작게

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, itemColl.transform.rotation, blockLayer);
        return overlaps.Length > 0;
    }

    private Vector3 ResolveOverlap(Vector3 currentPos, Collider itemColl)
    {
        // 위로 조금씩 올리면서 빈 공간 찾기
        Vector3 halfExtents = itemColl.bounds.extents;

        for (int i = 1; i <= 10; i++)
        {
            Vector3 testPos = currentPos + Vector3.up * (0.3f * i);
            if (!Physics.CheckBox(testPos, halfExtents, itemColl.transform.rotation, blockLayer | itemLayer))
            {
                return testPos;
            }
        }

        // 그래도 안되면 플레이어 위
        return transform.position + Vector3.up * 3f;
    }

    private IEnumerator ReenableCollision(Collider a, Collider b, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (a != null && b != null && b.gameObject.activeInHierarchy)
        {
            Physics.IgnoreCollision(a, b, false);
        }
    }
    public void OnChangedInteractionState(ePlayerState nextState)
    {
        if (nextState != ePlayerState.Steering)
        {
            _isSteering = false;
        }

        interactionState = nextState;
        RefreshInteractionUI();
    }

    public void OnRefuel()
    {
        if (_heldItem == null) return;
        if (!(_heldItem.type == ePoolType.Wood || _heldItem.type == ePoolType.WetWood))
            return;
        if(_heldItem.TryGetComponent<Wood>(out var wood))
        {
            InGameManager.Instance.OnRefuel(wood);

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.OnSpawnPool(ePoolType.Refuel.ToString(), InGameManager.Instance.Furnace.transform.position);
            }

            RepoManager.Instance.Unregister(_heldItem);
            wood.IsCollected = false;
            ObjectPoolManager.Instance.OnRelease(wood.key, wood);
            _heldItem = null;

            if (axeOverlay != null) axeOverlay.SetActive(true);
            player.isHoldAxe = true;
            currentItemOverlay = axeOverlay;
        }
    }

    public void OnCraft()
    {
        if (_heldItem == null)
        {
            var item = InGameManager.Instance.PopResource();
            if (item == null) return;

            PickUpItem(item);
        }
        else
        {
            if (!(_heldItem.type == ePoolType.Wood || _heldItem.type == ePoolType.Fabric))
                return;

            if (_heldItem.TryGetComponent<BaseResource>(out var item))
            {
                // ★ 보간 비활성화
                item.rb.interpolation = RigidbodyInterpolation.None;

                if (InGameManager.Instance.TryCrafting(item))
                {
                    _heldItem = null;
                    if (axeOverlay != null) axeOverlay.SetActive(true);
                    player.isHoldAxe = true;
                    currentItemOverlay = axeOverlay;
                }
            }
        }
    }

    private void PerformingChopping()
    {
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, reachDistance, blockLayer))
        {
            outlineCursor.SetActive(true);
            outlineCursor.transform.position = hit.collider.transform.position;
            outlineCursor.transform.rotation = boat.transform.rotation * Quaternion.Euler(0f, 90f, 0f);

            // 파티클 생성 및 위치 갱신
            if (_hittingParticle == null)
            {
                var poolObj = ObjectPoolManager.Instance.OnSpawnPool("Hitting", hit.point);
                if (poolObj != null)
                {
                    _hittingParticle = poolObj.GetComponent<ParticlePoolObject>();
                }
            }
            else
            {
                // 타격 지점으로 파티클 위치 계속 업데이트
                _hittingParticle.transform.position = hit.point;
            }

            if (_currentTargetBlock != hit.collider.transform)
            {
                _currentTargetBlock = hit.collider.transform;
                _currentChopTime = 0.0f;
            }

            _currentChopTime += Time.deltaTime;

            if (_currentChopTime >= chopTimeRequired)
            {
                var wood = ObjectPoolManager.Instance.OnSpawnResources<Wood>();
                RepoManager.Instance.Register(wood);
                wood.OnChangedWoodState(eWoodState.Dried);
                PickUpItem(wood);
                BreakBlock(hit.collider.gameObject);
                _currentTargetBlock = null;
                _currentChopTime = 0.0f;
            }
        }
        else
        {
            // 허공 쳐다보면 도끼질 멈춤
            StopChopping();
        }
    }

    public void StopChopping()
    {
        ResetOutline();
        ResetChopping();
    }

    private void ResetChopping()
    {
        // 파티클 즉시 종료 및 풀 반환
        if (_hittingParticle != null)
        {
            _hittingParticle.ForceRelease();
            _hittingParticle = null;
        }

        if (_currentChopTime > 0)
        {
            Debug.Log("도끼질 취소");
        }

        _currentChopTime = 0.0f;
        _currentTargetBlock = null;
    }

    private void BreakBlock(GameObject block)
    {
        targetGroup.RemoveMember(block.transform);

        outlineCursor.SetActive(false);
        Destroy(block);

        // 블록이 파괴될 때도 파티클 및 상태 초기화
        ResetChopping();
        
        InGameManager.Instance.boatCollUpdateAction?.Invoke();
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.OnSpawnPool(ePoolType.Break.ToString(), block.transform.position);
        }
    }

    public bool IsHoldingBuildWoodBlock()
    {
        if (_heldItem == null || currentItemOverlay == null)
        {
            return false;
        }

        BuildWoodBlock buildWoodBlock = currentItemOverlay.GetComponent<BuildWoodBlock>();
        if(buildWoodBlock == null)
        {
            return false;
        }

        if (buildWoodBlock.key != "BuildWoodBlock")
        {
            Debug.LogError("1");

            return false;
        }

        

        return true;
    }

    public bool TryConsumeHeldBuildWoodBlockForBuild()
    {
        if (!IsHoldingBuildWoodBlock())
        {
            return false;
        }

        ObjectPoolBase heldItem = _heldItem;
        RepoManager.Instance.Unregister(_heldItem);
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.OnRelease(heldItem.key, heldItem);
        }
        else
        {
            Destroy(heldItem.gameObject);
        }

        _heldItem = null;

        if (axeOverlay != null)
        {
            axeOverlay.SetActive(true);
            player.isHoldAxe = true;
        }

        currentItemOverlay = axeOverlay;
        return true;
    }
    public bool IsHoldingWetWood()
    {
        if (_heldItem == null)
        {
            return false;
        }

        Wood heldWood;
        bool isWood = _heldItem.TryGetComponent<Wood>(out heldWood);
        if (!isWood || heldWood == null)
        {
            return false;
        }

        return heldWood.CurState == eWoodState.Wet || heldWood.CurState == eWoodState.Drying;
    }

    public bool TryPlaceHeldWetWood(Vector3 worldPosition, Quaternion worldRotation, Transform parentOnBoat)
    {
        if (_heldItem == null) return false;

        if (!_heldItem.TryGetComponent<Wood>(out var heldWood)) return false;

        if (heldWood.CurState == eWoodState.Dried) return false;

        // ★ 보간 비활성화
        heldWood.rb.interpolation = RigidbodyInterpolation.None;

        heldWood.transform.SetParent(null);

        heldWood.transform.SetPositionAndRotation(worldPosition, worldRotation);

        Transform targetParent = parentOnBoat != null ? parentOnBoat : boatTr;
        heldWood.transform.SetParent(targetParent, true);

        if (heldWood.CurState == eWoodState.Wet)
        {
            heldWood.OnChangedWoodState(eWoodState.Drying);
        }

        Transform[] allChildren = heldWood.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            allChildren[i].gameObject.layer = _defaultLayer;
        }

        if (heldWood.coll != null)
        {
            heldWood.coll.isTrigger = false;
        }
        if (heldWood.rb != null)
        {
            heldWood.rb.isKinematic = true;
            heldWood.rb.useGravity = false;
        }

        heldWood.IsCollected = false;

        if (RepoManager.Instance != null)
        {
            RepoManager.Instance.RegisterWood(heldWood);
        }
        else
        {
            heldWood.PutResource();
        }

        _heldItem = null;
        if (axeOverlay != null) axeOverlay.SetActive(true);
        player.isHoldAxe = true;
        currentItemOverlay = axeOverlay;

        return true;
    }
    public bool IsHoldingNetBlock()
    {
        if (_heldItem == null || currentItemOverlay == null)
        {
            return false;
        }

        NetBlock netBlock;
        bool isNet = currentItemOverlay.TryGetComponent<NetBlock>(out netBlock);
        if (!isNet || netBlock == null)
        {
            return false;
        }

        return true;
    }

    public bool TryPlaceHeldNetBlock(Vector3 worldPosition, Quaternion worldRotation, Transform parentBlock)
    {
        if (_heldItem == null)
        {
            return false;
        }

        NetBlock heldNet;
        bool isNet = _heldItem.TryGetComponent<NetBlock>(out heldNet);
        if (!isNet || heldNet == null)
        {
            return false;
        }

        if (parentBlock != null)
        {
            heldNet.transform.SetParent(parentBlock, true);
        }

        heldNet.transform.SetPositionAndRotation(worldPosition, worldRotation);

        Rigidbody rootBoatRb = FindTopRigidbodyFrom(parentBlock);
        heldNet.BindBoatRigidbody(rootBoatRb);
        heldNet.SetInstalled(true);

        Transform[] allChildren = heldNet.GetComponentsInChildren<Transform>(true);
        int childCount = allChildren.Length;

        for (int i = 0; i < childCount; i++)
        {
            allChildren[i].gameObject.layer = _defaultLayer;
        }

        if (heldNet.coll != null)
        {
            heldNet.coll.isTrigger = true;
        }

        if (heldNet.rb != null)
        {
            heldNet.rb.isKinematic = true;
            heldNet.rb.useGravity = false;
            heldNet.rb.linearVelocity = Vector3.zero;
            heldNet.rb.angularVelocity = Vector3.zero;
        }

        heldNet.IsCollected = false;
        heldNet.IsEquipped = false;

        _heldItem = null;

        if (axeOverlay != null)
        {
            axeOverlay.SetActive(true);
        }

        player.isHoldAxe = true;
        currentItemOverlay = axeOverlay;

        return true;
    }

    private Rigidbody FindTopRigidbodyFrom(Transform start)
    {
        Transform current = start;
        Rigidbody found = null;

        while (current != null)
        {
            Rigidbody candidate = current.GetComponent<Rigidbody>();
            if (candidate != null)
            {
                found = candidate;
            }

            current = current.parent;
        }

        return found;
    }




}
