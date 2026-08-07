#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class LaunchMotor
{
    Transform target = null!;
    LayerMask wallMask;
    float launchDuration = 0.18f;
    float skinWidth = 0.2f;

    public event Action? OnLaunchCompleted;
    public bool IsLaunching { get; private set; }

    public void Initialize(Transform launchTarget, LayerMask walls, float duration = 0.18f)
    {
        target = launchTarget;
        wallMask = walls;
        launchDuration = duration;
    }

    public async UniTask LaunchAsync(Vector3 launchDirection, float distance, CancellationToken cancellationToken)
    {
        if (IsLaunching)
        {
            return;
        }

        IsLaunching = true;

        var direction = launchDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        var travel = ClampDistance(target.position, direction, distance);
        var destination = target.position + direction * travel;

        try
        {
            await KinematicMover.MoveToAsync(target, destination, launchDuration, cancellationToken);
        }
        finally
        {
            IsLaunching = false;
            OnLaunchCompleted?.Invoke();
        }
    }

    float ClampDistance(Vector3 origin, Vector3 direction, float desiredDistance)
    {
        if (Physics.Raycast(origin + Vector3.up * 0.5f, direction, out var hit, desiredDistance + skinWidth, wallMask, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(0f, hit.distance - skinWidth);
        }

        return desiredDistance;
    }
}
