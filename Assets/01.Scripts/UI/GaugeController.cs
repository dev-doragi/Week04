using TMPro;
using UnityEngine;

public class GaugeController : UIBase
{
    [Header("Source")]
    [SerializeField] private FireIntensityController _fireIntensityController;
    [SerializeField] private Boat _boat;

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
    private float _lastSpeedValue = -1f;

    public override void Setup()
    {
        if (_fireIntensityController == null || _boat == null)
            return;

        SetSource(_fireIntensityController, _boat);
    }

    private void Update()
    {
        float normalizedValue = _fireIntensityController.NormalizedIntensity;
        float currentSpeed = _boat.CurrentSpeed;

        if (!Mathf.Approximately(_lastNormalizedValue, normalizedValue) || !Mathf.Approximately(_lastSpeedValue, currentSpeed))
        {
            _lastNormalizedValue = normalizedValue;
            _lastSpeedValue = currentSpeed;
            Apply(normalizedValue, _lastSpeedValue);
        }
    }

    public void SetSource(FireIntensityController controller, Boat boat)
    {
        _fireIntensityController = controller;
        _boat = boat;
        _lastNormalizedValue = -1f;
        _lastSpeedValue = -1f;
    }

    public void Apply(float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);

        ApplyGaugeRotation(normalizedValue);
        ApplyFireVisual(normalizedValue);
    }

    public void Apply(float normalizedValue, float currentSpeed)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);

        ApplyGaugeRotation(normalizedValue);
        ApplyFireVisual(normalizedValue);
        ApplyText(currentSpeed);
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

    private void ApplyText(float speed)
    {
       if (_gaugeText == null)
           return;

       _gaugeText.text = $"{(int)speed} km/h";
    }
}