using System;
using Unity.Mathematics;

public enum ObstacleType
{
    Circle,
    Rectangle
}

public struct ObstacleData
{
    public ObstacleType type;
    public Circle circle;
    public Rectangle rect;
    public int id;

    public ObstacleData(ObstacleType type, Circle circle, Rectangle rect)
    {
        this.type = type;
        this.circle = circle;
        this.rect = rect;
        id = int.MaxValue;
    }
    public static bool operator ==(ObstacleData a, ObstacleData b) => a.id == b.id;
    public static bool operator !=(ObstacleData a, ObstacleData b) => !(a == b);
    public override readonly bool Equals(object obj) => obj is ObstacleData other && this == other;
    public override readonly int GetHashCode() => id.GetHashCode();
}

public struct Circle
{
    public float2 center;
    public float radius;

    public Circle(float2 center, float radius)
    {
        this.center = center;
        this.radius = radius;
    }
}

public struct Rectangle
{
    public float2 center;
    public float2 size;
    public float2 right;
    public float2 up;

    public Rectangle(float2 center, float2 size, float2 right, float2 up)
    {
        this.center = center;
        this.size = size;
        this.right = right;
        this.up = up;
    }
}
