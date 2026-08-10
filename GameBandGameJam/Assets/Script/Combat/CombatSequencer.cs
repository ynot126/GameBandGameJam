#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class CombatSequencer
{
    ChaseTeleport chaseTeleport = null!;
    float chaseDelaySeconds = 0.5f;
    bool awaitingChase;

    public event Action? OnSequenceReset;

    public void Initialize(ChaseTeleport teleport, float chaseDelay = 0.5f)
    {
        chaseTeleport = teleport;
        chaseDelaySeconds = chaseDelay;
        awaitingChase = false;
    }

    public void ArmChaseOnNextLaunch()
    {
        awaitingChase = true;
    }

    public async UniTask HandleLaunchAndChaseAsync(
        IHitable hitable,
        Vector3 hitDirection,
        float launchDistance,
        CancellationToken cancellationToken)
    {
        var launchTask = hitable.TryKnockback(
            KnockbackType.KnockbackToDistance,
            launchDistance,
            hitDirection,
            cancellationToken);

        if (!awaitingChase)
        {
            await launchTask;
            return;
        }

        awaitingChase = false;
        await launchTask;
        cancellationToken.ThrowIfCancellationRequested();

        if (chaseDelaySeconds > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(chaseDelaySeconds),
                cancellationToken: cancellationToken);
        }

        var targetTransform = ResolveTransform(hitable);
        if (targetTransform != null)
        {
            await chaseTeleport.ChaseBehindAsync(targetTransform, cancellationToken);
        }

        OnSequenceReset?.Invoke();
    }

    public void CancelPendingChase()
    {
        awaitingChase = false;
        chaseTeleport.SnapToGround();
    }

    public void SnapChaseToGround()
    {
        chaseTeleport.SnapToGround();
    }

    static Transform? ResolveTransform(IHitable hitable)
    {
        if (hitable is ICombatTarget target)
        {
            return target.Transform;
        }

        if (hitable is Component component)
        {
            return component.transform;
        }

        return null;
    }
}
