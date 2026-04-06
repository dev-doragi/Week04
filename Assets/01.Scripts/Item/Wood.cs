using UnityEngine;

public class Wood : BaseResource
{
    [Header("Fuel")]
    [SerializeField] private float driedFuelAmount = 10f;
    [SerializeField] private float wetFuelAmount = 3f;

    [Header("Drying")]
    [SerializeField] private eWoodState curState = eWoodState.Dried;
    [SerializeField] private float dryTime = 5f;
    [SerializeField] private float curProgressTime = 0f;
    public float CurProgressTime => curProgressTime;
    public eWoodState CurState => curState;

    [Header("Visual")]
    [SerializeField] private MeshRenderer targetRenderer;
    [SerializeField] private string wetnessPropertyName = "_Wetness";
    [SerializeField] private float wetnessWhenWet = 2f;
    [SerializeField] private float wetnessWhenDried = 1f;

    private MaterialPropertyBlock propBlock;
    private int wetnessId;

    private void Awake()
    {
        CacheReferences();
        ApplyVisualByState();
    }

    public override void Setup()
    {
        base.Setup();
        CacheReferences();

        if (type == ePoolType.WetWood)
        {
            curState = eWoodState.Wet;
            curProgressTime = Mathf.Max(0.01f, dryTime);
        }
        else
        {
            curState = eWoodState.Dried;
            curProgressTime = 0f;
        }

        ApplyVisualByState();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        OnChangedWoodState(eWoodState.Wet);
    }

    public override void PutResource()
    {
        base.PutResource();

        if (curState == eWoodState.Wet)
        {
            StartDrying();
        }
    }

    public bool OnCheckedWet()
    {
        return curState == eWoodState.Wet;
    }

    public float GetRefuelAmount()
    {
        if (curState == eWoodState.Dried)
        {
            return driedFuelAmount;
        }

        return wetFuelAmount;
    }

    public void OnChangedWoodState(eWoodState nextState)
    {
        if (curState == nextState)
        {
            return;
        }

        curState = nextState;

        if (curState == eWoodState.Wet)
        {
            curProgressTime = Mathf.Max(0.01f, dryTime);
            case eWoodState.Drying:
                IsCraft = false;
                break;

            case eWoodState.Wet:
                _propBlock.SetFloat(WetnessId, 2);
                IsCraft = false;
                break;

            case eWoodState.Dried:
                _propBlock.SetFloat(WetnessId, 1);
                IsCraft = true;
                break;
        }
        else if (curState == eWoodState.Dried)
        {
            curProgressTime = 0f;
        }
        else if (curState == eWoodState.Drying && curProgressTime <= 0f)
        {
            curProgressTime = Mathf.Max(0.01f, dryTime);
        }

        if (curState == eWoodState.Drying && RepoManager.Instance != null)
        {
            RepoManager.Instance.RegisterWood(this);
        }

        ApplyVisualByState();
    }

    public bool OnDryWood(float progressTime)
    {
        if (curState != eWoodState.Drying)
        {
            return false;
        }

        curProgressTime -= Mathf.Max(0f, progressTime);

        if (curProgressTime <= 0f)
        {
            curProgressTime = 0f;
            OnChangedWoodState(eWoodState.Dried);
            return true;
        }

        ApplyVisualByDryProgress();
        return false;
    }

    private void StartDrying()
    {
        curState = eWoodState.Drying;
        curProgressTime = Mathf.Max(curProgressTime, Mathf.Max(0.01f, dryTime));

        if (RepoManager.Instance != null)
        {
            RepoManager.Instance.RegisterWood(this);
        }

        ApplyVisualByDryProgress();
    }

    private void CacheReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (coll == null)
        {
            coll = GetComponent<BoxCollider>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        wetnessId = Shader.PropertyToID(wetnessPropertyName);
    }

    private void ApplyVisualByState()
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propBlock);

        if (curState == eWoodState.Wet)
        {
            propBlock.SetFloat(wetnessId, wetnessWhenWet);
        }
        else if (curState == eWoodState.Dried)
        {
            propBlock.SetFloat(wetnessId, wetnessWhenDried);
        }
        else
        {
            float ratio = 1f - Mathf.Clamp01(curProgressTime / Mathf.Max(0.01f, dryTime));
            float wetness = Mathf.Lerp(wetnessWhenWet, wetnessWhenDried, ratio);
            propBlock.SetFloat(wetnessId, wetness);
        }

        targetRenderer.SetPropertyBlock(propBlock);
    }

    private void ApplyVisualByDryProgress()
    {
        if (targetRenderer == null)
        {
            return;
        }

        float ratio = 1f - Mathf.Clamp01(curProgressTime / Mathf.Max(0.01f, dryTime));
        float wetness = Mathf.Lerp(wetnessWhenWet, wetnessWhenDried, ratio);

        targetRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(wetnessId, wetness);
        targetRenderer.SetPropertyBlock(propBlock);
    }
}
