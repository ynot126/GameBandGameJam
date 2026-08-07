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
        ICombatLaunchable launchable,
        Vector3 hitDirection,
        float launchDistance,
        CancellationToken cancellationToken)
    {
        if (!awaitingChase)
        {
            await launchable.LaunchAsync(hitDirection, launchDistance, cancellationToken);
            return;
        }

        awaitingChase = false;
        await launchable.LaunchAsync(hitDirection, launchDistance, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (launchable.Transform != null)
        {
            chaseTeleport.TeleportBehind(launchable.Transform);
        }

        OnSequenceReset?.Invoke();
    }

    public void CancelPendingChase()
    {
        awaitingChase = false;
    }
}

public interface ICombatLaunchable
{
    Transform Transform { get; }
    UniTask LaunchAsync(Vector3 direction, float distance, CancellationToken cancellationToken);
}
