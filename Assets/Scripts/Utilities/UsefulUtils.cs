using UnityEngine;

public static class UsefulUtils
{
    public static int GetLayer(LayerMask mask)
    {
        int maskValue = mask.value;
        int layer = -1;

        while (maskValue > 0)
        {
            maskValue >>= 1;
            layer++;
        }

        return layer;
    }

    public static Vector3 V2ToV3(Vector2 v2, float y = 0f)
    {
        return new Vector3(v2.x, y, v2.y);
    }

    public static Vector2 V3ToV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.z);
    }

    public static Vector2 ProjectOnLine(Vector2 inVec, Vector2 normal)
    {
        return inVec - Vector2.Dot(inVec, normal) * normal;
    }

    public static bool HasCollideWithCircleObstacle(Circle circle, Vector3 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        var center = V3ToV2(circle.transform.position);
        var isCollided = Vector2.SqrMagnitude(center - V3ToV2(unitWS)) < Mathf.Pow(circle.radius + unitRadius, 2);
        if (isCollided)
            negImpactDir = (V3ToV2(unitWS) - center).normalized;
        else
            negImpactDir = Vector2.zero;
        return isCollided;
    }
    public static bool HasCollideWithCircleObstacle(Circle circle, Vector2 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        return HasCollideWithCircleObstacle(circle, V2ToV3(unitWS), unitRadius, out negImpactDir);
    }

    /// <summary>
    /// If intersect, correct position automatically
    /// </summary>
    /// <param name="circle"></param>
    /// <param name="unitTrans"></param>
    /// <param name="unitRadius"></param>
    /// <returns></returns>
    public static bool IfIntersectWithCircleObstacle(Circle circle, Transform unitTrans, float unitRadius)
    {
        var center = circle.transform.position;
        var unitWS = unitTrans.position;
        var isInside = Vector3.SqrMagnitude(center - unitWS) < Mathf.Pow(circle.radius + unitRadius, 2);
        if (isInside)
        {
            var dir = (V3ToV2(unitWS) - V3ToV2(center)).normalized;
            unitTrans.SetPositionAndRotation(center + V2ToV3(dir) * (circle.radius + unitRadius), unitTrans.rotation);
        }

        return isInside;
    }

    public static bool HasCollideWithRectObstacle(Rectangle rect, Vector3 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        var rectTrans = rect.transform;
        var size = new Vector2(rect.baseSize.x * rectTrans.lossyScale.x, rect.baseSize.y * rectTrans.lossyScale.z);
        var unitWS2D = V3ToV2(unitWS);
        var right = rectTrans.right;
        var up = rectTrans.forward;

        var unitToCenter = unitWS - rectTrans.position;
        var projX = Vector3.Dot(unitToCenter, right);
        var projY = Vector3.Dot(unitToCenter, up);
        var unitLS = new Vector2(projX, projY);

        var isInside = unitLS.x < size.x / 2 && unitLS.x > -size.x / 2 && unitLS.y < size.y / 2 && unitLS.y > -size.y / 2;

        var closestPoint = Vector2.zero;
        closestPoint.x = Mathf.Clamp(unitLS.x, -size.x / 2, size.x / 2);
        closestPoint.y = Mathf.Clamp(unitLS.y, -size.y / 2, size.y / 2);
        var closestPointWS = V3ToV2(rectTrans.position + right * closestPoint.x + up * closestPoint.y);

        var isCollided = Vector2.SqrMagnitude(closestPoint - unitLS) < Mathf.Pow(unitRadius, 2) || isInside;
        if (isCollided)
        {
            var dir = (unitWS2D - closestPointWS).normalized;
            if (isInside)
                negImpactDir = -dir;
            else
                negImpactDir = dir;
        }
        else
        {
            negImpactDir = Vector2.zero;
        }
        return isCollided;
    }
    public static bool HasCollideWithRectObstacle(Rectangle rect, Vector2 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        return HasCollideWithRectObstacle(rect, V2ToV3(unitWS), unitRadius, out negImpactDir);
    }

    /// <summary>
    /// If intersect, correct position automatically
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="unitTrans"></param>
    /// <param name="unitRadius"></param>
    /// <returns></returns>
    public static bool IfIntersectWithRectObstacle(Rectangle rect, Transform unitTrans, float unitRadius)
    {
        var rectTrans = rect.transform;
        var size = new Vector2(rect.baseSize.x * rectTrans.lossyScale.x, rect.baseSize.y * rectTrans.lossyScale.z);
        var unitWS = unitTrans.position;
        var right = rectTrans.right;
        var up = rectTrans.forward;

        var unitToCenter = unitWS - rectTrans.position;
        var projX = Vector3.Dot(unitToCenter, right);
        var projY = Vector3.Dot(unitToCenter, up);
        var unitLS = new Vector2(projX, projY);

        var isInside = unitLS.x < size.x / 2 && unitLS.x > -size.x / 2 && unitLS.y < size.y / 2 && unitLS.y > -size.y / 2;

        var closestPoint = Vector2.zero;
        closestPoint.x = Mathf.Clamp(unitLS.x, -size.x / 2, size.x / 2);
        closestPoint.y = Mathf.Clamp(unitLS.y, -size.y / 2, size.y / 2);
        var closestPointWS = rectTrans.position + right * closestPoint.x + up * closestPoint.y;

        var isIntersected = Vector2.SqrMagnitude(closestPoint - unitLS) < Mathf.Pow(unitRadius, 2) || isInside;
        if (isIntersected)
        {
            var dir = (unitWS - closestPointWS).normalized;
            if (isInside)
                unitTrans.SetPositionAndRotation(closestPointWS - dir * unitRadius, unitTrans.rotation);
            else
                unitTrans.SetPositionAndRotation(closestPointWS + dir * unitRadius, unitTrans.rotation);
        }

        return isIntersected;
    }
}
