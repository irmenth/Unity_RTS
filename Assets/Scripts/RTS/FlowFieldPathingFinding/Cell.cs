using Unity.Mathematics;

public struct Cell
{
    public int index;
    public float2 worldPos;

    public Cell(int index, float2 worldPos)
    {
        this.index = index;
        this.worldPos = worldPos;
    }
}
