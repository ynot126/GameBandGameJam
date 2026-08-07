#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class KinematicMover
{
    public static async UniTask MoveByAsync(
        Transform target,
        Vector3 worldDelta,
        float duration,
        CancellationToken cancellationToken)
    {
        if (duration <= 0f || worldDelta.sqrMagnitude <= 0f)
        {
            target.position += worldDelta;
            return;
        }

        var start = target.position;
        var end = start + worldDelta;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        target.position = end;
    }

    public static async UniTask MoveToAsync(
        Transform target,
        Vector3 worldEnd,
        float duration,
        CancellationToken cancellationToken)
    {
        var delta = worldEnd - target.position;
        await MoveByAsync(target, delta, duration, cancellationToken);
    }
}
