using DG.Tweening;
using Unity.Cinemachine;
using UnityEditor.UIElements;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

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

    private ParticlePoolObject _hittingParticle;

    private IInteractable _currentInteractable;
    private GameObject _currentInteractableObject;

    [SerializeField] private PlayerEntity playerEntity;
    [SerializeField] private Transform boatTr;
    [SerializeField] private ePlayerState interactionState;
    private void Start()
    {
        _mainCamera = Camera.main;
        _overlayLayer = LayerMask.NameToLayer("ViewModel");
        _defaultLayer = LayerMask.NameToLayer("Interact");

        currentItemOverlay = axeOverlay;
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
        outlineCursor.gameObject.SetActive(false);
    }

    public void Interact()
    {
        // if (아이템 근처면 줍기) else (불에 넣기) else (젖은 나무 말리기)
        switch (interactionState)
        {
            case ePlayerState.None:
                if (_heldItem == null) return;
                break;
            case ePlayerState.Fueling:
                if (_heldItem == null) return;
                //사라지게 만들고 연료 추가
                OnRefuel();
                break;
            case ePlayerState.Crafting:
                //사라지게 만들고 크래프팅 쪽에 정보 넣어주기
                OnCraft();
                break;
            case ePlayerState.Steering:
                if (_heldItem != null) return;
                InGameManager.Instance.OnChangedGameMode();
                break;
        }
    }

    public void ApplyWoodPatch()
    {
        // 나무블록 설치 로직 작성
        TryPickUp();
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

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, itemLayer))
        {
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 1f);
            PickUpItem(hit.collider.gameObject);
        }
    }

    private void PickUpItem(GameObject item)
    {
        if(_heldItem != null) return;

        // [추가] 가장 먼저 ObjectPoolBase 컴포넌트가 있는지 확인 (안전성 확보)
        if (!item.TryGetComponent<BaseResource>(out var poolObj))
        {
            return;
        }

        axeOverlay.gameObject.SetActive(false);
        player.isHoldAxe = false;
        poolObj.IsEquipped = true;
        //item.transform.SetParent(playerEntity.transform);
        //item.gameObject.layer = _overlayLayer;
        //item.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
        //item.transform.localRotation = Quaternion.identity; // [추가] 들었을 때 회전값 초기화

        poolObj.transform.SetParent(null);
        Transform[] allChildren = item.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = _overlayLayer;
        }

        poolObj.transform.position = grabObjectPos;
        poolObj.transform.eulerAngles = grabObjectRot;

        poolObj.rb.useGravity = false;
        poolObj.rb.isKinematic = true; // [핵심 추가] 들고 있을 때는 물리 연산 완전 비활성화

        poolObj.coll.isTrigger = true;
        if (!poolObj.IsCollected)
            RepoManager.Instance.Register(poolObj);
        poolObj.IsCollected = true;

        currentItemOverlay = item;
        _heldItem = poolObj; // 캐싱된 컴포넌트 할당


    }

    private void PickUpItem(BaseResource item)
    {

        axeOverlay.gameObject.SetActive(false);
        player.isHoldAxe = false;
        item.IsEquipped = true;
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

        Debug.Log(_heldItem.gameObject.name);
        if (!_heldItem.TryGetComponent<BaseResource>(out var item)) return;
        item.IsEquipped = false;
        // 1. 부모 해제 및 레이어 복구
        // _heldItem.transform.SetParent(boatTr);
        Transform[] allChildren = _heldItem.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = _defaultLayer;
        }

        item.rb.isKinematic = false;
        item.rb.useGravity = true;
        item.rb.linearVelocity = Vector3.zero;
        item.rb.angularVelocity = Vector3.zero;


        _heldItem.transform.position = transform.position + transform.forward * 1f;

        item.rb.rotation = Quaternion.identity;
        item.coll.isTrigger = false;


        // 5. UI 및 상태 업데이트
        player.isHoldAxe = true;
        if (axeOverlay != null) axeOverlay.SetActive(true);
        currentItemOverlay = axeOverlay;

        _heldItem = null;
    }

    public void OnChangedInteractionState(ePlayerState nextState)
    {
        interactionState = nextState;
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
            if(item == null) return;

            PickUpItem(item);
        }
        else
        {
            if (!(_heldItem.type == ePoolType.Wood || _heldItem.type == ePoolType.Fabric))
                return;

            if (_heldItem.TryGetComponent<BaseResource>(out var item))
            {
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

        if (heldWood.CurState == eWoodState.Dried)  
        {
            return false;
        }

        if (parentOnBoat != null)
        {
            heldWood.transform.SetParent(parentOnBoat, true);
        }

        heldWood.transform.SetPositionAndRotation(worldPosition, worldRotation);

        if (heldWood.CurState == eWoodState.Wet)
        {
            heldWood.OnChangedWoodState(eWoodState.Drying);
        }


        Transform[] allChildren = heldWood.GetComponentsInChildren<Transform>(true);
        int childCount = allChildren.Length;
        for (int i = 0; i < childCount; i++)
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
            heldWood.rb.linearVelocity = Vector3.zero;
            heldWood.rb.angularVelocity = Vector3.zero;
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

        if (axeOverlay != null)
        {
            axeOverlay.SetActive(true);
        }

        player.isHoldAxe = true;
        currentItemOverlay = axeOverlay;

        return true;
    }


}
