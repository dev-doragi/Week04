using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatStructure : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform blocksRoot;
    [SerializeField] SimpleBuoyantObject buoyancy;

    [Header("Hull")]
    [SerializeField] float baseMass = 5f;
    [SerializeField] float blockHalfHeight = 0.5f;
    [SerializeField] float detachImpulse = 2f;

    readonly Dictionary<Vector3Int, BoatBlock> blockMap = new Dictionary<Vector3Int, BoatBlock>();
    Rigidbody rb;

    static readonly Vector3Int[] N6 =
    {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (blocksRoot == null) blocksRoot = transform;
        if (buoyancy == null) TryGetComponent<SimpleBuoyantObject>(out buoyancy);

        RebuildMap();
        RecomputeHullPhysics(new List<BoatBlock>(blockMap.Values), rb, buoyancy, baseMass);
    }

    public void BreakBlock(BoatBlock broken)
    {
        if (broken == null) return;
        if (!blockMap.Remove(broken.cell)) return;

        DetachSingleDebris(broken);

        if (blockMap.Count == 0)
        {
            RecomputeHullPhysics(new List<BoatBlock>(), rb, buoyancy, baseMass);
            return;
        }

        List<List<BoatBlock>> components = FindConnectedComponents(blockMap.Values);
        List<BoatBlock> mainComponent = PickMainComponent(components);

        foreach (List<BoatBlock> component in components)
        {
            if (ReferenceEquals(component, mainComponent)) continue;

            SpawnChunk(component);

            foreach (BoatBlock block in component)
            {
                blockMap.Remove(block.cell);
            }
        }

        RecomputeHullPhysics(mainComponent, rb, buoyancy, baseMass);
    }

    void RebuildMap()
    {
        blockMap.Clear();

        BoatBlock[] blocks = blocksRoot.GetComponentsInChildren<BoatBlock>();
        foreach (BoatBlock block in blocks)
        {
            if (!blockMap.ContainsKey(block.cell))
            {
                blockMap.Add(block.cell, block);
            }
        }
    }

    static List<List<BoatBlock>> FindConnectedComponents(IEnumerable<BoatBlock> blocks)
    {
        Dictionary<Vector3Int, BoatBlock> map = new Dictionary<Vector3Int, BoatBlock>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        List<List<BoatBlock>> result = new List<List<BoatBlock>>();

        foreach (BoatBlock block in blocks)
        {
            if (!map.ContainsKey(block.cell))
            {
                map.Add(block.cell, block);
            }
        }

        foreach (KeyValuePair<Vector3Int, BoatBlock> pair in map)
        {
            Vector3Int startCell = pair.Key;
            if (!visited.Add(startCell)) continue;

            List<BoatBlock> component = new List<BoatBlock>();
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(startCell);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                component.Add(map[current]);

                for (int i = 0; i < N6.Length; i++)
                {
                    Vector3Int next = current + N6[i];
                    if (!map.ContainsKey(next)) continue;
                    if (!visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }

            result.Add(component);
        }

        return result;
    }

    static List<BoatBlock> PickMainComponent(List<List<BoatBlock>> components)
    {
        List<BoatBlock> bestCoreComponent = null;
        int bestCoreCount = -1;

        for (int i = 0; i < components.Count; i++)
        {
            List<BoatBlock> component = components[i];
            bool hasCore = false;

            for (int j = 0; j < component.Count; j++)
            {
                if (component[j].isCore)
                {
                    hasCore = true;
                    break;
                }
            }

            if (hasCore && component.Count > bestCoreCount)
            {
                bestCoreComponent = component;
                bestCoreCount = component.Count;
            }
        }

        if (bestCoreComponent != null) return bestCoreComponent;

        List<BoatBlock> largest = null;
        int largestCount = -1;

        for (int i = 0; i < components.Count; i++)
        {
            List<BoatBlock> component = components[i];
            if (component.Count > largestCount)
            {
                largest = component;
                largestCount = component.Count;
            }
        }

        return largest;
    }

    void DetachSingleDebris(BoatBlock block)
    {
        block.transform.SetParent(null, true);

        Rigidbody debrisRb = block.gameObject.GetComponent<Rigidbody>();
        if (debrisRb == null) debrisRb = block.gameObject.AddComponent<Rigidbody>();

        debrisRb.mass = Mathf.Max(0.1f, block.mass);
        debrisRb.AddForce((transform.forward + transform.up) * detachImpulse, ForceMode.Impulse);
    }

    void SpawnChunk(List<BoatBlock> component)
    {
        GameObject chunk = new GameObject("BoatChunk");
        chunk.transform.SetPositionAndRotation(transform.position, transform.rotation);

        foreach (BoatBlock block in component)
        {
            block.transform.SetParent(chunk.transform, true);
        }

        Rigidbody chunkRb = chunk.AddComponent<Rigidbody>();
        SimpleBuoyantObject chunkBuoyancy = chunk.AddComponent<SimpleBuoyantObject>();

        chunkBuoyancy.waterReference = buoyancy != null ? buoyancy.waterReference : null;
        chunkBuoyancy.waterLevelOffset = buoyancy != null ? buoyancy.waterLevelOffset : 0f;

        RecomputeHullPhysics(component, chunkRb, chunkBuoyancy, 0f);
    }

    void RecomputeHullPhysics(List<BoatBlock> blocks, Rigidbody targetRb, SimpleBuoyantObject targetBuoy, float rootBaseMass)
    {
        float blocksMass = 0f;
        Vector3 weightedPositionSum = Vector3.zero;
        Vector3[] points = new Vector3[blocks.Count];

        for (int i = 0; i < blocks.Count; i++)
        {
            BoatBlock block = blocks[i];
            Vector3 localPos = targetRb.transform.InverseTransformPoint(block.transform.position);

            blocksMass += block.mass;
            weightedPositionSum += localPos * block.mass;
            points[i] = localPos + Vector3.down * blockHalfHeight;
        }

        targetRb.mass = Mathf.Max(0.1f, rootBaseMass + blocksMass);
        targetRb.centerOfMass = blocksMass > 0.001f ? (weightedPositionSum / blocksMass) : Vector3.zero;

        if (targetBuoy != null)
        {
            targetBuoy.localFloatPoints = points;
        }
    }
}
