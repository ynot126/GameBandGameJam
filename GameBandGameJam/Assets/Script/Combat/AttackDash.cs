#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class AttackDash
{
    Rigidbody body = null!;

    public void Initialize(Rigidbody ownerBody)
    {
        body = ownerBody;
    }

    public UniTask DashForwardAsync(float distance, float duration, CancellationToken cancellationToken)
    {
        return DashAsync(GetOwnerForward(), distance, duration, cancellationToken);
    }

    public UniTask DashAsync(Vector3 worldDirection, float distance, float duration, CancellationToken cancellationToken)
    {
        var direction = Flatten(worldDirection);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = GetOwnerForward();
        }

        return KinematicMover.MoveByAsync(body, direction * distance, duration, cancellationToken);
    }

    Vector3 GetOwnerForward()
    {
        return Flatten(body.transform.forward);
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }
}
