using UnityEngine;

[RequireComponent(typeof(ItemBuoyancy))]
public class BaseResource : ObjectPoolBase
{
    public bool IsBurn;
    public bool IsCraft;
    public bool IsCollected;

    public Rigidbody rb;
    public BoxCollider coll;
    public virtual void PutResource()
    {

    }

    public virtual void GetResource()
    {

    }

    public virtual void PlayerInteraction()
    {
        ReleasePool();
    }

    public virtual void ThrowResource()
    {
        ConvertToOrb();
        ReleasePool();
    }

    protected virtual void ConvertToOrb()
    {

    }

    public virtual void SinkObject()
    {

    }

    public virtual void ReleasePool()
    {

    }

    public virtual void OnSpawn()
    {

    }
}
