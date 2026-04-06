using System.Collections.Generic;
using UnityEngine;

public class NetBlock : BaseResource
{
    [Header("Installed")]
    [SerializeField] private bool isInstalled = false;
    [SerializeField] private Transform catchRoot;
    [SerializeField] private Rigidbody boatRb;
    private Collider[] boatColliders;

    [Header("Catch")]
    [SerializeField] private int maxCatchCount = 8;
    [SerializeField] private int slotColumnCount = 3;
    [SerializeField] private Vector3 slotStartLocal = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 slotSpacing = new Vector3(0.35f, 0f, 0.35f);

    private readonly List<BaseResource> caughtItems = new List<BaseResource>();
    private readonly Dictionary<BaseResource, Vector3> caughtWorldScaleMap = new Dictionary<BaseResource, Vector3>();



    private void Awake()
    {
        if (catchRoot == null)
        {
            catchRoot = transform;
        }

        CacheBoatColliders();
    }

    private void OnDisable()
    {
        SetInstalled(false);
    }

    public void BindBoatRigidbody(Rigidbody targetBoatRb)
    {
        boatRb = targetBoatRb;
        CacheBoatColliders();
    }

    public void SetInstalled(bool value)
    {
        isInstalled = value;

        if (isInstalled)
        {
            if (boatRb == null || boatRb == rb)
            {
                boatRb = FindBoatRigidbodyInParents();
                CacheBoatColliders();
            }
            return;
        }

        ReleaseAllCaughtItems();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInstalled)
        {
            return;
        }

        BaseResource item = other.GetComponentInParent<BaseResource>();
        if (item == null || item == this)
        {
            return;
        }

        if (item.IsEquipped)
        {
            return;
        }

        NetBlock otherNet;
        bool isNet = item.TryGetComponent<NetBlock>(out otherNet);
        if (isNet)
        {
            return;
        }

        if (caughtItems.Contains(item))
        {
            return;
        }

        if (caughtItems.Count >= maxCatchCount)
        {
            return;
        }

