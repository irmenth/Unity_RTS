using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GetSelectedListJob : IJob
{
    [ReadOnly] private NativeArray<bool> selectedMap;
    private NativeList<int> selectedList;
    private readonly int size;

    public GetSelectedListJob(
        NativeArray<bool> selectedMap,
        NativeList<int> selectedList,
        int size
    )
    {
        this.selectedMap = selectedMap;
        this.selectedList = selectedList;
        this.size = size;
    }

    public void Execute()
    {
        for (int i = 0; i < size; i++)
        {
            if (selectedMap[i])
            {
                selectedList.AddNoResize(i);
            }
        }
    }
}
