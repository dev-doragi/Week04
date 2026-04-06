using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepoManager : MonoBehaviour
{
    private static RepoManager instance;
    public static RepoManager Instance { get { return instance; } }

    [Header("Status")]
    public bool isPlaying = true;

    // [변경] ResourceItem의 poolKey(string)를 기반으로 배 위의 실시간 자원을 관리
    private Dictionary<string, HashSet<BaseResource>> _resourcesOnShip = new();

    // [기존 건조 시스템]
    private HashSet<Wood> dryingWoods = new HashSet<Wood>();
    private Queue<Wood> removalQueue = new Queue<Wood>();
    private bool isDryUpdate = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    #region Ship Area Registry (물리적 감지)

    // 자원이 배 영역(Trigger)에 들어왔을 때 호출
    public void Register(BaseResource item)
    {
        if (item == null) return;

        string key = item.key;
        if (!_resourcesOnShip.ContainsKey(key))
            _resourcesOnShip[key] = new HashSet<BaseResource>();

        if (_resourcesOnShip[key].Add(item))
        {
            // 만약 나무라면 자동으로 건조 시퀀스 등록 시도
            if (item is Wood wood)
            {
                RegisterWood(wood);
            }
        }
    }

    // 자원이 배 영역에서 나갔을 때 호출
    public void Unregister(BaseResource item)
    {
        if (item == null) return;

        if (_resourcesOnShip.TryGetValue(item.key, out var set))
        {
            set.Remove(item);
        }
    }

    // 특정 자원의 총 개수 확인 (UI 등에서 호출)
    public int GetResourceCount(string poolKey)
    {
        if (_resourcesOnShip.TryGetValue(poolKey, out var set))
        {
            return set.Count;
        }
        return 0;
    }

    #endregion

    #region Wood Drying System (건조 시스템)

    public bool RegisterWood(Wood wood)
    {
        // 젖은 상태가 아니면 등록 안 함
        if (!wood.OnCheckedWet()) return false;

        if (dryingWoods.Add(wood))
        {
            wood.PutResource(); // 배 위에 놓인 상태로 전환 (애니메이션 등)
            isDryUpdate = true;
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (!isDryUpdate || !isPlaying) return;

        float dt = Time.deltaTime;

        // 건조 진행
        foreach (var wood in dryingWoods)
        {
            if (wood.OnDryWood(dt)) // 건조 완료 시 true 반환 가정
            {
                removalQueue.Enqueue(wood);
            }
        }

        // 완료된 나무들 제거 로직
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