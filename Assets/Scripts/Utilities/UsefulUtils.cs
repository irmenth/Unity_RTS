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

    public static bool HasCollideWithRectObstacle(Rectangle rect, Vector3 unitWS, float unitRadius, out Vector2 negImpactDir)
    {
        var size = V3ToV2(rect.transform.localScale);
        var unitWS2D = V3ToV2(unitWS);
        var localPos = V3ToV2(rect.transform.worldToLocalMatrix.MultiplyPoint(unitWS));

        var isInside = localPos.x < size.x / 2 && localPos.x > -size.x / 2 && localPos.y < size.y / 2 && localPos.y > -size.y / 2;

        var closePoint = Vector2.zero;
        closePoint.x = Mathf.Clamp(localPos.x, -size.x / 2, size.x / 2);
        closePoint.y = Mathf.Clamp(localPos.y, -size.y / 2, size.y / 2);
        var closePointWS = V3ToV2(rect.transform.localToWorldMatrix.MultiplyPoint(V2ToV3(closePoint)));

        var isCollided = Vector2.SqrMagnitude(closePoint - localPos) < Mathf.Pow(unitRadius, 2) | isInside;
        if (isCollided)
        {
            if (isInside)
                negImpactDir = (closePointWS - unitWS2D).normalized;
            else
                negImpactDir = (unitWS2D - closePointWS).normalized;
            Debug.Log(negImpactDir);
        }
        else
        {
            negImpactDir = Vector2.zero;
        }
        return isCollided;
    }
}
