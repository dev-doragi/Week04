using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro 사용을 위해 필수 추가

public class CompassBar : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform compassBarTransform;
    public RectTransform northMarkerTransform;
    public RectTransform southMarkerTransform;
    public RectTransform eastMarkerTransform;
    public RectTransform westMarkerTransform;
    public RectTransform objectiveMarkerTransform;

    [Header("Distance UI")]
    [Tooltip("목적지 마커 자식으로 있는 거리 표시용 텍스트")]
    public TextMeshProUGUI objectiveDistanceText;

    [Header("Transform References")]
    public Transform cameraObjectTransform;      // 메인 카메라 (플레이어 시선)
    public Transform objectiveObjectTransform;

    [Header("Tick Settings")]
    public RectTransform tickPrefab;
    public int tickGapAngle = 15;
    private List<RectTransform> _tickPool = new List<RectTransform>();

    [Header("Compass Settings")]
    [Tooltip("나침반 UI가 렌더링할 전체 시야각")]
    public float visibleAngleRange = 180f;

    private void Start()
    {
        int spawnCount = 360 / tickGapAngle;
        for (int i = 0; i < spawnCount; i++)
        {
            RectTransform tick = Instantiate(tickPrefab, compassBarTransform);
            tick.gameObject.SetActive(false);
            _tickPool.Add(tick);
        }
    }

    private void Update()
    {
        if (cameraObjectTransform == null) return;

        UpdateMarkers();
        UpdateTickMarks();
    }

    private void UpdateMarkers()
    {
        // 1. 목적지 거리 및 위치 업데이트
        if (objectiveObjectTransform != null)
        {
            // 거리 계산 (실시간)
            float distance = Vector3.Distance(cameraObjectTransform.position, objectiveObjectTransform.position);

            // 마커 위치 설정 및 가시성 여부 반환
            bool isVisible = SetMarkerPosition(objectiveMarkerTransform, objectiveObjectTransform.position);

            // 나침반 영역 안에 있을 때만 텍스트 업데이트
            if (isVisible && objectiveDistanceText != null)
            {
                objectiveDistanceText.text = $"{Mathf.RoundToInt(distance)}m";
            }
        }
        else
        {
            objectiveMarkerTransform.gameObject.SetActive(false);
        }

        // 2. 절대 방위 업데이트
        SetMarkerDirection(northMarkerTransform, Vector3.forward);
        SetMarkerDirection(southMarkerTransform, Vector3.back);
        SetMarkerDirection(eastMarkerTransform, Vector3.right);
        SetMarkerDirection(westMarkerTransform, Vector3.left);
    }

    private void UpdateTickMarks()
    {
        for (int i = 0; i < _tickPool.Count; i++)
        {
            float targetAngle = i * tickGapAngle;

            if (targetAngle % 90 == 0)
            {
                _tickPool[i].gameObject.SetActive(false);
                continue;
            }

            Vector3 dir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            SetMarkerDirection(_tickPool[i], dir);
        }
    }

    // 반환 타입을 bool로 수정하여 가시성 상태를 외부(UpdateMarkers)에서 알 수 있게 함
    private bool SetMarkerPosition(RectTransform markerTransform, Vector3 worldPosition)
    {
        Vector3 directionToTarget = worldPosition - cameraObjectTransform.position;
        return SetMarkerDirection(markerTransform, directionToTarget);
    }

    private bool SetMarkerDirection(RectTransform markerTransform, Vector3 targetDirection)
    {
        Vector2 targetDir = new Vector2(targetDirection.x, targetDirection.z).normalized;
        Vector2 cameraForward = new Vector2(cameraObjectTransform.forward.x, cameraObjectTransform.forward.z).normalized;

        float angle = -Vector2.SignedAngle(cameraForward, targetDir);

        float compassPositionX = angle / (visibleAngleRange / 2f);

        // 절댓값이 1.0 이하일 때만 활성화 (나침반 UI 범위 내)
        if (Mathf.Abs(compassPositionX) <= 1.0f)
        {
            markerTransform.gameObject.SetActive(true);

            float xPos = (compassBarTransform.rect.width / 2f) * compassPositionX;
            markerTransform.anchoredPosition = new Vector2(xPos, 0f);
            return true;
        }
        else
        {
            markerTransform.gameObject.SetActive(false);
            return false;
        }
    }
}