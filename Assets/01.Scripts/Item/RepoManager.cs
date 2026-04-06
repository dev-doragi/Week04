using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepoManager : MonoBehaviour
{
    private static RepoManager instance;
    public static RepoManager Instance { get { return instance; } }

    [Header("Status")]
    public bool isPlaying = true;

    // [����] ResourceItem�� poolKey(string)�� ������� �� ���� �ǽð� �ڿ��� ����
    private Dictionary<string, HashSet<BaseResource>> _resourcesOnShip = new();

    // [���� ���� �ý���]
    private HashSet<Wood> dryingWoods = new HashSet<Wood>();
    private Queue<Wood> removalQueue = new Queue<Wood>();
    private bool isDryUpdate = false;
    public Action<string, int> OnResourceChanged;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    #region Ship Area Registry (������ ����)

    public void Register(BaseResource item)
    {
        if (item == null || !item.IsCollected) return;

        string key = item.key;
        if (!_resourcesOnShip.ContainsKey(key))
            _resourcesOnShip[key] = new HashSet<BaseResource>();

        // HashSet.Add�� ���� �� true�� ��ȯ�ϹǷ� �ߺ� ȣ�� ���� ����
        if (_resourcesOnShip[key].Add(item))
        {
            // �̺�Ʈ ȣ��: (�ڿ� Ű, ���� �ش� �ڿ��� �� ����)
            OnResourceChanged?.Invoke(key, _resourcesOnShip[key].Count);

            // ���� ������� �߰� ���� ����
            if (item is Wood wood) RegisterWood(wood);
        }
    }

    public void Unregister(BaseResource item)
    {
        if (item == null) return;

        string key = item.key;
        if (_resourcesOnShip.TryGetValue(key, out var set))
        {
            if (set.Remove(item))
            {
                // ���� ���� �ÿ��� �̺�Ʈ ȣ��
                OnResourceChanged?.Invoke(key, set.Count);
            }
        }
    }

    // Ư�� �ڿ��� �� ���� Ȯ�� (UI ��� ȣ��)
    public int GetResourceCount(string poolKey)
    {
        if (_resourcesOnShip.TryGetValue(poolKey, out var set))
        {
            return set.Count;
        }
        return 0;
    }
    #endregion

    #region Wood Drying System (���� �ý���)

    public bool RegisterWood(Wood wood)
    {
        if (wood == null)
        {
            return false;
        }

        bool canDry = wood.CurState == eWoodState.Wet || wood.CurState == eWoodState.Drying;
        if (!canDry)
        {
            return false;
        }

        if (dryingWoods.Add(wood))
        {
            wood.PutResource();
            isDryUpdate = true;
            return true;
        }

        return false;
    }

    private void Update()
    {
        if (!isDryUpdate || !isPlaying) return;

        float dt = Time.deltaTime;

        // ���� ����
        foreach (var wood in dryingWoods)
        {
            if (wood.OnDryWood(dt)) // ���� �Ϸ� �� true ��ȯ ����
            {
                removalQueue.Enqueue(wood);
            }
        }

        // �Ϸ�� ������ ���� ����
        if (removalQueue.Count > 0)
        {
            while (removalQueue.Count > 0)
            {
                var finishedWood = removalQueue.Dequeue();
                dryingWoods.Remove(finishedWood);
            }

            if (dryingWoods.Count == 0)
                isDryUpdate = false;
        }
    }

    #endregion
}