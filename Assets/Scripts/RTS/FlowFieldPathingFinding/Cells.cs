using Unity.Mathematics;

public struct DirectionCell
{
    public int index;
    public float2 worldPos;
    public float cost;
    /// <summary>
    /// Positive Infinity means impassible
    /// </summary>
    public float heat;
    /// <summary>
    /// (-1, -1) means impassible
    /// </summary>
    public float2 direction;

    public DirectionCell(int index, float2 worldPos)
    {
        this.index = index;
        this.worldPos = worldPos;
        cost = 1;
        heat = float.PositiveInfinity;
        direction = float2.zero;
    }
}

public struct ObstacleCell
{
    public int index;
    public float2 worldPos;

    public ObstacleCell(int index, float2 worldPos)
    {
        this.worldPos = worldPos;
        this.index = index;
    }
}
