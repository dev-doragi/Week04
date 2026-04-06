using UnityEngine;
using UnityEngine.UIElements;

public class Wood : BaseResource
{

    [SerializeField] private float baseFuelAmount = 10f;
    [SerializeField] private float wetFuelAmount = 3f;

    [SerializeField] eWoodState curState;
    [SerializeField] private float dryTime = 5f;
    [SerializeField] private float curProgressTime;
    public float CurProgressTime => curProgressTime;

    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private MaterialPropertyBlock _propBlock;
    [SerializeField] private static readonly int WetnessId = Shader.PropertyToID("_Wetness");

    public override void OnSpawn()
    {
        base.OnSpawn();
        OnChangedWoodState(eWoodState.Wet);
    }

    public override void PutResource()
    {
        base.PutResource();
        if (curState != eWoodState.Wet) return;
        OnChangedWoodState(eWoodState.Drying);
    }

    public bool OnCheckedWet()
    {
        return curState == eWoodState.Wet;
    }

    public float GetRefuelAmount()
    {
        return curState == eWoodState.Dried ? baseFuelAmount : wetFuelAmount;
    }

    public void OnChangedWoodState(eWoodState nextState)
    {
        if (curState == nextState) return;

        curState = nextState;

        _renderer.GetPropertyBlock(_propBlock);
        switch (nextState)
        {
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
        _renderer.SetPropertyBlock(_propBlock);
    }

    public bool OnDryWood(float progressTime)
    {
        curProgressTime -= progressTime;

        if(dryTime <= 0)
        {
            curProgressTime = 0;
            OnChangedWoodState(eWoodState.Dried);
            return true;
        }

        return false;
    }
}
