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
    private int _buildLayer;
    private void Start()
    {
        _overlayLayer = LayerMask.NameToLayer("Interact");
        _buildLayer = LayerMask.NameToLayer("Building");
    }

    public BaseResource OnCheckedCrafting()
    {
        if (repoStack.Count == 0) return null;
         return OnCraftItem();
    }

    //TODO : �˸´� ������ Ǯ�� �����ϱ�
    public BaseResource OnCraftItem()
    {
        var counts = repoStack.GroupBy(x => x.GetType())
                      .ToDictionary(g => g.Key, g => g.Count());

        // 1. ���� 3������ Ȯ��
        if (counts.TryGetValue(typeof(Wood), out int w) && w == 3)
        {
            ReturnToPool();
            return GetCraftItem<BuildWoodBlock>();
        }
        // 2. ���� 1�� + õ 2������ Ȯ��
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
            ChangedLayerMask(item, _overlayLayer);
            item.transform.localScale = Vector3.one;
            return item;
        }
        return null;
    }

    public bool OnPushItem(BaseResource newItem)
    {
        if (newItem == null)
        {
            return false;
        }

        if (!newItem.IsCraft)
        {
            return false;
        }

        int slotIndex = repoStack.Count;

        if (slotIndex >= maxCount) return false;

        if (repoSlot == null || slotIndex >= repoSlot.Length || repoSlot[slotIndex] == null) return false;


        ChangedLayerMask(newItem, _buildLayer);

        if (newItem.coll != null)
        {
            newItem.coll.isTrigger = true;
        }

        newItem.transform.SetParent(repoSlot[slotIndex], false);
        newItem.transform.localScale = Vector3.one * 0.5f;
        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;

        repoStack.Push(newItem);
        
        return true;
    }

    public T GetCraftItem<T>() where T : BaseResource 
    {
        var obj = ObjectPoolManager.Instance.OnSpawnResources<T>();
        RepoManager.Instance.Register(obj);
        return obj;
    }

    private void ReturnToPool()
    {
        while (repoStack.Count > 0) 
        { 
            var item = repoStack.Pop();
            item.transform.localScale = Vector3.one;
            //TODO �θ�ٲ����
            item.transform.SetParent(ObjectPoolManager.Instance.gameObject.transform);

            ChangedLayerMask(item, _overlayLayer);

            RepoManager.Instance.Unregister(item);
            item.IsCollected = false;
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

    private void ChangedLayerMask(BaseResource item, int layer)
    {
        Transform[] allChildren = item.GetComponentsInChildren<Transform>(true);
        int childCount = allChildren.Length;

        for (int i = 0; i < childCount; i++)
        {
            allChildren[i].gameObject.layer = layer;
        }
    }
}
