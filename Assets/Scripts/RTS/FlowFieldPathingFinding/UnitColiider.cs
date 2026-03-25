using UnityEngine;

public class UnitColiider : MonoBehaviour
{
    [SerializeField] private GridController gridCC;
    [SerializeField] private UnitPathFinder pathFinder;

    private Vector2Int lastGridPos;

    private void UpdateUnitCurrentCell()
    {
        var flowField = gridCC.CurFlowField;
        var og = flowField.ObstacleGrid;

        var unitGridPos = flowField.WorldToObstacleGridPos(transform.position);
        if (unitGridPos == new Vector2Int(-1, -1)) return;

        if (unitGridPos != lastGridPos)
            og[unitGridPos.x, unitGridPos.y].unitList.Remove(pathFinder);

        if (!og[unitGridPos.x, unitGridPos.y].unitList.Contains(pathFinder))
            og[unitGridPos.x, unitGridPos.y].unitList.Add(pathFinder);

        lastGridPos = unitGridPos;
    }

    private void Update()
    {
        if (!pathFinder.IsMovingTo) return;

        UpdateUnitCurrentCell();
    }
}
