using System.Collections.Generic;
using UnityEngine;

public class CompassBar : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform compassBarTransform;
    public RectTransform northMarkerTransform;
    public RectTransform southMarkerTransform;
    public RectTransform eastMarkerTransform;
    public RectTransform westMarkerTransform;
    public RectTransform objectiveMarkerTransform;

    [Header("Transform References")]
    public Transform cameraObjectTransform;      // 메인 카메라 (플레이어 시선)
    public Transform objectiveObjectTransform;

    [Header("Tick Settings")]
    public RectTransform tickPrefab;
    public int tickGapAngle = 15;
    private List<RectTransform> _tickPool = new List<RectTransform>();

    [Header("Compass Settings")]
    [Tooltip("나침반 UI가 렌더링할 전체 시야각 (180으로 설정 시 바의 양 끝이 정확히 좌/우 90도를 의미함)")]
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
        // 목적지는 월드좌표 기반
        if (objectiveObjectTransform != null)
            SetMarkerPosition(objectiveMarkerTransform, objectiveObjectTransform.position);

        // 절대 방위
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

    private void SetMarkerPosition(RectTransform markerTransform, Vector3 worldPosition)
    {
        Vector3 directionToTarget = worldPosition - cameraObjectTransform.position;
        SetMarkerDirection(markerTransform, directionToTarget);
    }

    private void SetMarkerDirection(RectTransform markerTransform, Vector3 targetDirection)
    {
        Vector2 targetDir = new Vector2(targetDirection.x, targetDirection.z).normalized;
        Vector2 cameraForward = new Vector2(cameraObjectTransform.forward.x, cameraObjectTransform.forward.z).normalized;

        float angle = -Vector2.SignedAngle(cameraForward, targetDir);

        float compassPositionX = angle / (visibleAngleRange / 2f);

        if (Mathf.Abs(compassPositionX) <= 1.0f)
        {
            markerTransform.gameObject.SetActive(true);

            float xPos = (compassBarTransform.rect.width / 2f) * compassPositionX;
            markerTransform.anchoredPosition = new Vector2(xPos, 0f);
        }
        else
        {
            markerTransform.gameObject.SetActive(false);
        }
    }
}