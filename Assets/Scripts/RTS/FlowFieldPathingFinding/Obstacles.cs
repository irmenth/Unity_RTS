using UnityEngine;

public enum ObstacleType
{
    Circle,
    Rectangle
}

public class Obstacles
{
    public ObstacleType type;
    public Circle circle;
    public Rectangle rect;

    public Obstacles(ObstacleType type, Circle circle, Rectangle rect)
    {
        this.type = type;
        this.circle = circle;
        this.rect = rect;
    }
}

public struct Circle
{
    public Transform transform;
    public float radius;

    public Circle(Transform transform, float radius)
    {
        this.transform = transform;
        this.radius = radius;
    }
}

public struct Rectangle
{
    public Transform transform;
    public Vector2[] verteices;

    public Rectangle(Transform transform)
    {
        this.transform = transform;
        var center = UsefulUtils.V3ToV2(transform.position);
        var size = UsefulUtils.V3ToV2(transform.localScale);
        var rad = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

        verteices = new Vector2[4];
        verteices[0] = -0.5f * size.x * Mathf.Cos(rad) * Vector2.right + 0.5f * size.y * Mathf.Sin(rad) * Vector2.up + center;
        verteices[1] = 0.5f * size.x * Mathf.Cos(rad) * Vector2.right + 0.5f * size.y * Mathf.Sin(rad) * Vector2.up + center;
        verteices[2] = 0.5f * size.x * Mathf.Cos(rad) * Vector2.right - 0.5f * size.y * Mathf.Sin(rad) * Vector2.up + center;
        verteices[3] = -0.5f * size.x * Mathf.Cos(rad) * Vector2.right - 0.5f * size.y * Mathf.Sin(rad) * Vector2.up + center;
    }
}
