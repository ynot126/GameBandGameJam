#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class CombatSequencer
{
    ChaseTeleport chaseTeleport = null!;
    bool awaitingChase;

    public event Action? OnSequenceReset;

    public void Initialize(ChaseTeleport teleport)
    {
        chaseTeleport = teleport;
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
        
        var targetTransform = ResolveTransform(hitable);
        if (targetTransform != null)
        {
            chaseTeleport.TeleportBehind(targetTransform);
        }

        OnSequenceReset?.Invoke();
    }

    public void CancelPendingChase()
    {
        awaitingChase = false;
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
