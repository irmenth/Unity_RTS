using Unity.Mathematics;

public struct UnitAgentData
{
    public int id;
    public readonly float radius;
    public readonly float speed;
    public float curMaxSpeed;
    public float2 position;
    public float2 velocity;
    public int dgIndex;
    public int ogIndex;
    public float2 lastDir;

    public UnitAgentData(float radius, float speed, float2 position)
    {
        id = int.MaxValue;
        this.radius = radius;
        this.speed = speed;
        curMaxSpeed = speed;
        this.position = position;
        velocity = new float2(0, 0);
        dgIndex = -1;
        ogIndex = -1;
        lastDir = new float2(float.PositiveInfinity, float.PositiveInfinity);
    }
    public static bool operator ==(UnitAgentData a, UnitAgentData b) => a.id == b.id;
    public static bool operator !=(UnitAgentData a, UnitAgentData b) => a.id != b.id;
    public override readonly bool Equals(object obj) => obj is UnitAgentData other && this == other;
    public override readonly int GetHashCode() => id.GetHashCode();
    public override readonly string ToString() => $"unit agent data:\nid: {id}, radius: {radius}, speed: {speed}, position: {position}, velocity: {velocity}, dgIndex: {dgIndex}, ogIndex: {ogIndex}";
}