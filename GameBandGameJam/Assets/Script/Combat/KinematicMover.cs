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
}
