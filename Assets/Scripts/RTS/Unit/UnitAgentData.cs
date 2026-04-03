using Unity.Mathematics;

public struct UnitAgentData
{
    public int id;
    public readonly float radius;
    public readonly float speed;
    public float2 position;
    public float2 velocity;
    public int dgIndex;
    public int ogIndex;

    public UnitAgentData(float radius, float speed, float2 position)
    {
        id = int.MaxValue;
        this.radius = radius;
        this.speed = speed;
        this.position = position;
        velocity = new float2(0, 0);
        dgIndex = -1;
        ogIndex = -1;
    }
    public static bool operator ==(UnitAgentData a, UnitAgentData b) => a.id == b.id;
    public static bool operator !=(UnitAgentData a, UnitAgentData b) => a.id != b.id;
    public override readonly bool Equals(object obj) => obj is UnitAgentData other && this == other;
    public override readonly int GetHashCode() => id.GetHashCode();
}