#nullable enable

public enum CombatAttackInputRouting
{
    Ignore,
    QueueFollowUp,
    Commit,
    CommitIfBufferOpen,
}

public enum CancelOpenResult
{
    Ignored,
    HardBreakAcknowledged,
    EnteredCancelWindow,
}

/// <summary>
/// Owns combat phase, phase-driven input policy, and cancel-window signaling.
/// </summary>
public sealed class CombatPhaseMachine
{
    int cancelOpenGeneration;

    public CombatPhase Current { get; private set; } = CombatPhase.Idle;

    public bool IsIdle => Current == CombatPhase.Idle;
    public bool IsHardBreak => Current == CombatPhase.HardBreak;
    public bool IsAttackPlaying => Current != CombatPhase.Idle;
    public bool IsBusy => Current is CombatPhase.Startup or CombatPhase.Active or CombatPhase.Launch;
    public bool BlocksComboTimeout => IsBusy;

    public void EnterIdle() => Set(CombatPhase.Idle);

    public void EnterStartup() => Set(CombatPhase.Startup);

    public void EnterLaunch() => Set(CombatPhase.Launch);

    public bool TryEnterActiveAfterStartup() => TryTransition(CombatPhase.Startup, CombatPhase.Active);

    public bool TryEnterRecovery() => TryTransition(CombatPhase.CancelWindow, CombatPhase.Recovery);

    public bool TryEnterChaseAwait() =>
        TryTransitionFromAny(CombatPhase.ChaseAwait, CombatPhase.CancelWindow, CombatPhase.Recovery);

    public bool TryEnterHardBreak()
    {
        if (!IsAttackPlaying || IsHardBreak)
        {
            return false;
        }

        Set(CombatPhase.HardBreak);
        return true;
    }

    public CancelOpenResult TryOpenCancel()
    {
        if (IsHardBreak)
        {
            SignalCancelOpened();
            return CancelOpenResult.HardBreakAcknowledged;
        }

        // Launch must also cancel-open so post-chase chains can commit (same as old isBusy).
        if (!TryTransitionFromAny(
                CombatPhase.CancelWindow,
                CombatPhase.Startup,
                CombatPhase.Active,
                CombatPhase.Launch))
        {
            return CancelOpenResult.Ignored;
        }

        SignalCancelOpened();
        return CancelOpenResult.EnteredCancelWindow;
    }

    public int CaptureCancelGate() => cancelOpenGeneration;

    public bool HasCancelOpenedSince(int gate) =>
        cancelOpenGeneration != gate || IsIdle;

    public bool NeedsCancelFallback(int gate) =>
        cancelOpenGeneration == gate
        && Current is CombatPhase.Startup or CombatPhase.Active or CombatPhase.HardBreak;

    public CombatAttackInputRouting ResolveAttackInputRouting()
    {
        return Current switch
        {
            CombatPhase.Startup or CombatPhase.Active or CombatPhase.Launch
                => CombatAttackInputRouting.QueueFollowUp,
            CombatPhase.CancelWindow or CombatPhase.Recovery or CombatPhase.ChaseAwait
                => CombatAttackInputRouting.Commit,
            CombatPhase.HardBreak => CombatAttackInputRouting.Ignore,
            CombatPhase.Idle => CombatAttackInputRouting.CommitIfBufferOpen,
            _ => CombatAttackInputRouting.CommitIfBufferOpen,
        };
    }

    void SignalCancelOpened() => cancelOpenGeneration++;

    void Set(CombatPhase next) => Current = next;

    bool TryTransition(CombatPhase expectedCurrent, CombatPhase next)
    {
        if (Current != expectedCurrent)
        {
            return false;
        }

        Current = next;
        return true;
    }

    bool TryTransitionFromAny(CombatPhase next, params CombatPhase[] fromPhases)
    {
        for (var i = 0; i < fromPhases.Length; i++)
        {
            if (Current == fromPhases[i])
            {
                Current = next;
                return true;
            }
        }

        return false;
    }
}
