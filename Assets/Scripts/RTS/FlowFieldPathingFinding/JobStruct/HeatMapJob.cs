using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct HeatMapJob : IJob
{
    public int width, height;
    public int destination;
    public NativeQueue<int> openList;
    public NativeArray<byte> inOpenList;
    public NativeArray<byte> closeList;
    public NativeArray<float> costMap;
    public NativeArray<float> heatMap;

    public HeatMapJob(int width, int height, int destination, NativeQueue<int> openList, NativeArray<byte> inOpenList, NativeArray<byte> closeList, NativeArray<float> costMap, NativeArray<float> heatMap)
    {
        this.width = width;
        this.height = height;
        this.destination = destination;
        this.openList = openList;
        this.inOpenList = inOpenList;
        this.closeList = closeList;
        this.costMap = costMap;
        this.heatMap = heatMap;
    }

    public void Execute()
    {
        for (int i = 0; i < width * height; i++)
        {
            heatMap[i] = float.PositiveInfinity;
        }
        heatMap[destination] = 0f;
        openList.Enqueue(destination);
        inOpenList[destination] = 1;

        while (openList.Count > 0)
        {
            var curIndex = openList.Dequeue();
            inOpenList[curIndex] = 0;
            closeList[curIndex] = 1;

            var curGridPos = new int2(curIndex / height, curIndex % height);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = curGridPos.x + dx, ny = curGridPos.y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    int newIndex = nx * height + ny;
                    if (closeList[newIndex] == 1) continue;
                    if (math.isinf(costMap[newIndex]))
                    {
                        closeList[newIndex] = 1;
                        continue;
                    }

                    float cost = costMap[newIndex];
                    if (dx != 0 && dy != 0)
                        cost *= 1.4f;

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
