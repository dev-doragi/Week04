using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using VInspector.Libs;

public class Crafting_Table : MonoBehaviour
{
    [SerializeField] private Stack<BaseResource> repoStack = new Stack<BaseResource>();
    [SerializeField] private Transform[] repoSlot = new Transform[3];
    [SerializeField] private int maxCount = 3;

    private int _overlayLayer;

    private void Start()
    {
        _overlayLayer = LayerMask.NameToLayer("Interact");
    }

    public BaseResource OnCheckedCrafting()
    {
        if (repoStack.Count == 0) return null;
         return OnCraftItem();
    }

    //TODO : 알맞는 데이터 풀링 리턴하기
    public BaseResource OnCraftItem()
    {
        var counts = repoStack.GroupBy(x => x.GetType())
                      .ToDictionary(g => g.Key, g => g.Count());

        // 1. 나무 3개인지 확인
        if (counts.TryGetValue(typeof(Wood), out int w) && w == 3)
        {
            ReturnToPool();
            return GetCraftItem<WoodBlock>();
        }
        // 2. 나무 1개 + 천 2개인지 확인
        else if (counts.TryGetValue(typeof(Wood), out int w1) && w1 == 1 &&
                 counts.TryGetValue(typeof(Fabric), out int c2) && c2 == 2)
        {
            ReturnToPool();
            return GetCraftItem<NetBlock>();
        }
        return PopResourceItem();
    }

    public BaseResource PopResourceItem()
    {
        if(repoStack.TryPop(out BaseResource item))
        {
            item.transform.localScale = Vector3.one;
            return item;
        }
        return null;
    }

    public bool OnPushItem(BaseResource newItem)
    {
        if (repoStack.Count > maxCount || !newItem.IsCraft) return false;

        Transform[] allChildren = newItem.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            child.gameObject.layer = _overlayLayer;
        }

        newItem.coll.isTrigger = true;
        newItem.transform.SetParent(repoSlot[repoStack.Count]);
        newItem.transform.localScale = Vector3.one * 0.5f;
        newItem.transform.localPosition = Vector3.zero;
        repoStack.Push(newItem);

        return true;
    }

    public T GetCraftItem<T>() where T : BaseResource 
    {
        return ObjectPoolManager.Instance.OnSpawnResources<T>();
    }

    private void ReturnToPool()
    {
        while (repoStack.Count > 0) 
        { 
            var item = repoStack.Pop();
            item.transform.localScale = Vector3.one;
            //TODO 부모바꿔놓기
            item.transform.SetParent(ObjectPoolManager.Instance.gameObject.transform);
            ObjectPoolManager.Instance.OnRelease(item.key, item);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            if(other.TryGetComponent(out PlayerInteraction player))
            {
                player.OnChangedInteractionState(ePlayerState.Crafting);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.TryGetComponent(out PlayerInteraction player))
            {
                player.OnChangedInteractionState(ePlayerState.None);
            }
        }
    }
}
