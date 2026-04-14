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
    private NativeArray<DirectionCell> directionGrid;

    public HeatMapJob(
        int2 size,
        NativeArray<int> destination,
        NativeQueue<int> openList,
        NativeArray<byte> inOpenList,
        NativeArray<byte> closeList,
        NativeArray<DirectionCell> directionGrid
        )
    {
        this.size = size;
        this.destination = destination;
        this.openList = openList;
        this.inOpenList = inOpenList;
        this.closeList = closeList;
        this.directionGrid = directionGrid;
    }

    private void ChangeHeat(int index, float heat)
    {
        DirectionCell cell = directionGrid[index];
        cell.heat = heat;
        directionGrid[index] = cell;
    }

    public void Execute()
    {
        float sqr2 = math.sqrt(2f);

        for (int i = 0; i < size.x * size.y; i++)
        {
            ChangeHeat(i, float.PositiveInfinity);
        }

        if (math.isinf(directionGrid[destination[0]].cost))
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

                        if (math.isfinite(directionGrid[newIndex].cost))
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

        ChangeHeat(destination[0], 0f);
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
                    if (math.isinf(directionGrid[newIndex].cost))
                    {
                        closeList[newIndex] = 1;
                        continue;
                    }

                    float cost = directionGrid[curIndex].cost;
                    if (dx != 0 && dy != 0)
                        cost *= sqr2;

                    float newHeat = directionGrid[curIndex].heat + cost;
                    if (newHeat < directionGrid[newIndex].heat)
                    {
                        ChangeHeat(newIndex, newHeat);
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
