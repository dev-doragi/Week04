using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatStructureBalanceAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform blocksRoot;
    [SerializeField] private BoatPhysics boatPhysics;

    [Header("Mass")]
    [SerializeField] private float baseHullMass = 300f;
    [SerializeField] private float minHullMass = 50f;

    [Header("Wobble")]
    [SerializeField] private float addImpulseTorque = 10f;
    [SerializeField] private float removeImpulseTorque = 30f;
    [SerializeField] private float comBlend = 0.35f;

    [SerializeField] private float comYOffset = -0.35f;
    [SerializeField] private float maxComShiftXZ = 0.6f;

    private Vector3 baseCom;

    private Rigidbody rb;
    private readonly List<BoatBlock> blocks = new List<BoatBlock>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (blocksRoot == null)
        {
            blocksRoot = transform;
        }

        RefreshBlocks();
        RecomputeAndWobble(false, transform.position, 0f);
    }

    public void RefreshBlocks()
    {
        blocks.Clear();

        BoatBlock[] arr = blocksRoot.GetComponentsInChildren<BoatBlock>(true);
        for (int i = 0; i < arr.Length; i++)
        {
            if (blocks.Contains(arr[i]) == false)
            {
                blocks.Add(arr[i]);
            }
        }
    }

    public void NotifyBlockRemoved(BoatBlock block, bool disableObject)
    {
        if (block == null)
        {
            return;
        }

        block.InHull = false;

        Vector3 eventPoint = block.transform.position;

        if (disableObject)
        {
            block.gameObject.SetActive(false);
        }
        else
        {
            Destroy(block.gameObject);
        }

        RecomputeAndWobble(false, eventPoint, removeImpulseTorque);
    }

    public void NotifyBlockAdded(BoatBlock block)
    {
        if (block == null)
        {
            return;
        }

        if (blocks.Contains(block) == false)
        {
            blocks.Add(block);
        }

        block.InHull = true;
        block.gameObject.SetActive(true);

        RecomputeAndWobble(true, block.transform.position, addImpulseTorque);
    }

    private void RecomputeAndWobble(bool added, Vector3 worldPoint, float torqueImpulse)
{
    float activeMass = 0f;
    Vector3 weightedLocal = Vector3.zero;
    int activeCount = 0;

    for (int i = 0; i < blocks.Count; i++)
    {
        BoatBlock b = blocks[i];
        if (b == null)
        {
            continue;
        }

        if (b.InHull == false || b.gameObject.activeInHierarchy == false)
        {
            continue;
        }

        float m = Mathf.Max(0.01f, b.Mass);
        Vector3 localPos = transform.InverseTransformPoint(b.transform.position);

        activeMass += m;
        weightedLocal += localPos * m;
        activeCount += 1;
    }

    float targetMass = Mathf.Max(minHullMass, baseHullMass + activeMass);
    rb.mass = targetMass;

    Vector3 targetCom = baseCom;

    if (activeMass > 0.001f)
    {
        Vector3 avg = weightedLocal / activeMass;
        targetCom.x = Mathf.Clamp(avg.x, -maxComShiftXZ, maxComShiftXZ);
        targetCom.z = Mathf.Clamp(avg.z, -maxComShiftXZ, maxComShiftXZ);
    }

    targetCom.y = baseCom.y + comYOffset;
    rb.centerOfMass = Vector3.Lerp(rb.centerOfMass, targetCom, Mathf.Clamp01(comBlend));

    if (boatPhysics != null)
    {
        boatPhysics.centerOfMass = rb.centerOfMass;
    }

    Vector3 lever = worldPoint - rb.worldCenterOfMass;
    Vector3 torqueAxis = Vector3.Cross(Vector3.up, lever);

    if (torqueAxis.sqrMagnitude > 0.0001f && torqueImpulse > 0f)
    {
        Vector3 torque = torqueAxis.normalized * torqueImpulse;
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    Debug.Log("BoatBalance activeCount=" + activeCount + " mass=" + rb.mass + " com=" + rb.centerOfMass);
}
}
