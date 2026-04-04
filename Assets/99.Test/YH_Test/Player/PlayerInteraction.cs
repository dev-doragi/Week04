using UnityEngine;
using DG.Tweening;

public class PlayerInteraction : MonoBehaviour
{
    [Header("좌클릭(도끼)")]
    public float swingAngle = 80f;
    public float swingDuration = 0.2f;
    public float returnDuration = 0.15f;

    private bool _isSwinging = false;

    [Header("F키_아이템 줍기")]
    public GameObject axeOverlay;
    public GameObject currentItemOverlay;
    public float pickupRange = 2f;
    public LayerMask itemLayer;

    private GameObject _heldItem;
    private Camera _mainCamera;

    private int _overlayLayer;
    private int _defaultLayer;

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
    }

    public void Interact()
    {
        // if (아이템 근처면 줍기) else (불에 넣기) else (젖은 나무 말리기)
        TryPickUp();
    }

    public void ApplyWoodPatch()
    {
        // 나무블록 설치 로직 작성
    }

    private void Swing(Transform axe)
    {
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
        axeOverlay.gameObject.SetActive(false);

        item.layer = _overlayLayer;
        item.transform.position = new Vector3(-0.831f, 1.04f, -2.665f);
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Rigidbody>().useGravity = false;
        item.GetComponent<BoxCollider>().isTrigger = true;

        currentItemOverlay = item;
        //currentItemOverlay.SetActive(true);

        _heldItem = item;
    }

    public void DropItem()
    {
        if (_heldItem == null) return;

        _heldItem.layer = _defaultLayer;
        _heldItem.GetComponent<Rigidbody>().isKinematic = false;
        _heldItem.GetComponent<Rigidbody>().useGravity = true;
        _heldItem.GetComponent<BoxCollider>().isTrigger = false;

        _heldItem.transform.position = transform.position + transform.forward * 1f;
        _heldItem.GetComponent<Rigidbody>().AddForce(transform.forward * 2f + Vector3.up * 1f, ForceMode.Impulse);

        currentItemOverlay = axeOverlay;
        _heldItem = null;

        axeOverlay.SetActive(true);
    }
}
