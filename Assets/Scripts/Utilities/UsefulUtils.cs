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

    public static bool Approximately(Vector2 a, Vector2 b, float eps = 1e-3f) => Vector2.SqrMagnitude(a - b) < Mathf.Pow(eps, 2f);

    public static bool Approximately(Vector3 a, Vector3 b, float eps = 1e-3f) => Vector3.SqrMagnitude(a - b) < Mathf.Pow(eps, 2f);

    public static bool Approximately(float2 a, float2 b, float eps = 1e-3f) => math.lengthsq(a - b) < math.pow(eps, 2f);

    public static bool Approximately(float3 a, float3 b, float eps = 1e-3f) => math.lengthsq(a - b) < math.pow(eps, 2f);

    public static float2 ClampMagnitude(float2 v, float maxLength)
    {
        bool mask = math.length(v) <= maxLength;
        return math.select(math.normalizesafe(v) * maxLength, v, mask);
    }

    public static Vector2 ProjectOnLine(Vector2 inVec, Vector2 normal) => inVec - Vector2.Dot(inVec, normal) * normal;

    public static float2 ProjectOnLine(float2 inVec, float2 normal) => inVec - math.dot(inVec, normal) * normal;

    public static bool HasCollideWithCircleObstacle(Circle circle, float2 unitWS, float unitRadius, out float2 negImpactDir)
    {
        float2 center = circle.center;
        bool isCollided = math.lengthsq(center - unitWS) < math.pow(circle.radius + unitRadius, 2);
        negImpactDir = math.select(float2.zero, math.normalizesafe(unitWS - center), isCollided);

        return isCollided;
    }

    /// <summary>
    /// If intersect, correct position automatically
    /// </summary>
    /// <param name="circle"></param>
    /// <param name="unitTrans"></param>
    /// <param name="unitRadius"></param>
    /// <returns></returns>
    public static float2 IfIntersectWithCircleObstacle(Circle circle, float2 unitWS, float unitRadius)
    {
        float2 position = unitWS;
        float2 center = circle.center;
        bool isIntersected = math.lengthsq(center - unitWS) < math.pow(circle.radius + unitRadius, 2);

        if (isIntersected)
        {
            float2 dir = math.normalizesafe(unitWS - center);
            position = center + (circle.radius + 1e-3f + unitRadius) * dir;
        }

        return position;
    }

    public static bool HasCollideWithRectObstacle(Rectangle rect, float2 unitWS, float unitRadius, out float2 negImpactDir)
    {
        negImpactDir = float2.zero;

        float2 center = rect.center;
        float2 halfSize = rect.size / 2f;
        float2 right = rect.right;
        float2 up = rect.up;

        float2 unitToCenter = unitWS - center;
        float projX = math.dot(unitToCenter, right);
        float projY = math.dot(unitToCenter, up);
        float2 unitLS = new(projX, projY);

        bool isInside = unitLS.x < halfSize.x && unitLS.x > -halfSize.x && unitLS.y < halfSize.y && unitLS.y > -halfSize.y;

        float2 closestPoint = float2.zero;
        if (!isInside)
        {
            closestPoint.x = math.clamp(unitLS.x, -halfSize.x, halfSize.x);
            closestPoint.y = math.clamp(unitLS.y, -halfSize.y, halfSize.y);
        }
        else
        {
            if (math.min(halfSize.x - unitLS.x, unitLS.x + halfSize.x) < math.min(halfSize.y - unitLS.y, unitLS.y + halfSize.y))
            {
                closestPoint.y = math.clamp(unitLS.y, -halfSize.y, halfSize.y);
                closestPoint.x = math.select(-halfSize.x, halfSize.x, unitLS.x > 0);
            }
            else
            {
                closestPoint.x = math.clamp(unitLS.x, -halfSize.x, halfSize.x);
                closestPoint.y = math.select(-halfSize.y, halfSize.y, unitLS.y > 0);
            }
        }

        bool isCollided = isInside || math.lengthsq(closestPoint - unitLS) < math.pow(unitRadius, 2);
        if (isCollided)
        {
            if (math.abs(unitLS.x - halfSize.x) < 1e-3f)
                closestPoint.x += math.select(1e-3f, -1e-3f, unitLS.x > closestPoint.x);
            else if (math.abs(unitLS.x + halfSize.x) < 1e-3f)
                closestPoint.x += math.select(1e-3f, -1e-3f, unitLS.x > closestPoint.x);
            if (math.abs(unitLS.y - halfSize.y) < 1e-3f)
                closestPoint.y += math.select(1e-3f, -1e-3f, unitLS.y > closestPoint.y);
            else if (math.abs(unitLS.y + halfSize.y) < 1e-3f)
                closestPoint.y += math.select(1e-3f, -1e-3f, unitLS.y > closestPoint.y);

            float2 dirLS = unitLS - closestPoint;
            float2 dirWS = math.normalizesafe(dirLS.x * right + dirLS.y * up);
            negImpactDir = math.select(dirWS, -dirWS, isInside);
        }

        return isCollided;
    }

    /// <summary>
    /// If intersect, correct position automatically
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="unitTrans"></param>
    /// <param name="unitRadius"></param>
    /// <returns></returns>
    public static float2 IfIntersectWithRectObstacle(Rectangle rect, float2 unitWS, float unitRadius)
    {
        float2 position = unitWS;
        float2 center = rect.center;
        float2 halfSize = rect.size / 2f;
        float2 right = rect.right;
        float2 up = rect.up;

        float2 unitToCenter = unitWS - center;
        float projX = math.dot(unitToCenter, right);
        float projY = math.dot(unitToCenter, up);
        float2 unitLS = new(projX, projY);

        bool isInside = unitLS.x < halfSize.x && unitLS.x > -halfSize.x && unitLS.y < halfSize.y && unitLS.y > -halfSize.y;

        float2 closestPoint = float2.zero;
        if (!isInside)
        {
            closestPoint.x = math.clamp(unitLS.x, -halfSize.x, halfSize.x);
            closestPoint.y = math.clamp(unitLS.y, -halfSize.y, halfSize.y);
        }
        else
        {
            if (math.min(halfSize.x - unitLS.x, unitLS.x + halfSize.x) < math.min(halfSize.y - unitLS.y, unitLS.y + halfSize.y))
            {
                closestPoint.y = math.clamp(unitLS.y, -halfSize.y, halfSize.y);
                closestPoint.x = math.select(-halfSize.x, halfSize.x, unitLS.x > 0);
            }
            else
            {
                closestPoint.x = math.clamp(unitLS.x, -halfSize.x, halfSize.x);
                closestPoint.y = math.select(-halfSize.y, halfSize.y, unitLS.y > 0);
            }
        }

        var isIntersected = isInside || math.lengthsq(closestPoint - unitLS) < math.pow(unitRadius, 2);
        if (isIntersected)
        {
            if (math.abs(unitLS.x - halfSize.x) < 1e-3f)
                closestPoint.x += math.select(1e-3f, -1e-3f, unitLS.x > closestPoint.x);
            else if (math.abs(unitLS.x + halfSize.x) < 1e-3f)
                closestPoint.x += math.select(1e-3f, -1e-3f, unitLS.x > closestPoint.x);
            if (math.abs(unitLS.y - halfSize.y) < 1e-3f)
                closestPoint.y += math.select(1e-3f, -1e-3f, unitLS.y > closestPoint.y);
            else if (math.abs(unitLS.y + halfSize.y) < 1e-3f)
                closestPoint.y += math.select(1e-3f, -1e-3f, unitLS.y > closestPoint.y);

            float2 closestPointWS = center + closestPoint.x * right + closestPoint.y * up;
            float2 dirLS = unitLS - closestPoint;
            float2 dirWS = math.normalizesafe(dirLS.x * right + dirLS.y * up);
            position = math.select(closestPointWS + (unitRadius + 1e-3f) * dirWS, closestPointWS - (unitRadius + 1e-3f) * dirWS, isInside);
        }

        return position;
    }
}
