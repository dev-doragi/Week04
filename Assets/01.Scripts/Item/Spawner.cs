using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class SpawnData
{
    public string poolKey;
    public float spawnIntervalZ = 15f;   // 다음 소환까지의 기본 거리
    public float minSafeDistance = 10f;  // 오브젝트 간 안전 거리
    public float spawnOffsetZ = 50f;     // 카메라 앞 생성 시작 지점

    [Header("Multi Spawn Settings")]
    public int minSpawnCount = 1;        // 한 번에 최소 몇 개?
    public int maxSpawnCount = 3;        // 한 번에 최대 몇 개?
    public float randomZRange = 5f;      // 정해진 Z값에서 +- 랜덤 오차

    [HideInInspector] public float nextTargetZ;
}

public class Spawner : Singleton<Spawner>
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Map Range (X Axis)")]
    public float mapMinX = -140f;
    public float mapMaxX = 140f;

    [Header("Spawn Settings")]
    public List<SpawnData> spawnList;
    public float despawnOffsetZ = 20f;
    public float retryStepZ = 1.0f;
    public int maxRetryAttempts = 5;

    private List<ObjectPoolBase> _activeObjects = new List<ObjectPoolBase>();

    protected override void Init()
    {

    }

    public void Setup()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        var cts = this.GetCancellationTokenOnDestroy();

        foreach (var data in spawnList)
        {
            data.nextTargetZ = cameraTransform.position.z + data.spawnOffsetZ;
            RunIndependentSpawn(data, cts).Forget();
        }

        RunDespawnLoop(cts).Forget();
    }

    private async UniTaskVoid RunIndependentSpawn(SpawnData data, CancellationToken token)
    {
        float minSafeDistanceSqr = data.minSafeDistance * data.minSafeDistance;

        while (!token.IsCancellationRequested)
        {
            if (cameraTransform == null) return;

            if (cameraTransform.position.z + data.spawnOffsetZ >= data.nextTargetZ)
            {
                int spawnCount = UnityEngine.Random.Range(data.minSpawnCount, data.maxSpawnCount + 1);
                int successfullySpawned = 0;

                for (int s = 0; s < spawnCount; s++)
                {
                    bool currentSpawnSuccess = false; // 이번 개체 소환 성공 여부 초기화

                    for (int i = 0; i < maxRetryAttempts; i++)
                    {
                        float randomX = UnityEngine.Random.Range(mapMinX, mapMaxX);
                        float randomZ = data.nextTargetZ + UnityEngine.Random.Range(-data.randomZRange, data.randomZRange);
                        Vector3 potentialPos = new Vector3(randomX, 10, randomZ);

                        if (IsPositionSafeOptimized(potentialPos, minSafeDistanceSqr))
                        {
                            SpawnObject(data.poolKey, potentialPos);
                            currentSpawnSuccess = true; // 소환 성공 기록
                            successfullySpawned++;
                            break;
                        }
                    }

                    // 만약 특정 개체가 소환에 실패했다면(빈 공간 없음) 로그를 찍거나 
                    // 해당 스폰 시퀀스를 중단할 수도 있습니다.
                    if (!currentSpawnSuccess)
                    {
                        Debug.Log($"{data.poolKey}의 {s}번째 개체가 소환 공간을 찾지 못함");
                    }
                }

                // 하나라도 소환했다면 간격만큼 전진, 아예 실패했다면 조금만 전진(재시도)
                if (successfullySpawned > 0)
                {
                    data.nextTargetZ += data.spawnIntervalZ;
                }
                else
                {
                    // 화면에 공간이 너무 꽉 차서 하나도 못 뽑은 경우
                    data.nextTargetZ += retryStepZ;
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private bool IsPositionSafeOptimized(Vector3 pos, float minDistanceSqr)
    {
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            if (_activeObjects[i] == null) continue;

            // Vector3.Distance 대신 sqrMagnitude 연산 사용 (제곱근 연산 제거)
            float distSqr = (pos - _activeObjects[i].transform.position).sqrMagnitude;
            if (distSqr < minDistanceSqr)
                return false;
        }
        return true;
    }

    private void SpawnObject(string key, Vector3 position)
    {
        // TODO: PoolManager.Get(key) 호출
        ObjectPoolBase obj = null;
        if (key =="Wood")
        {
            obj = ObjectPoolManager.Instance.OnSpawnResources<Wood>();
        }
        else if(key == "Fabric")
        {
            obj = ObjectPoolManager.Instance.OnSpawnResources<Fabric>();
        }
        else if(key == "Rock")
        {
            obj = ObjectPoolManager.Instance.OnSpawnResources<RockBreakBlock>();
        }
        
        if (obj != null)
        {
            obj.transform.position = position;
            _activeObjects.Add(obj);
        }
    }

    private async UniTaskVoid RunDespawnLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (cameraTransform == null) return;

            float threshold = cameraTransform.position.z - despawnOffsetZ;

            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                var obj = _activeObjects[i];

                if (obj == null || obj.transform.position.z < threshold)
                {
                    // null이 아닐 때만 풀로 반환하도록 안전하게 분기
                    if (obj != null)
                    {
                        ObjectPoolManager.Instance.OnRelease(obj.key, obj);
                    }

                    _activeObjects.RemoveAt(i);
                }
            }
            await UniTask.Delay(200, cancellationToken: token);
        }
    }
}
