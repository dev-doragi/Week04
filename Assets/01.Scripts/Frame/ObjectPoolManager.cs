using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager instance;
    public static ObjectPoolManager Instance { get { return instance; } private set { instance = value; } }


    private Dictionary<string, Queue<ObjectPoolBase>> poolDic = new();
    private Dictionary<string, HashSet<ObjectPoolBase>> poolActiveHash = new();
    [SerializeField] private List<ObjectPoolBase> poolPrefab = new();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);

        SyncPoolPrefabKeys();
    }

    private void OnValidate() // 에디터에서도 자동 동기화
    {
        SyncPoolPrefabKeys();
    }

    private void Start()
    {
        Initialized();
    }

    public void Initialized()
    {
        foreach (var prefab in poolPrefab)
        {
            if (prefab == null)
                continue;

            if (!poolDic.ContainsKey(prefab.key))
                poolDic.Add(prefab.key, new Queue<ObjectPoolBase>());

            if (!poolActiveHash.ContainsKey(prefab.key))
                poolActiveHash.Add(prefab.key, new HashSet<ObjectPoolBase>());

            OnCreatedPools(prefab.key);
        }
    }

    private void SyncPoolPrefabKeys() // 프리팹 동기화
    {
        if (poolPrefab == null)
            return;

        for (int i = 0; i < poolPrefab.Count; i++)
        {
            if (poolPrefab[i] == null)
                continue;

            poolPrefab[i].key = poolPrefab[i].name;
        }
    }

    public void OnCreatedPools(string key)
    {
        var prefab = GetPrefabInfo(key);
        if (prefab == null)
        {
            Debug.LogError($"풀 프리팹 정보 없음: {key}");
            return;
        }

        int createCount = Mathf.Max(1, prefab.prevCount);

        for (int i = 0; i < createCount; i++)
        {
            var item = OnCreatedPool(key, prefab);

            if (item != null)
            {
                poolDic[key].Enqueue(item);
            }
        }
    }

    public ObjectPoolBase OnCreatedPool(string key, ObjectPoolBase prefab = null, bool isOn = false)
    {
        if (prefab == null)
            prefab = GetPrefabInfo(key);
        if(prefab == null)
        {
            Debug.LogError("데이터 없음");
            return null;
        }    

        var pool = Instantiate<ObjectPoolBase>(prefab, Vector3.zero, Quaternion.identity);

        if(pool == null)
        {
            Debug.LogError("데이터 없음");
            return null;
        }

        pool.gameObject.transform.SetParent(gameObject.transform, false);

        pool.gameObject.SetActive(true);
        pool.Init();
        if (!isOn)
            pool.gameObject.SetActive(false);

        return pool;
    }

    public T OnSpawnResources<T>() where T : ObjectPoolBase
    {
        string key = typeof(T).Name;
        T resource = OnSpawnPool(key) as T;
        if (resource == null)
        {
            Debug.LogError($"{key} 타입을 풀에서 생성할 수 없습니다.");
        }
        return resource;
    }

    public void OnSpawnHazard()
    {

    }

    public ObjectPoolBase OnSpawnPool(string key)
    {
        if (!poolDic.TryGetValue(key, out var itemQueue))
        {
            Debug.LogError("데이터 없음 추가요망");
            return null;
        }

        ObjectPoolBase item;
        if(itemQueue.Count <= 0)
        {
            item = OnCreatedPool(key, null, true);
        }
        else
        {
            item = itemQueue.Dequeue();
        }

        if (!poolActiveHash.TryGetValue(key, out var itemActiveHash))
        {
            var newHash = new HashSet<ObjectPoolBase>();
            poolActiveHash.Add(key, newHash);
            itemActiveHash = newHash;
        }

        itemActiveHash.Add(item);
        item.gameObject.SetActive(true);
        item.Setup();
        return item;
    }

    // 위치 값을 추가한 버전
    public ObjectPoolBase OnSpawnPool(string key, Vector3 position)
    {
        if (!poolDic.TryGetValue(key, out var itemQueue))
        {
            Debug.LogError($"데이터 없음 추가요망: {key}");
            return null;
        }

        ObjectPoolBase item;
        if (itemQueue.Count <= 0)
        {
            item = OnCreatedPool(key, null, true);
        }
        else
        {
            item = itemQueue.Dequeue();
        }

        if (!poolActiveHash.TryGetValue(key, out var itemActiveHash))
        {
            var newHash = new HashSet<ObjectPoolBase>();
            poolActiveHash.Add(key, newHash);
            itemActiveHash = newHash;
        }

        item.transform.position = position;
        itemActiveHash.Add(item);
        item.gameObject.SetActive(true);
        item.Setup();

        return item;
    }

    public void OnRelease(string key, ObjectPoolBase item)
    {
        if (item == null) return;

        item.gameObject.SetActive(false);

        if (poolDic.TryGetValue(key, out var itemQueue))
        {
            if (!itemQueue.Contains(item))
                itemQueue.Enqueue(item);
        }

        if (poolActiveHash.TryGetValue(key, out var itemActiveHash))
        {
            itemActiveHash.Remove(item);
        }
    }

    public ObjectPoolBase GetPrefabInfo(string key)
    {
        return poolPrefab.FirstOrDefault(x => x.key == key);
    }

    public void AllRelease()
    {
        foreach(var hashData in poolActiveHash.Values)
        {
            if(hashData.Count <= 0) continue;
            foreach (var item in hashData)
                OnRelease(item.key, item);
        }
    }
}
