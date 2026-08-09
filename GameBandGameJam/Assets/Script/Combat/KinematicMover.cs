#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class KinematicMover
{
    public static async UniTask MoveByAsync(
        Rigidbody body,
        Vector3 worldDelta,
        float duration,
        CancellationToken cancellationToken)
    {
        if (duration <= 0f || worldDelta.sqrMagnitude <= 0f)
        {
            body.MovePosition(body.position + worldDelta);
            return;
        }

        var start = body.position;
        var end = start + worldDelta;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.fixedDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            body.MovePosition(Vector3.Lerp(start, end, t));
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken);
        }

        body.MovePosition(end);
    }

    public static async UniTask MoveToAsync(
        Rigidbody body,
        Vector3 worldEnd,
        float duration,
        CancellationToken cancellationToken)
    {
        var delta = worldEnd - body.position;
        await MoveByAsync(body, delta, duration, cancellationToken);
    }

    /// <summary>
    /// Moves along a horizontal lerp with a parabolic Y offset (projectile / ball arc).
    /// </summary>
    public static async UniTask MoveAlongArcAsync(
        Rigidbody body,
        Vector3 worldEnd,
        float duration,
        float arcHeight,
        CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            body.MovePosition(worldEnd);
            return;
        }

        var start = body.position;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.fixedDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var grounded = Vector3.Lerp(start, worldEnd, t);
            var peak = arcHeight * 4f * t * (1f - t);
            grounded.y += peak;
            body.MovePosition(grounded);
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken);
        }

        body.MovePosition(worldEnd);
    }
}
