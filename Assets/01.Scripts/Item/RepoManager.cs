using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepoManager : MonoBehaviour
{
    private static RepoManager instance;
    public static RepoManager Instance { get { return instance; } private set { instance = value; } }
    public bool IsPlaying = true;

    [SerializeField] private Dictionary<eItemType, int> storageDic = new();
    [SerializeField] private HashSet<Wood> dryingWoods = new HashSet<Wood>();
    private Queue<Wood> removalQueue = new Queue<Wood>();
    bool isDryUpdate = false;
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
            Destroy(this);
    }

    void Start()
    {
        storageDic.Add(eItemType.Wood, 0);
        storageDic.Add(eItemType.WetWood, 0);
        storageDic.Add(eItemType.Fabric, 0);
        storageDic.Add(eItemType.Block, 0);
        storageDic.Add(eItemType.Catcher, 0);
    }

    public void RegisterWood(Wood wood)
    {
        if (dryingWoods.Add(wood))
        {
            isDryUpdate = true;
        }
    }

    private void Update()
    {
        if (!isDryUpdate) return;

        float dt = Time.deltaTime;

        foreach (var wood in dryingWoods)
        {
            if (wood.OnDryWood(dt))
            {
                removalQueue.Enqueue(wood);
            }
        }

        if (removalQueue.Count > 0)
        {
            while (removalQueue.Count > 0)
            {
                var wood = removalQueue.Dequeue();
                dryingWoods.Remove(wood);
            }

            if (dryingWoods.Count == 0)
            {
                isDryUpdate = false;
            }
        }
    }

    public void GetResourceItem(BaseResource item)
    {
        if (item == null) return;
        if(storageDic.TryGetValue(item.type, out var storage))
        {
            storage += 1;
        }
    }

    public void RemoveResourceItem(BaseResource item)
    {
        if (item == null) return;
        if (storageDic.TryGetValue(item.type, out var storage))
        {
            storage -= 1;
        }
    }
}

