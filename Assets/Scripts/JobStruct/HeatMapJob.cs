using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct HeatMapJob : IJob
{
    private readonly int2 size;
    private NativeArray<int> destination;
    private NativeQueue<int> openList;
    private NativeArray<byte> inOpenList;
    private NativeArray<byte> closeList;
    [ReadOnly] private NativeArray<float> costMap;
    private NativeArray<float> heatMap;

    public HeatMapJob(
        int2 size,
        NativeArray<int> destination,
        NativeQueue<int> openList,
        NativeArray<byte> inOpenList,
        NativeArray<byte> closeList,
        NativeArray<float> costMap,
        NativeArray<float> heatMap
        )
    {
        this.size = size;
        this.destination = destination;
        this.openList = openList;
        this.inOpenList = inOpenList;
        this.closeList = closeList;
        this.costMap = costMap;
        this.heatMap = heatMap;
    }

    public void Execute()
    {
        float sqr2 = math.sqrt(2f);

        for (int i = 0; i < size.x * size.y; i++)
        {
            heatMap[i] = float.PositiveInfinity;
        }

        if (math.isinf(costMap[destination[0]]))
        {
            int step = 1;
            while (step < math.max(size.x, size.y))
            {
                bool canBreak = false;
                for (int dx = -step; dx <= step; dx++)
                {
                    for (int dy = -step; dy <= step; dy++)
                    {
                        if (dx != -step && dx != step && dy != -step && dy != step) continue;

                        int2 newPos = new(destination[0] / size.y + dx, destination[0] % size.y + dy);
                        if (newPos.x < 0 || newPos.x >= size.x || newPos.y < 0 || newPos.y >= size.y) continue;
                        int newIndex = newPos.x * size.y + newPos.y;

                        if (math.isfinite(costMap[newIndex]))
                        {
                            destination[0] = newIndex;
                            canBreak = true;
                            break;
                        }
                    }
                    if (canBreak) break;
                }
                if (canBreak) break;

                step++;
            }
        }

        heatMap[destination[0]] = 0f;
        openList.Enqueue(destination[0]);
        inOpenList[destination[0]] = 1;

        while (openList.Count > 0)
        {
            int curIndex = openList.Dequeue();
            inOpenList[curIndex] = 0;
            closeList[curIndex] = 1;

            int2 curGridPos = new(curIndex / size.y, curIndex % size.y);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = curGridPos.x + dx, ny = curGridPos.y + dy;
                    if (nx < 0 || nx >= size.x || ny < 0 || ny >= size.y) continue;

                    int newIndex = nx * size.y + ny;
                    if (closeList[newIndex] == 1) continue;
                    if (math.isinf(costMap[newIndex]))
                    {
                        closeList[newIndex] = 1;
                        continue;
                    }

                    float cost = costMap[curIndex];
                    if (dx != 0 && dy != 0)
                        cost *= sqr2;

                    float newHeat = heatMap[curIndex] + cost;
                    if (newHeat < heatMap[newIndex])
                    {
                        heatMap[newIndex] = newHeat;
                        if (inOpenList[newIndex] == 0)
                        {
                            openList.Enqueue(newIndex);
                            inOpenList[newIndex] = 1;
                        }
                    }
                }
            }
        }
    }
}
