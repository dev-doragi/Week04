using UnityEngine;

[ExecuteAlways]
public class FireIntensityController : MonoBehaviour
{
    [System.Serializable]
    public class ParticleModuleSettings
    {
        [SerializeField] private ParticleSystem _particleSystem;

        [SerializeField, HideInInspector] private ParticleSystem _capturedParticleSystem;
        [SerializeField, HideInInspector] private bool _hasCapturedData;

        [Header("Captured Base Values")]
        [SerializeField] private Vector2 _lifeTimeRange;
        [SerializeField] private Vector2 _speedRange;
        [SerializeField] private Vector2 _sizeRange;
        [SerializeField] private float _emissionRate;
        [SerializeField] private float _shapeAngle;
        [SerializeField] private float _shapeRadius;

        public bool IsValid => _particleSystem != null && _hasCapturedData && _particleSystem == _capturedParticleSystem;

        public void InvalidateIfReferenceChanged()
        {
            if (_particleSystem != _capturedParticleSystem)
            {
                _hasCapturedData = false;
            }
        }

        public void Capture()
        {
            if (_particleSystem == null)
            {
                ClearCapturedData();
                return;
            }

            ParticleSystem.MainModule main = _particleSystem.main;
            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            ParticleSystem.ShapeModule shape = _particleSystem.shape;

            _lifeTimeRange = ReadRange(main.startLifetime);
            _speedRange = ReadRange(main.startSpeed);
            _sizeRange = ReadRange(main.startSize);

            _emissionRate = ReadConstant(emission.rateOverTime);
            _shapeAngle = shape.angle;
            _shapeRadius = shape.radius;

            _capturedParticleSystem = _particleSystem;
            _hasCapturedData = true;
        }

        public void Apply(float generalIntensity, float emissionIntensity)
        {
            if (!IsValid)
                return;

            ParticleSystem.MainModule main = _particleSystem.main;
            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            ParticleSystem.ShapeModule shape = _particleSystem.shape;

            main.startLifetime = CreateTwoConstantsCurve(_lifeTimeRange * generalIntensity);
            main.startSpeed = CreateTwoConstantsCurve(_speedRange * generalIntensity);
            main.startSize = CreateTwoConstantsCurve(_sizeRange * generalIntensity);

            emission.rateOverTime = _emissionRate * emissionIntensity;
            shape.angle = _shapeAngle * generalIntensity;
            shape.radius = _shapeRadius * generalIntensity;
        }

        public void ClearCapturedData()
        {
            _capturedParticleSystem = null;
            _hasCapturedData = false;
            _lifeTimeRange = Vector2.zero;
            _speedRange = Vector2.zero;
            _sizeRange = Vector2.zero;
            _emissionRate = 0f;
            _shapeAngle = 0f;
            _shapeRadius = 0f;
        }

        private static Vector2 ReadRange(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => new Vector2(curve.constant, curve.constant),
                ParticleSystemCurveMode.TwoConstants => new Vector2(curve.constantMin, curve.constantMax),
                _ => new Vector2(curve.constantMin, curve.constantMax)
            };
        }

        private static float ReadConstant(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.Constant => curve.constant,
                ParticleSystemCurveMode.TwoConstants => curve.constantMax,
                _ => curve.constant
            };
        }

        private static ParticleSystem.MinMaxCurve CreateTwoConstantsCurve(Vector2 range)
        {
            return new ParticleSystem.MinMaxCurve(range.x, range.y);
        }
    }

    [Header("Particle Modules")]
    [SerializeField] private ParticleModuleSettings _fire = new();
    [SerializeField] private ParticleModuleSettings _smoke = new();
    [SerializeField] private ParticleModuleSettings _ember = new();

    [Header("Optional Components")]
    [SerializeField] private Light _sparkBaseLight;
    [SerializeField] private AnimatedLightValue _animatedLightValue;
    [SerializeField] private AudioSource _audioSource;

    [Header("Control")]
    [Range(0f, 2.5f)]
    [SerializeField] private float _intensity = 1f;

    [Header("Editor Preview")]
    [SerializeField] private bool _previewInEditMode = false;

    [Header("Captured Base Values")]
    [SerializeField, HideInInspector] private float _baseAudioVolume = 1f;

    public float Intensity
    {
        get => _intensity;
        set
        {
            _intensity = Mathf.Clamp(value, 0f, 2.5f);
            TryApply();
        }
    }

    [ContextMenu("Capture Base Settings")]
    public void CaptureBaseSettings()
    {
        _fire.Capture();
        _smoke.Capture();
        _ember.Capture();

        if (_audioSource != null)
            _baseAudioVolume = _audioSource.volume;
    }

    [ContextMenu("Clear Captured Data")]
    public void ClearCapturedData()
    {
        _fire.ClearCapturedData();
        _smoke.ClearCapturedData();
        _ember.ClearCapturedData();
    }

    [ContextMenu("Apply Now")]
    public void ApplyNow()
    {
        TryApply(forceInEditor: true);
    }

    [ContextMenu("Restore Base Settings")]
    public void RestoreBaseSettings()
    {
        RestoreParticleModule(_fire);
        RestoreParticleModule(_smoke);
        RestoreParticleModule(_ember);

        ApplyLight(1f);

        if (_audioSource != null)
            _audioSource.volume = _baseAudioVolume;
    }

    private void OnEnable()
    {
        InvalidateChangedReferences();
        TryApply();
    }

    private void OnValidate()
    {
        //_intensity = Mathf.Clamp01(_intensity);
        InvalidateChangedReferences();

        if (!Application.isPlaying && _previewInEditMode)
        {
            TryApply(forceInEditor: true);
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            TryApply();
            return;
        }

        if (_previewInEditMode)
        {
            TryApply(forceInEditor: true);
        }
    }

    private void InvalidateChangedReferences()
    {
        _fire.InvalidateIfReferenceChanged();
        _smoke.InvalidateIfReferenceChanged();
        _ember.InvalidateIfReferenceChanged();
    }

    private void TryApply(bool forceInEditor = false)
    {
        if (!Application.isPlaying && !forceInEditor)
            return;

        float generalIntensity = _intensity;
        float emissionIntensity = _intensity;
        float emberEmissionStep = Mathf.Floor(_intensity * 4f) / 4f;

        if (_fire.IsValid)
            _fire.Apply(generalIntensity, emissionIntensity);

        if (_smoke.IsValid)
            _smoke.Apply(generalIntensity, emissionIntensity);

        if (_ember.IsValid)
            _ember.Apply(generalIntensity, emberEmissionStep);

        ApplyLight(_intensity);
        ApplyAudio();
    }

    private void ApplyLight(float overallIntensity)
    {
        if (_sparkBaseLight == null)
            return;

        float animatedBaseIntensity = 1f;

        if (_animatedLightValue != null)
            animatedBaseIntensity = _animatedLightValue.animatedIntensity;

        _sparkBaseLight.intensity = animatedBaseIntensity * overallIntensity;
    }

    private void ApplyAudio()
    {
        if (_audioSource == null)
            return;

        _audioSource.volume = _baseAudioVolume * _intensity;
    }

    private void RestoreParticleModule(ParticleModuleSettings module)
    {
        if (module == null || !module.IsValid)
            return;

        module.Apply(1f, 1f);
    }
}