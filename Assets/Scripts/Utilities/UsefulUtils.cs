using Unity.Mathematics;
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
        var center = circle.transform.position;
        var center2D = V3ToV2(center);
        var unitWS2D = V3ToV2(unitWS);
        var isCollided = Vector2.SqrMagnitude(center2D - unitWS2D) < Mathf.Pow(circle.radius + unitRadius, 2);
        var isInside = isCollided && Vector2.SqrMagnitude(center2D - unitWS2D) < Mathf.Pow(circle.radius, 2);

        if (isCollided)
        {
            var dir = (unitWS2D - center2D).normalized;
            if (!isInside)
                negImpactDir = dir;
            else
                negImpactDir = -dir;
        }
        else
        {
            negImpactDir = Vector2.zero;
        }

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
    public static Vector3 IfIntersectWithCircleObstacle(Circle circle, Vector3 unitWS, float unitRadius)
    {
        var position = unitWS;
        var center = circle.transform.position;
        var center2D = V3ToV2(center);
        var unitWS2D = V3ToV2(unitWS);
        var isIntersected = Vector2.SqrMagnitude(center2D - unitWS2D) < Mathf.Pow(circle.radius + unitRadius, 2);
        var isInside = isIntersected && Vector2.SqrMagnitude(center2D - unitWS2D) < Mathf.Pow(circle.radius, 2);

        if (isIntersected)
        {
            var dir = (unitWS2D - center2D).normalized;
            position = center + V2ToV3(dir) * (circle.radius + unitRadius);
        }

        return position;
    }

    public static bool HasCollideWithRectObstacle(Rectangle rect, Vector3 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        var rectTrans = rect.transform;
        var center2D = V3ToV2(rectTrans.position);
        var halfSize = new Vector2(rect.baseSize.x * rectTrans.lossyScale.x, rect.baseSize.y * rectTrans.lossyScale.z) / 2;
        var unitWS2D = V3ToV2(unitWS);
        var right = rectTrans.right;
        var up = rectTrans.forward;

        var unitToCenter = V2ToV3(unitWS2D - center2D);
        var projX = Vector3.Dot(unitToCenter, right);
        var projY = Vector3.Dot(unitToCenter, up);
        var unitLS = new Vector2(projX, projY);

        var isInside = unitLS.x < halfSize.x && unitLS.x > -halfSize.x && unitLS.y < halfSize.y && unitLS.y > -halfSize.y;

        var closestPoint = Vector2.zero;
        if (!isInside)
        {
            closestPoint.x = Mathf.Clamp(unitLS.x, -halfSize.x, halfSize.x);
            closestPoint.y = Mathf.Clamp(unitLS.y, -halfSize.y, halfSize.y);
        }
        else
        {
            if (Mathf.Min(halfSize.x - unitLS.x, unitLS.x + halfSize.x) < Mathf.Min(halfSize.y - unitLS.y, unitLS.y + halfSize.y))
            {
                closestPoint.y = Mathf.Clamp(unitLS.y, -halfSize.y, halfSize.y);
                closestPoint.x = unitLS.x > 0 ? halfSize.x : -halfSize.x;
            }
            else
            {
                closestPoint.x = Mathf.Clamp(unitLS.x, -halfSize.x, halfSize.x);
                closestPoint.y = unitLS.y > 0 ? halfSize.y : -halfSize.y;
            }
        }

        if (Mathf.Abs(unitLS.x - halfSize.x) < 1e-3f)
            closestPoint.x += unitLS.x > halfSize.x ? -1e-3f : 1e-3f;
        else if (Mathf.Abs(unitLS.x + halfSize.x) < 1e-3f)
            closestPoint.x += unitLS.x > -halfSize.x ? -1e-3f : 1e-3f;
        if (Mathf.Abs(unitLS.y - halfSize.y) < 1e-3f)
            closestPoint.y += unitLS.y > halfSize.y ? -1e-3f : 1e-3f;
        else if (Mathf.Abs(unitLS.y + halfSize.y) < 1e-3f)
            closestPoint.y += unitLS.y > -halfSize.y ? -1e-3f : 1e-3f;

        var closestPointWS = V3ToV2(V2ToV3(center2D) + right * closestPoint.x + up * closestPoint.y);
        var isCollided = isInside || Vector2.SqrMagnitude(closestPoint - unitLS) < Mathf.Pow(unitRadius, 2);
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
    public static Vector3 IfIntersectWithRectObstacle(Rectangle rect, Vector3 unitWS, float unitRadius)
    {
        var position = unitWS;
        var rectTrans = rect.transform;
        var center2D = V3ToV2(rectTrans.position);
        var halfSize = new Vector2(rect.baseSize.x * rectTrans.lossyScale.x, rect.baseSize.y * rectTrans.lossyScale.z) / 2;
        var unitWS2D = V3ToV2(unitWS);
        var right = rectTrans.right;
        var up = rectTrans.forward;

        var unitToCenter = V2ToV3(unitWS2D - center2D);
        var projX = Vector3.Dot(unitToCenter, right);
        var projY = Vector3.Dot(unitToCenter, up);
        var unitLS = new Vector2(projX, projY);

        var isInside = unitLS.x < halfSize.x && unitLS.x > -halfSize.x && unitLS.y < halfSize.y && unitLS.y > -halfSize.y;

        var closestPoint = Vector2.zero;
        if (!isInside)
        {
            closestPoint.x = Mathf.Clamp(unitLS.x, -halfSize.x, halfSize.x);
            closestPoint.y = Mathf.Clamp(unitLS.y, -halfSize.y, halfSize.y);
        }
        else
        {
            if (Mathf.Min(halfSize.x - unitLS.x, unitLS.x + halfSize.x) < Mathf.Min(halfSize.y - unitLS.y, unitLS.y + halfSize.y))
            {
                closestPoint.y = Mathf.Clamp(unitLS.y, -halfSize.y, halfSize.y);
                closestPoint.x = unitLS.x > 0 ? halfSize.x : -halfSize.x;
            }
            else
            {
                closestPoint.x = Mathf.Clamp(unitLS.x, -halfSize.x, halfSize.x);
                closestPoint.y = unitLS.y > 0 ? halfSize.y : -halfSize.y;
            }
        }

        if (Mathf.Abs(unitLS.x - halfSize.x) < 1e-3f)
            closestPoint.x += unitLS.x > halfSize.x ? -1e-3f : 1e-3f;
        else if (Mathf.Abs(unitLS.x + halfSize.x) < 1e-3f)
            closestPoint.x += unitLS.x > -halfSize.x ? -1e-3f : 1e-3f;
        if (Mathf.Abs(unitLS.y - halfSize.y) < 1e-3f)
            closestPoint.y += unitLS.y > halfSize.y ? -1e-3f : 1e-3f;
        else if (Mathf.Abs(unitLS.y + halfSize.y) < 1e-3f)
            closestPoint.y += unitLS.y > -halfSize.y ? -1e-3f : 1e-3f;

        var closestPointWS = V3ToV2(V2ToV3(center2D) + right * closestPoint.x + up * closestPoint.y);
        var closestPointWS3D = V2ToV3(closestPointWS);
        var isIntersected = Vector2.SqrMagnitude(closestPoint - unitLS) < Mathf.Pow(unitRadius, 2) || isInside;
        if (isIntersected)
        {
            var dir = V2ToV3((unitWS2D - closestPointWS).normalized);
            if (isInside)
                position = closestPointWS3D - dir * unitRadius;
            else
                position = closestPointWS3D + dir * unitRadius;
        }

        return position;
    }
}
