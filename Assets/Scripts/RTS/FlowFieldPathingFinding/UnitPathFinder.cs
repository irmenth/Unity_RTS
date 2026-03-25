using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private float unitRadius;
    [SerializeField] private float moveSpeed;

    private Vector2 acceleration;

    private void SteeringCorrectAcceleration()
    {
        acceleration = Vector2.zero;

        var selfPos = transform.position;
        var flowField = gridCC.CurFlowField;
        var og = flowField.ObstacleGrid;

        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(selfPos);
        var predictOgPos = flowField.WorldToObstacleGridPos(predictPos);
        if (predictOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        foreach (var otherUnit in og[predictOgPos.x, predictOgPos.y].unitList)
        {
            if (otherUnit.transform == transform) continue;

            var otherPos = otherUnit.transform.position;
            if (Vector3.SqrMagnitude(otherPos - selfPos) <= Mathf.Pow(unitRadius + otherUnit.unitRadius + 0.5f, 2))
            {
                acceleration += 100f / Vector3.Distance(otherPos, selfPos) * UsefulUtils.V3ToV2(selfPos - otherPos).normalized;
            }
        }
    }

    private Vector2 velocity;

    private void KenimaticCorrectVelocity()
    {
        var selfPos = transform.position;
        var flowField = gridCC.CurFlowField;
        var dg = flowField.DirGrid;
        var og = flowField.ObstacleGrid;

        Vector2Int unitDgPos = flowField.WorldToDirGridPos(selfPos);
        Vector2Int unitOgPos = flowField.WorldToObstacleGridPos(selfPos);
        if (unitDgPos == new Vector2Int(-1, -1) || unitOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(selfPos);
        var predictOgPos = flowField.WorldToObstacleGridPos(predictPos);
        if (predictOgPos == new Vector2Int(-1, -1))
        {
            velocity = Vector2.zero;
            return;
        }

        var arrived = false;
        if (Vector2.SqrMagnitude(UsefulUtils.V3ToV2(selfPos) - destination) <= Mathf.Pow(unitRadius, 2))
        {
            arrived = true;
            if (velocity == Vector2.zero)
            {
                IsMovingTo = false;
                return;
            }

            acceleration += -8f * moveSpeed * velocity.normalized;
            velocity += acceleration * Time.deltaTime;
            if (Vector2.Dot(velocity, acceleration) > 0)
                velocity = Vector2.zero;
        }

        var dir = dg[unitDgPos.x, unitDgPos.y].direction;
        if (!arrived && dir != -Vector2.one)
        {
            acceleration += 8f * moveSpeed * dir;
            var cost = dg[unitDgPos.x, unitDgPos.y].cost;
            var curMoveSpeed = float.IsInfinity(cost) ? moveSpeed : moveSpeed / cost;
            velocity = Vector2.ClampMagnitude(velocity + acceleration * Time.deltaTime, curMoveSpeed);
        }

        var obstacleList = og[predictOgPos.x, predictOgPos.y].obstacleList;
        for (int i = 0; i < obstacleList.Count; i++)
        {
            var obstacle = obstacleList[i];

            switch (obstacle.type)
            {
                case ObstacleType.Circle:
                    UsefulUtils.IfIntersectWithCircleObstacle(obstacle.circle, transform, unitRadius);
                    if (UsefulUtils.HasCollideWithCircleObstacle(obstacle.circle, predictPos, unitRadius, out var negImpactDir))
                        velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
                    break;
                case ObstacleType.Rectangle:
                    UsefulUtils.IfIntersectWithRectObstacle(obstacle.rect, transform, unitRadius);
                    if (UsefulUtils.HasCollideWithRectObstacle(obstacle.rect, predictPos, unitRadius, out negImpactDir))
                        velocity = UsefulUtils.ProjectOnLine(velocity, negImpactDir);
                    break;
            }
        }
    }

    public bool IsMovingTo { get; private set; }
    private Vector2 destination;

    private void MarkIsMovingTo(MoveToEvent evt)
    {
        IsMovingTo = true;
        destination = UsefulUtils.V3ToV2(evt.destination);
    }

    private void Awake()
    {
        EventBus.Subscribe<MoveToEvent>(MarkIsMovingTo);
    }

    private void Update()
    {
        SteeringCorrectAcceleration();

        if (IsMovingTo)
            KenimaticCorrectVelocity();

        transform.position += UsefulUtils.V2ToV3(velocity) * Time.deltaTime;
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MoveToEvent>(MarkIsMovingTo);
    }
}
