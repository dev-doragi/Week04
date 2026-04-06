using UnityEngine;

public class ShipAreaManager : MonoBehaviour
{
    [SerializeField] private BoxCollider shipAreaCollider;

    [Header("Padding Settings")]
    public Vector3 padding = new Vector3(0.5f, 2.0f, 0.5f); // 좌우, 높이, 앞뒤 여유분

    private Bounds _currentBounds; // 기즈모 표시용 저장 변수

    void Start()
    {
        if (shipAreaCollider == null)
            shipAreaCollider = GetComponent<BoxCollider>();
        
        InGameManager.Instance.boatCollUpdateAction -= AdjustCollider;
        InGameManager.Instance.boatCollUpdateAction += AdjustCollider;

        // 시작할 때 한 번 맞춤
        AdjustCollider();
    }

    [ContextMenu("Adjust Collider To Fit")]
    public void AdjustCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. 모든 자식 렌더러의 영역 합산
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // 2. 패딩(Padding) 적용
        // Expand는 양쪽으로 늘어나므로 입력값의 2배가 커집니다. 
        // 직접 크기를 제어하고 싶다면 아래처럼 하거나 combinedBounds.Expand를 사용하세요.
        _currentBounds = combinedBounds;

        // 3. 로컬 좌표로 변환하여 콜라이더에 적용
        Vector3 localCenter = transform.InverseTransformPoint(_currentBounds.center);
        Vector3 localSize = transform.InverseTransformVector(_currentBounds.size);

        shipAreaCollider.center = localCenter;
        // 패딩 적용: 절대값으로 변환 후 설정한 패딩 추가
        shipAreaCollider.size = new Vector3(
            Mathf.Abs(localSize.x) + padding.x,
            Mathf.Abs(localSize.y) + padding.y,
            Mathf.Abs(localSize.z) + padding.z
        );
    }

    // 에디터 뷰에서 영역을 시각적으로 표시
    private void OnDrawGizmos()
    {
        if (shipAreaCollider == null) return;

        // 배의 현재 위치와 회전에 맞춰 기즈모 좌표계 설정
        Gizmos.matrix = transform.localToWorldMatrix;

        // 1. 반투명한 박스 (영역 내부)
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawCube(shipAreaCollider.center, shipAreaCollider.size);

        // 2. 진한 선 (테두리)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(shipAreaCollider.center, shipAreaCollider.size);
    }
}