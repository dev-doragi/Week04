using System;
using UnityEngine;

[Serializable]
public class ObjectPoolBase : MonoBehaviour
{
    public string key;
    public int prevCount = 5;
    public ePoolType type;
    public bool IsEquipped;
    public virtual void Init() { }
    public virtual void Setup() { }
    public virtual void OnRelease() { }
    public virtual void OnSpawn() { }
}
