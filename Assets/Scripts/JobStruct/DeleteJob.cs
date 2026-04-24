using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct DeleteJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<bool> enableMap;
    private NativeArray<int> readyDelete;
    private NativeArray<int> length;

    public DeleteJob(
        NativeArray<bool> enableMap,
        NativeArray<int> readyDelete,
        NativeArray<int> length
    )
    {
        this.enableMap = enableMap;
        this.readyDelete = readyDelete;
        this.length = length;
    }

    public void Execute(int index)
    {
        if (enableMap[index])
        {
            readyDelete[length[0]] = index;
            length[0]++;
        }
    }
}
