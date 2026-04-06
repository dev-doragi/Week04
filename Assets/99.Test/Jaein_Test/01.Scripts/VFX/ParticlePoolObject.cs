using UnityEngine;

public enum eParticlePlayMode
{
    OneShot,
    Loop
}

public class ParticlePoolObject : ObjectPoolBase
{
    [SerializeField] private ParticleSystem _rootParticleSystem;
    [SerializeField] private eParticlePlayMode _playMode = eParticlePlayMode.OneShot;

    public override void Init()
    {
        if (_rootParticleSystem == null)
        {
            _rootParticleSystem = GetComponent<ParticleSystem>();
        }

        if (_rootParticleSystem == null)
        {
            Debug.LogError($"{name} : Root ParticleSystem이 없습니다.");
            return;
        }

        ParticleSystem.MainModule main = _rootParticleSystem.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public override void Setup()
    {
        if (_rootParticleSystem == null)
        {
            _rootParticleSystem = GetComponent<ParticleSystem>();
        }

        if (_rootParticleSystem == null)
            return;

        _rootParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _rootParticleSystem.Clear(true);
        _rootParticleSystem.Play(true);
    }

    public override void OnRelease()
    {
        if (_rootParticleSystem == null)
            return;

        _rootParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _rootParticleSystem.Clear(true);
    }

    public void StopLoop()
    {
        if (_playMode != eParticlePlayMode.Loop)
            return;

        if (_rootParticleSystem == null)
            return;

        _rootParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void ForceRelease()
    {
        if (ObjectPoolManager.Instance == null)
            return;

        OnRelease();
        ObjectPoolManager.Instance.OnRelease(key, this);
    }

    private void OnParticleSystemStopped()
    {
        if (ObjectPoolManager.Instance == null)
            return;

        OnRelease();
        ObjectPoolManager.Instance.OnRelease(key, this);
    }
}