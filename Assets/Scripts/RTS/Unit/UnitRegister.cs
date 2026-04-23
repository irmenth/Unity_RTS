using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitRegister : MonoBehaviour
{
    public static UnitRegister instance;

    [SerializeField] private int capacity = (int)1e4;

    public NativeArray<float> radii;
    public NativeArray<float> speeds;
    public NativeArray<float> curMaxSpeeds;
    public NativeArray<float2> positions;
    public NativeArray<float2> lastPositions;
    public NativeArray<quaternion> rotations;
    public NativeArray<float2> dirAccs;
    public NativeArray<float2> boidsAccs;
    public NativeArray<float> dirAccRatios;
    public NativeArray<float2> velocities;
    public NativeArray<int> dgIndices;
    public NativeArray<int> ogIndices;
    public TransformAccessArray unitTrans;

    [HideInInspector] public int indexer = -1;

    private void Remove(int index)
    {
        radii[index] = radii[indexer];
        speeds[index] = speeds[indexer];
        curMaxSpeeds[index] = curMaxSpeeds[indexer];
        positions[index] = positions[indexer];
        lastPositions[index] = lastPositions[indexer];
        rotations[index] = rotations[indexer];
        dirAccs[index] = dirAccs[indexer];
        boidsAccs[index] = boidsAccs[indexer];
        dirAccRatios[index] = dirAccRatios[indexer];
        velocities[index] = velocities[indexer];
        dgIndices[index] = dgIndices[indexer];
        ogIndices[index] = ogIndices[indexer];
        unitTrans.RemoveAtSwapBack(index);
    }

    public int Register(Transform trans, float radius, float speed, float2 position)
    {
        indexer++;
        radii[indexer] = radius;
        speeds[indexer] = speed;
        curMaxSpeeds[indexer] = speed;
        positions[indexer] = position;
        lastPositions[indexer] = position;
        rotations[indexer] = quaternion.identity;
        dirAccs[indexer] = float2.zero;
        boidsAccs[indexer] = float2.zero;
        dirAccRatios[indexer] = 0.5f;
        velocities[indexer] = float2.zero;
        dgIndices[indexer] = -1;
        ogIndices[indexer] = -1;
        unitTrans.Add(trans);
        return indexer;
    }

    public void Unregister(int index)
    {
        if (index > indexer)
        {
            Debug.LogError("Invalid ID");
            return;
        }
        Remove(index);
        EventBus.Publish(new UnitRemoveEvent(indexer, index));
        indexer--;
    }

    private void Awake()
    {
        instance = this;

        radii = new(capacity, Allocator.Persistent);
        speeds = new(capacity, Allocator.Persistent);
        curMaxSpeeds = new(capacity, Allocator.Persistent);
        positions = new(capacity, Allocator.Persistent);
        lastPositions = new(capacity, Allocator.Persistent);
        rotations = new(capacity, Allocator.Persistent);
        dirAccs = new(capacity, Allocator.Persistent);
        boidsAccs = new(capacity, Allocator.Persistent);
        dirAccRatios = new(capacity, Allocator.Persistent);
        velocities = new(capacity, Allocator.Persistent);
        dgIndices = new(capacity, Allocator.Persistent);
        ogIndices = new(capacity, Allocator.Persistent);
        unitTrans = new(capacity);
    }

    private void OnDestroy()
    {
        radii.Dispose();
        speeds.Dispose();
        curMaxSpeeds.Dispose();
        positions.Dispose();
        lastPositions.Dispose();
        rotations.Dispose();
        dirAccs.Dispose();
        boidsAccs.Dispose();
        dirAccRatios.Dispose();
        velocities.Dispose();
        dgIndices.Dispose();
        ogIndices.Dispose();
        unitTrans.Dispose();

        instance = null;
    }
}
