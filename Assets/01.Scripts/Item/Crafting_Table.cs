using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VInspector.Libs;

public class Crafting_Table : MonoBehaviour
{
    [SerializeField] private Stack<BaseResource> repoStack = new Stack<BaseResource>();
    [SerializeField] private Transform[] repoSlot = new Transform[3];
    [SerializeField] private int maxCount = 3;

    //TODO : 알맞는 데이터 풀링 리턴하기
    public BaseResource OnCraftItem()
    {
        var counts = repoStack.GroupBy(x => x.GetType())
                      .ToDictionary(g => g.Key, g => g.Count());

        // 1. 나무 3개인지 확인
        if (counts.TryGetValue(typeof(Wood), out int w) && w == 3)
        {
            repoStack.Clear();
            return null;
        }
        // 2. 나무 1개 + 천 2개인지 확인
        else if (counts.TryGetValue(typeof(Wood), out int w1) && w1 == 1 &&
                 counts.TryGetValue(typeof(Fabric), out int c2) && c2 == 2)
        {
            repoStack.Clear();
            return null;
        }
        return null;
    }

    public BaseResource PopResourceItem()
    {
        if (repoStack.Count == 0) return null;
        if(repoStack.TryPop(out BaseResource item))
        {
            return item;
        }
        return null;
    }

    public bool OnPushItem(BaseResource newItem)
    {
        if (repoStack.Count > maxCount || !newItem.IsCraft) return false;


        newItem.transform.position = repoSlot[repoStack.Count].position;
        repoStack.Push(newItem);

        return true;
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
