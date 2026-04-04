using TMPro;
using UnityEngine;

public class GaugeController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private FireIntensityController _fireIntensityController;

    [Header("Gauge Rotation")]
    [SerializeField] private Transform _gaugePivot;
    [SerializeField] private float _minAngle = 90f;
    [SerializeField] private float _maxAngle = -90f;

    [Header("Fire Visual")]
    [SerializeField] private Transform _fireVisual;
    [SerializeField] private Vector3 _minFireScale = Vector3.zero;
    [SerializeField] private Vector3 _maxFireScale = Vector3.one;

    [Header("Text")]
    [SerializeField] private TMP_Text _gaugeText;

    private float _lastNormalizedValue = -1f;

    private void Update()
    {
        if (_fireIntensityController == null)
            return;

        float normalizedValue = _fireIntensityController.NormalizedIntensity;

        if (Mathf.Approximately(_lastNormalizedValue, normalizedValue))
            return;

        _lastNormalizedValue = normalizedValue;
        Apply(normalizedValue);
    }

    public void SetSource(FireIntensityController controller)
    {
        _fireIntensityController = controller;
        _lastNormalizedValue = -1f;
    }

    private void Apply(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);

        ApplyGaugeRotation(normalizedValue);
        ApplyFireVisual(normalizedValue);
        ApplyText();
    }

    private void ApplyGaugeRotation(float normalizedValue)
    {
        if (_gaugePivot == null)
            return;

        float angle = Mathf.Lerp(_minAngle, _maxAngle, normalizedValue);
        _gaugePivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ApplyFireVisual(float normalizedValue)
    {
        if (_fireVisual == null)
            return;

        _fireVisual.localScale = Vector3.Lerp(_minFireScale, _maxFireScale, normalizedValue);

        if (_gaugePivot != null)
        {
            float pivotZ = _gaugePivot.localEulerAngles.z;

            if (pivotZ > 180f)
                pivotZ -= 360f;

            _fireVisual.localRotation = Quaternion.Euler(0f, 0f, -pivotZ);
        }
    }

    private void ApplyText()
    {
        if (_gaugeText == null)
            return;

        // TODO: 추후 배 속도 시스템과 연동해서 텍스트 표시
        _gaugeText.text = string.Empty;
    }
}