using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateUnitDirAccJob : IJobParallelFor
{
    [ReadOnly] private NativeArray<float2> dirMap;
    [ReadOnly] private NativeArray<int> dgIndices;
    private NativeArray<float2> dirAccs;
    [ReadOnly] private NativeArray<float> curMaxSpeeds;
    private readonly int2 dgSize;

    public UpdateUnitDirAccJob(
        NativeArray<float2> dirMap,
        NativeArray<int> dgIndices,
        NativeArray<float2> dirAccs,
        NativeArray<float> curMaxSpeeds,
        int2 dgSize
    )
    {
        this.dirMap = dirMap;
        this.dgIndices = dgIndices;
        this.dirAccs = dirAccs;
        this.curMaxSpeeds = curMaxSpeeds;
        this.dgSize = dgSize;
    }

    public void Execute(int index)
    {
        int2 dgPos = new(dgIndices[index] / dgSize.y, dgIndices[index] % dgSize.y);
        float2 baseDir = dirMap[dgIndices[index]];
        bool baseInf = math.isinf(baseDir.x) && math.isinf(baseDir.y);

        if (baseInf)
        {
            int step = 1;
            while (step < math.max(dgSize.x, dgSize.y))
            {
                bool canBreak = false;
                for (int dx = -step; dx <= step; dx++)
                {
                    for (int dy = -step; dy <= step; dy++)
                    {
                        if (dx != -step && dx != step && dy != -step && dy != step) continue;

                        int2 newPos = new(dgPos.x + dx, dgPos.y + dy);
                        if (newPos.x < 0 || newPos.x >= dgSize.x || newPos.y < 0 || newPos.y >= dgSize.y) continue;
                        int newIndex = newPos.x * dgSize.y + newPos.y;

                        float2 newDir = dirMap[newIndex];
                        if (math.isfinite(newDir.x) && math.isfinite(newDir.y))
                        {
                            baseDir = newDir;
                            canBreak = true;
                        }
                    }
                    if (canBreak) break;
                }
                if (canBreak) break;
                step++;
            }
        }

        dirAccs[index] += 4 * curMaxSpeeds[index] * baseDir;
    }
}