        CatchItem(item);
    }

    private void LateUpdate()
    {
        if (!isInstalled)
        {
            return;
        }

        CompactCaughtList();
        RepositionCaughtItems();
    }

    private void CatchItem(BaseResource item)
    {
        Vector3 originalWorldScale = item.transform.lossyScale;

        caughtItems.Add(item);
        caughtWorldScaleMap[item] = originalWorldScale;

        item.IsEquipped = false;
        item.IsCollected = false;

        if (item.rb != null)
        {
            item.rb.linearVelocity = Vector3.zero;
            item.rb.angularVelocity = Vector3.zero;
            item.rb.isKinematic = true;
            item.rb.useGravity = false;
        }

        if (item.coll != null)
        {
            item.coll.isTrigger = false;
        }

        item.transform.SetParent(catchRoot, true);
        item.transform.position = GetSlotWorldPosition(caughtItems.Count - 1);
        item.transform.rotation = catchRoot.rotation;
        
        ApplyWorldScale(item.transform, originalWorldScale);


        SetIgnoreCollisionWithBoat(item, true);

        int interactLayer = LayerMask.NameToLayer("Interact");
        if (interactLayer >= 0)
        {
            Transform[] allChildren = item.GetComponentsInChildren<Transform>(true);
            int childCount = allChildren.Length;

            for (int i = 0; i < childCount; i++)
            {
                allChildren[i].gameObject.layer = interactLayer;
            }
        }
    }

    public void ReleaseCaughtItem(BaseResource item)
    {
        ReleaseCaughtItemInternal(item, false);
    }

    private void ReleaseAllCaughtItems()
    {
        for (int i = caughtItems.Count - 1; i >= 0; i--)
        {
            BaseResource item = caughtItems[i];
            ReleaseCaughtItemInternal(item, true);
        }
        caughtItems.Clear();
        caughtWorldScaleMap.Clear();

    }

    private void ReleaseCaughtItemInternal(BaseResource item, bool restorePhysics)
{
    if (item == null)
    {
        return;
    }

    Vector3 keepWorldScale;
    bool hasScale = caughtWorldScaleMap.TryGetValue(item, out keepWorldScale);
    if (!hasScale)
    {
        keepWorldScale = item.transform.lossyScale;
    }

    caughtItems.Remove(item);
    caughtWorldScaleMap.Remove(item);
    SetIgnoreCollisionWithBoat(item, false);

    if (item.transform.parent == catchRoot)
    {
        item.transform.SetParent(null, true);
        ApplyWorldScale(item.transform, keepWorldScale);
    }

    if (restorePhysics)
    {
        if (item.rb != null)
        {
            item.rb.isKinematic = false;
            item.rb.useGravity = true;
            item.rb.linearVelocity = Vector3.zero;
            item.rb.angularVelocity = Vector3.zero;
        }

        if (item.coll != null)
        {
            item.coll.isTrigger = false;
        }
    }
}

    private void CompactCaughtList()
    {
        for (int i = caughtItems.Count - 1; i >= 0; i--)
        {
            BaseResource item = caughtItems[i];

            if (item == null)
            {
                caughtItems.RemoveAt(i);
                continue;
            }

            if (item.IsEquipped)
            {
                caughtItems.RemoveAt(i);
                continue;
            }

            if (item.transform.parent != catchRoot)
            {
                caughtItems.RemoveAt(i);
                continue;
            }
        }
    }

    private void RepositionCaughtItems()
    {
        int count = caughtItems.Count;

        for (int i = 0; i < count; i++)
        {
            BaseResource item = caughtItems[i];
            if (item == null)
            {
                continue;
            }

            item.transform.position = GetSlotWorldPosition(i);
            item.transform.rotation = catchRoot.rotation;
        }
    }

    private Vector3 GetSlotWorldPosition(int index)
    {
        int column = Mathf.Max(1, slotColumnCount);
        int row = index / column;
        int col = index % column;

        float centeredX = (col - (column - 1) * 0.5f) * slotSpacing.x;

        Vector3 local = new Vector3(
            slotStartLocal.x + centeredX,
            slotStartLocal.y,
            slotStartLocal.z + row * slotSpacing.z
        );

        return catchRoot.TransformPoint(local);
    }

    private Rigidbody FindBoatRigidbodyInParents()
    {
        Transform current = transform.parent;
        Rigidbody found = null;

        while (current != null)
        {
            Rigidbody candidate = current.GetComponent<Rigidbody>();
            if (candidate != null)
            {
                found = candidate;
            }

            current = current.parent;
        }

        return found;
    }

    private void CacheBoatColliders()
    {
        if (boatRb == null)
        {
            boatColliders = null;
            return;
        }

        boatColliders = boatRb.GetComponentsInChildren<Collider>(true);
    }

    private void SetIgnoreCollisionWithBoat(BaseResource item, bool ignore)
    {
        if (item == null || boatColliders == null)
        {
            return;
        }

        Collider[] itemColliders = item.GetComponentsInChildren<Collider>(true);
        int itemCount = itemColliders.Length;
        int boatCount = boatColliders.Length;

        for (int i = 0; i < itemCount; i++)
        {
            Collider itemCol = itemColliders[i];
            if (itemCol == null)
            {
                continue;
            }

            for (int j = 0; j < boatCount; j++)
            {
                Collider boatCol = boatColliders[j];
                if (boatCol == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(itemCol, boatCol, ignore);
            }
        }
    }
    private void ApplyWorldScale(Transform target, Vector3 worldScale)
    {
        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        float x = Mathf.Abs(parentScale.x) < 0.0001f ? worldScale.x : worldScale.x / parentScale.x;
        float y = Mathf.Abs(parentScale.y) < 0.0001f ? worldScale.y : worldScale.y / parentScale.y;
        float z = Mathf.Abs(parentScale.z) < 0.0001f ? worldScale.z : worldScale.z / parentScale.z;

        target.localScale = new Vector3(x, y, z);
    }

}
