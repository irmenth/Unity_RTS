using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct HeatMapJob : IJob
{
    private readonly int width, height;
    private readonly int destination;
    private NativeQueue<int> openList;
    private NativeArray<byte> inOpenList;
    private NativeArray<byte> closeList;
    private NativeArray<DirectionCell> directionGrid;

    public HeatMapJob(int width, int height, int destination, NativeQueue<int> openList, NativeArray<byte> inOpenList, NativeArray<byte> closeList, NativeArray<DirectionCell> directionGrid)
    {
        this.width = width;
        this.height = height;
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

        for (int i = 0; i < width * height; i++)
        {
            ChangeHeat(i, float.PositiveInfinity);
        }
        ChangeHeat(destination, 0f);
        openList.Enqueue(destination);
        inOpenList[destination] = 1;

        while (openList.Count > 0)
        {
            int curIndex = openList.Dequeue();
            inOpenList[curIndex] = 0;
            closeList[curIndex] = 1;

            int2 curGridPos = new(curIndex / height, curIndex % height);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = curGridPos.x + dx, ny = curGridPos.y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    int newIndex = nx * height + ny;
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
