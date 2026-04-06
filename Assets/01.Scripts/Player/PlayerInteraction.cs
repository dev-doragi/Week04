using UnityEngine;
using DG.Tweening;
using UnityEditor.UIElements;

public class PlayerInteraction : MonoBehaviour
{
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
    public float pickupRange = 2f;
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

    private ObjectPoolBase _heldItem;
    private Camera _mainCamera;

    private int _overlayLayer;
    private int _defaultLayer;

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
        // [추가] 가장 먼저 ObjectPoolBase 컴포넌트가 있는지 확인 (안전성 확보)
        if (!item.TryGetComponent<ObjectPoolBase>(out var poolObj))
        {
            Debug.LogError("픽업 실패: 이 프리팹에는 ObjectPoolBase 컴포넌트가 없습니다! 이름: " + item.name);
            return;
        }

        axeOverlay.gameObject.SetActive(false);

        //item.transform.SetParent(playerEntity.transform);
        item.gameObject.layer = _overlayLayer;
        //item.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
        //item.transform.localRotation = Quaternion.identity; // [추가] 들었을 때 회전값 초기화
        item.transform.position = grabObjectPos;
        item.transform.eulerAngles = grabObjectRot;

        var itemRb = item.GetComponent<Rigidbody>();
        itemRb.useGravity = false;
        itemRb.isKinematic = true; // [핵심 추가] 들고 있을 때는 물리 연산 완전 비활성화

        var itemColl = item.GetComponent<BoxCollider>();
        itemColl.isTrigger = true;

        currentItemOverlay = item;
        _heldItem = poolObj; // 캐싱된 컴포넌트 할당
    }

    private void PickUpItem(BaseResource item)
    {
        // [추가] 가장 먼저 ObjectPoolBase 컴포넌트가 있는지 확인 (안전성 확보)
        if (!item.TryGetComponent<ObjectPoolBase>(out var poolObj)) return;

        axeOverlay.gameObject.SetActive(false);

        item.gameObject.layer = _overlayLayer;
        item.transform.SetParent(playerEntity.transform);
        item.transform.localPosition = new Vector3(0.5f, 0.5f, 1f);
        item.transform.localRotation = Quaternion.identity; // [추가] 들었을 때 회전값 초기화

        item.rb.useGravity = false;
        item.rb.isKinematic = true; // [핵심 추가] 들고 있을 때는 물리 연산 완전 비활성화

        item.coll.isTrigger = true;

        currentItemOverlay = item.gameObject;
        _heldItem = poolObj; // 캐싱된 컴포넌트 할당
    }

    public void DropItem()
    {
        if (_heldItem == null) return;

        Rigidbody itemRb = _heldItem.GetComponent<Rigidbody>();
        BoxCollider itemColl = _heldItem.GetComponent<BoxCollider>();

        // 1. 부모 해제 및 레이어 복구
        _heldItem.transform.SetParent(boatTr);
        _heldItem.gameObject.layer = _defaultLayer;

        // 2. 위치 설정 (플레이어 정면 바닥 쪽)
        // 플레이어 중심에서 앞쪽으로 1m, 위쪽으로 0.2m 지점 계산
        Vector3 dropPos = transform.position + (transform.forward * 1.0f) + (Vector3.up * 0.2f);

        // 유니티 6 키네마틱 물체는 MovePosition이 가장 안전함
        itemRb.MovePosition(dropPos);
        itemRb.rotation = Quaternion.identity; // 떨굴 때 회전 초기화 (똑바로 서게 함)

        // 3. 물리 속성 변경
        itemColl.isTrigger = false;

        // 4. [핵심] 정지 상태 강제 주입
        // 던지는 힘을 없애기 위해 모든 속도를 0으로 초기화
        itemRb.isKinematic = false;
        itemRb.useGravity = true;
        itemRb.linearVelocity = Vector3.zero;
        itemRb.angularVelocity = Vector3.zero;

        // 5. UI 및 상태 업데이트
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
        if (!(_heldItem.type == eItemType.Wood || _heldItem.type == eItemType.WetWood))
            return;
        if(_heldItem.TryGetComponent<Wood>(out var wood))
        {
            InGameManager.Instance.OnRefuel(wood);
            ObjectPoolManager.Instance.OnRelease(wood.key, wood);
            _heldItem = null;

            if (axeOverlay != null) axeOverlay.SetActive(true);
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
            if (!(_heldItem.type == eItemType.Wood || _heldItem.type == eItemType.Fabric))
                return;

            if (_heldItem.TryGetComponent<BaseResource>(out var item))
            {
                if (InGameManager.Instance.TryCrafting(item))
                {
                    _heldItem = null;

                    if (axeOverlay != null) axeOverlay.SetActive(true);
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
            outlineCursor.transform.rotation = boat.transform.rotation;

            if (_currentTargetBlock != hit.transform)
            {
                _currentTargetBlock = hit.transform;
                _currentChopTime = 0.0f;
            }

            _currentChopTime += Time.deltaTime;

            if (_currentChopTime >= chopTimeRequired)
            {
                GameObject singleWood = Instantiate(woodOnePiece);
                PickUpItem(singleWood);
                BreakBlock(hit.collider.gameObject);
            }
        }
        else
        {
            // 허공 쳐다보면 도끼질 멈춤
            outlineCursor.SetActive(false);
            ResetChopping();
        }
    }

    private void ResetChopping()
    {
        if (_currentChopTime > 0)
        {
            Debug.Log("도끼질 취소");
        }

        _currentChopTime = 0.0f;
        _currentTargetBlock = null;
    }

    private void BreakBlock(GameObject block)
    {
        outlineCursor.SetActive(false);
        Destroy(block);
        ResetChopping();
    }
}
