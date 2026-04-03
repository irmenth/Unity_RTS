using Unity.Mathematics;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private Transform cube;
    [SerializeField] private Transform cylinder;

    private Rectangle rect;
    private Circle circle;

    private void Awake()
    {
        rect = new(new(cube.position.x, cube.position.z), new(cube.localScale.x, cube.localScale.z), new(cube.right.x, cube.right.z), new(cube.forward.x, cube.forward.z));
        circle = new(new(cylinder.position.x, cylinder.position.z), cylinder.localScale.x / 2f);
    }

    private float2 velocity;

    private void Update()
    {
        velocity = new(-4, 0);
        float2 pos = new(transform.position.x, transform.position.z);

        if (UsefulUtils.HasCollideWithRectObstacle(rect, pos, 0.25f, out float2 negImpactDir))
            velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
        if (UsefulUtils.HasCollideWithCircleObstacle(circle, pos, 0.25f, out negImpactDir))
            velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);

        pos += Time.deltaTime * velocity;
        pos = UsefulUtils.IfIntersectWithRectObstacle(rect, pos, 0.25f);
        pos = UsefulUtils.IfIntersectWithCircleObstacle(circle, pos, 0.25f);

        transform.position = new(pos.x, transform.position.y, pos.y);
    }
}
