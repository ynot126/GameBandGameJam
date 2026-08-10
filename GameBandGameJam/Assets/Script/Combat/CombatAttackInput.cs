#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Owns raw attack press capture, simultaneous-input resolve, and follow-up queue storage.
/// Acceptance gating stays with the orchestrator.
/// </summary>
public sealed class CombatAttackInput
{
    KeyCode lightKey = KeyCode.J;
    KeyCode heavyKey = KeyCode.K;
    KeyCode dashKey = KeyCode.LeftShift;
    float simultaneousInputWindow = 0.05f;
    float activeComboInputWindow = 0.5f;

    float? pendingLightTime;
    float? pendingHeavyTime;
    float? pendingDashTime;
    AttackInputType? queuedFollowUp;
    float queuedFollowUpTime;

    public void Initialize(
        KeyCode light,
        KeyCode heavy,
        KeyCode dash,
        float simultaneousWindow,
        float comboInputWindow)
    {
        lightKey = light;
        heavyKey = heavy;
        dashKey = dash;
        simultaneousInputWindow = simultaneousWindow;
        activeComboInputWindow = comboInputWindow;
        ClearAll();
    }

    public void SetComboInputWindow(float window)
    {
        activeComboInputWindow = window;
    }

    public void CaptureRawAttackPresses()
    {
        if (Input.GetKeyDown(dashKey))
        {
            pendingDashTime = Time.time;
        }

        if (Input.GetKeyDown(heavyKey) || Input.GetMouseButtonDown(1))
        {
            pendingHeavyTime = Time.time;
        }

        if (Input.GetKeyDown(lightKey) || Input.GetMouseButtonDown(0))
        {
            pendingLightTime = Time.time;
        }
    }

    public bool TryResolvePendingAttackInput(out AttackInputType input)
    {
        input = default;
        var now = Time.time;

        if (pendingDashTime.HasValue)
        {
            input = AttackInputType.Dash;
            ClearPending();
            return true;
        }

        if (pendingHeavyTime.HasValue)
        {
            input = AttackInputType.Heavy;
            ClearPending();
            return true;
        }

        if (pendingLightTime.HasValue)
        {
            if (now - pendingLightTime.Value < simultaneousInputWindow)
            {
                return false;
            }

            input = AttackInputType.Light;
            pendingLightTime = null;
            return true;
        }

        return false;
    }

    public void ClearPending()
    {
        pendingDashTime = null;
        pendingHeavyTime = null;
        pendingLightTime = null;
    }

    public void QueueFollowUp(AttackInputType input)
    {
        queuedFollowUp = input;
        queuedFollowUpTime = Time.time;
    }

    public void ExpireStaleQueuedFollowUp(Func<AttackInputType, bool> canAccept)
    {
        if (!queuedFollowUp.HasValue)
        {
            return;
        }

        if (Time.time - queuedFollowUpTime > activeComboInputWindow)
        {
            queuedFollowUp = null;
            return;
        }

        if (!canAccept(queuedFollowUp.Value))
        {
            queuedFollowUp = null;
        }
    }

    public bool TryTakeQueuedFollowUp(out AttackInputType input)
    {
        input = default;
        if (!queuedFollowUp.HasValue)
        {
            return false;
        }

        if (Time.time - queuedFollowUpTime > activeComboInputWindow)
        {
            queuedFollowUp = null;
            return false;
        }

        input = queuedFollowUp.Value;
        queuedFollowUp = null;
        return true;
    }

    public void ClearQueuedFollowUp()
    {
        queuedFollowUp = null;
    }

    public void ClearAll()
    {
        ClearPending();
        ClearQueuedFollowUp();
    }
}
