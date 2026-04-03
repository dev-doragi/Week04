using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class ObjectPoolManager : MonoBehaviour
{
    [Header("생성할 프리팹")]
    public GameObject wood;
    public GameObject iron;

    [Header("위치 기준 오브젝트")]
    public GameObject cam;
    public GameObject boat;

    [Header("Random Position X")]
    public Transform spawnPointA;
    public Transform spawnPointB;

    [Header("한 번에 생성할 개수")]
    public int spawnCount = 3;

    [Space(10)]
    public float spawnInterval;
    private Vector3 offset = new Vector3(0, 0, 50);

    private IObjectPool<GameObject> poolA;
    private IObjectPool<GameObject> poolB;

    void Start()
    {
        poolA = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(wood, transform, false),
            actionOnGet: OnGetObj, actionOnRelease: OnReleaseObj, actionOnDestroy: OnDestroyObj
        );

        poolB = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(iron, transform, false),
            actionOnGet: OnGetObj, actionOnRelease: OnReleaseObj, actionOnDestroy: OnDestroyObj
        );

        StartCoroutine(SpawnRoutine());
    }

    private void OnGetObj(GameObject obj) => obj.SetActive(true);
    private void OnReleaseObj(GameObject obj) => obj.SetActive(false);
    private void OnDestroyObj(GameObject obj) => Destroy(obj);

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            Vector3 spawnPosition = boat.transform.position + offset;

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject obsA = poolA.Get();
                obsA.transform.position = RandomPosition(spawnPosition);
                //obsA.transform.SetParent(transform, false);

                StartCoroutine(CheckPositionRoutine(obsA, poolA));
            }

            for (int i = 0; i < spawnCount; i++)
            {
                GameObject obsB = poolB.Get();
                obsB.transform.position = RandomPosition(spawnPosition);
                //obsB.transform.SetParent(transform, false);

                StartCoroutine(CheckPositionRoutine(obsB, poolB));
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }
    private IEnumerator CheckPositionRoutine(GameObject obj, IObjectPool<GameObject> pool)
    {
        while (obj.activeSelf)
        {
            if (obj.transform.position.z < cam.transform.position.z)
            {
                pool.Release(obj); 
                yield break;                   }
            
            yield return null;
        }
    }

    Vector3 RandomPosition(Vector3 objectPos)
    {
        float randomX = Random.Range(spawnPointA.position.x, spawnPointB.position.x);

        return new Vector3(randomX, objectPos.y, objectPos.z);
    }
}
