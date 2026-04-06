using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[System.Serializable]
public class SpawnData
{
    public string poolKey;
    public float spawnIntervalZ = 15f;
    public float minSafeDistance = 10f;
    public float spawnOffsetZ = 50f;

    [Header("Multi Spawn Settings")]
    public int minSpawnCount = 1;
    public int maxSpawnCount = 3;
    public float randomZRange = 5f;

    [HideInInspector] public float nextTargetZ;
}

public class Spawner : Singleton<Spawner>
{
    [Header("References")]
    public Transform cameraTransform;
    [SerializeField] private GameObject island; 
    [SerializeField] private float islandSafeRadius = 200f; // 섬 주변 안전 거리
    [Header("Map Range (X Axis)")]
    public float mapMinX = -140f;
    public float mapMaxX = 140f;

    [Header("Spawn Settings")]
    public List<SpawnData> spawnList;
    [SerializeField] private float despawnOffsetZ = 20f;
    [SerializeField] private float retryStepZ = 1.0f;
    [SerializeField] private int maxRetryAttempts = 5;
    [SerializeField] private float minSpawnOffsetZ = 50f;
    private List<ObjectPoolBase> _activeObjects = new List<ObjectPoolBase>();

    protected override void Init() { }

    public void Setup()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        var cts = this.GetCancellationTokenOnDestroy();

        foreach (var data in spawnList)
        {
            float startDelayDistance = minSpawnOffsetZ;
            data.nextTargetZ = cameraTransform.position.z + data.spawnOffsetZ + startDelayDistance;

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

            float currentZ = cameraTransform.position.z;

            if (currentZ + data.spawnOffsetZ >= data.nextTargetZ)
            {
                int spawnCount = UnityEngine.Random.Range(data.minSpawnCount, data.maxSpawnCount + 1);
                int successfullySpawned = 0;

                Vector3 baseForwardPos = cameraTransform.position + cameraTransform.forward * data.spawnOffsetZ;

                for (int s = 0; s < spawnCount; s++)
                {
                    for (int i = 0; i < maxRetryAttempts; i++)
                    {
                        float randomX = UnityEngine.Random.Range(mapMinX, mapMaxX);
                        float randomOffsetZ = UnityEngine.Random.Range(-data.randomZRange, data.randomZRange);

                        Vector3 potentialPos = new Vector3(
                            randomX,
                            10f,
                            baseForwardPos.z + randomOffsetZ
                        );

                        if (IsPositionSafeOptimized(potentialPos, minSafeDistanceSqr))
                        {
                            SpawnObject(data.poolKey, potentialPos);
                            successfullySpawned++;
                            break;
                        }
                    }
                }

                if (successfullySpawned > 0)
                {
                    data.nextTargetZ += data.spawnIntervalZ;
                }
                else
                {
                    data.nextTargetZ += retryStepZ;
                }

                if (data.nextTargetZ < currentZ)
                {
                    data.nextTargetZ = currentZ;
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private bool IsPositionSafeOptimized(Vector3 pos, float minDistanceSqr, string key = "")
    {
        // 기존 액티브 오브젝트 간격 체크
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            if (_activeObjects[i] == null) continue;

            float distSqr = (pos - _activeObjects[i].transform.position).sqrMagnitude;
            if (distSqr < minDistanceSqr)
                return false;
        }

        // RockBreakBlock이면 island 근처 차단
        if (key == "RockBreakBlock" && island != null)
        {
            float islandDistSqr = (pos - island.transform.position).sqrMagnitude;
            if (islandDistSqr < islandSafeRadius * islandSafeRadius)
                return false;
        }

        return true;
    }

    private void SpawnObject(string key, Vector3 position)
    {
        ObjectPoolBase obj = null;

        if (key == "WetWood")
        {
            obj = ObjectPoolManager.Instance.OnSpawnResources<Wood>("WetWood");
        }
        else if (key == "Fabric")
        {
            obj = ObjectPoolManager.Instance.OnSpawnResources<Fabric>();
        }
        else if (key == "RockBreakBlock")
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
                    if (obj != null && !obj.IsEquipped)
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