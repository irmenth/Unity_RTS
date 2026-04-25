using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct AssignDirMapIndexJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<bool> enableMap;
    private NativeArray<ulong> dirMapIndices;
    private NativeArray<bool> arrived;
    private readonly ulong dirMapID;

    public AssignDirMapIndexJob(
        NativeArray<bool> enableMap,
        NativeArray<ulong> dirMapIndices,
        NativeArray<bool> arrived,
        ulong dirMapID
    )
    {
        this.enableMap = enableMap;
        this.dirMapIndices = dirMapIndices;
        this.arrived = arrived;
        this.dirMapID = dirMapID;
    }

    public void Execute(int index)
    {
        if (enableMap[index])
        {
            dirMapIndices[index] = dirMapID;
            arrived[index] = false;
        }
    }
}
