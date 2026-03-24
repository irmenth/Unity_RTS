using System.Collections.Generic;
using UnityEngine;

public class UnitPathFinder : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private float unitRadius;
    [SerializeField] private float moveSpeed;

    private Vector2 velocity;

    private void SteeringGetVelocity()
    {
        var flowField = gridCC.CurFlowField;
        var grid = flowField.Grid;
        Vector2Int unitGridPos = flowField.WorldToGridPos(transform.position);
        if (unitGridPos == new Vector2Int(-1, -1)) return;

        // var dir = grid[unitGridPos.x, unitGridPos.y].direction;
        // if (dir == -Vector2.one) return;

        // var acceleration = 8f * moveSpeed * dir;
        // if (dir == Vector2.zero)
        // {
        //     if (velocity == Vector2.zero) return;

        //     acceleration = -8f * moveSpeed * velocity.normalized;
        //     velocity += acceleration * Time.deltaTime;
        //     if (Vector2.Dot(velocity, acceleration) > 0) velocity = Vector2.zero;
        //     return;
        // }
        // var cost = grid[unitGridPos.x, unitGridPos.y].cost;
        // var curMoveSpeed = float.IsInfinity(cost) ? moveSpeed : moveSpeed / cost;
        // velocity = Vector2.ClampMagnitude(velocity + acceleration * Time.deltaTime, curMoveSpeed);

        velocity = Vector2.left;

        var predictPos = Time.deltaTime * velocity + UsefulUtils.V3ToV2(transform.position) + unitRadius * velocity.normalized;
        var predictGridPos = flowField.WorldToGridPos(predictPos);
        if (unitGridPos == new Vector2Int(-1, -1)) return;

        var obstacleList = grid[predictGridPos.x, predictGridPos.y].obstacleList;
        for (int i = 0; i < obstacleList.Count; i++)
        {
            switch (obstacleList[i].type)
            {
                case ObstacleType.Circle:
                    if (UsefulUtils.HasCollideWithCircleObstacle(obstacleList[i].circle, predictPos, unitRadius, out var negImpactDir))
                    {
                        var probableReflectDir = new Vector2(-negImpactDir.y, negImpactDir.x);
                        if (Vector2.Dot(probableReflectDir, velocity) > 0)
                            velocity = Vector2.Reflect(velocity, probableReflectDir);
                        else
                            velocity = Vector2.Reflect(velocity, -probableReflectDir);
                    }
                    break;
                case ObstacleType.Rectangle:
                    if (UsefulUtils.HasCollideWithRectObstacle(obstacleList[i].rect, predictPos, unitRadius, out negImpactDir))
                    {
                        // Debug.Log(negImpactDir);
                        // var probableReflectDir = new Vector2(-negImpactDir.y, negImpactDir.x);
                        // if (Vector2.Dot(probableReflectDir, velocity) > 0)
                        //     velocity = Vector2.Reflect(velocity, probableReflectDir);
                        // else
                        //     velocity = Vector2.Reflect(velocity, -probableReflectDir);
                        velocity = Vector2.Reflect(velocity, negImpactDir);
                    }
                    break;
            }
        }
    }

    private void Update()
    {
        SteeringGetVelocity();
        transform.position += UsefulUtils.V2ToV3(velocity) * Time.deltaTime;
    }
}
