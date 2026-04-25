using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class UnitRegister : MonoBehaviour
{
    public static UnitRegister instance;

    [SerializeField] private int capacity = (int)1.5e4;

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
    public NativeArray<bool> arrived;
    public NativeArray<int> dgIndices;
    public NativeArray<int> ogIndices;
    public TransformAccessArray unitTrans;
    public NativeArray<bool> selectedMap;
    public NativeList<int> selectedList;
    public NativeArray<ulong> dirMapIndices;

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
        arrived[index] = arrived[indexer];
        dgIndices[index] = dgIndices[indexer];
        ogIndices[index] = ogIndices[indexer];
        unitTrans.RemoveAtSwapBack(index);
        selectedMap[index] = selectedMap[indexer];
        dirMapIndices[index] = dirMapIndices[indexer];
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
        arrived[indexer] = false;
        dgIndices[indexer] = -1;
        ogIndices[indexer] = -1;
        unitTrans.Add(trans);
        selectedMap[indexer] = false;
        dirMapIndices[indexer] = ulong.MaxValue;
        return indexer;
    }

    public void Unregister(int index)
    {
        if (index > indexer)
        {
            Debug.LogError("[UnitRegister] remove: invalid ID");
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
        arrived = new(capacity, Allocator.Persistent);
        dgIndices = new(capacity, Allocator.Persistent);
        ogIndices = new(capacity, Allocator.Persistent);
        unitTrans = new(capacity);
        selectedMap = new(capacity, Allocator.Persistent);
        selectedList = new(capacity, Allocator.Persistent);
        dirMapIndices = new(capacity, Allocator.Persistent);
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
        arrived.Dispose();
        dgIndices.Dispose();
        ogIndices.Dispose();
        unitTrans.Dispose();
        selectedMap.Dispose();
        selectedList.Dispose();
        dirMapIndices.Dispose();

        instance = null;
    }
}
