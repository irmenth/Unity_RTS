using UnityEngine;

public class ObstacleCollider : MonoBehaviour
{
    [SerializeField] private ObstacleType obstacleType;
    [SerializeField] private float circleRadius;

    public Obstacles obstacle;

    private void Awake()
    {
        obstacle = new Obstacles(obstacleType, new Circle(transform, circleRadius), new Rectangle(transform));
    }
}
