#nullable enable
using UnityEngine;

public static class MapBoundary
{
    static readonly Vector3[] CardinalDirections =
    {
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    public static bool IsNearBoundary(Vector3 position, float proximity, LayerMask wallMask)
    {
        if (proximity <= 0f || wallMask.value == 0)
        {
            return false;
        }

        var origin = position + Vector3.up * 0.5f;
        for (var i = 0; i < CardinalDirections.Length; i++)
        {
            if (Physics.Raycast(
                    origin,
                    CardinalDirections[i],
                    out var hit,
                    proximity,
                    wallMask,
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        return false;
    }

    public static Vector3 ResolveOutwardDirection(Vector3 position)
    {
        var flat = position;
        flat.y = 0f;
        if (flat.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return flat.normalized;
    }
}
