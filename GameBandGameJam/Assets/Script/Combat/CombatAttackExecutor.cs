#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Owns attack lifecycle, cancel/generation, hit reaction, launch, and navigation reset.
/// </summary>
public sealed class CombatAttackExecutor
{
    Transform owner = null!;
    UnityEngine.Object logContext = null!;
    PlayerController playerController = null!;
    CombatHitbox hitbox = null!;
    PlayerAnimationController animationController = null!;
    AttackDash attackDash = null!;
    CombatAutoLock autoLock = null!;
    CombatSequencer sequencer = null!;
    CombatPhaseMachine phaseMachine = null!;
    CombatFacing facing = null!;
    InputBuffer inputBuffer = null!;
    DamageNumberVisual? damageNumberPrefab;
    ImpactFrameParticleConfig? impactFrameParticleConfig;

    float cameraShakeDuration;
    float cameraShakeStrength;
    float cameraShakeFrequency;

    CancellationTokenSource? attackCts;
    int attackGeneration;
    AttackDefinition? activeAttack;
    bool cameraShakeArmed;

    Action<AttackId>? onAttackExecuted;
    Action<AttackDefinition>? onApplyComboInputWindow;
    Action? onTryConsumeQueuedFollowUp;
    Action? onClearQueuedFollowUp;
    Action? onResetNavigationExtras;
    Action<float>? onBeginAttackLockout;
    Action? onClearAttackLockout;
    Action? onRestoreDefaultComboWindow;

    public AttackDefinition? ActiveAttack => activeAttack;

    public void Initialize(
        Transform ownerTransform,
        UnityEngine.Object unityLogContext,
        PlayerController controller,
        CombatHitbox combatHitbox,
        PlayerAnimationController animController,
        AttackDash dash,
        CombatAutoLock lockSystem,
        CombatSequencer combatSequencer,
        CombatPhaseMachine phases,
        CombatFacing combatFacing,
        InputBuffer buffer,
        DamageNumberVisual? numberPrefab,
        ImpactFrameParticleConfig? particleConfig,
        float shakeDuration,
        float shakeStrength,
        float shakeFrequency,
        Action<AttackId>? attackExecuted,
        Action<AttackDefinition>? applyComboInputWindow,
        Action? tryConsumeQueuedFollowUp,
        Action? clearQueuedFollowUp,
        Action? resetNavigationExtras,
        Action<float>? beginAttackLockout,
        Action? clearAttackLockout,
        Action? restoreDefaultComboWindow)
    {
        owner = ownerTransform;
        logContext = unityLogContext;
        playerController = controller;
        hitbox = combatHitbox;
        animationController = animController;
        attackDash = dash;
        autoLock = lockSystem;
        sequencer = combatSequencer;
        phaseMachine = phases;
        facing = combatFacing;
        inputBuffer = buffer;
        damageNumberPrefab = numberPrefab;
        impactFrameParticleConfig = particleConfig;
        cameraShakeDuration = shakeDuration;
        cameraShakeStrength = shakeStrength;
        cameraShakeFrequency = shakeFrequency;
        onAttackExecuted = attackExecuted;
        onApplyComboInputWindow = applyComboInputWindow;
        onTryConsumeQueuedFollowUp = tryConsumeQueuedFollowUp;
        onClearQueuedFollowUp = clearQueuedFollowUp;
        onResetNavigationExtras = resetNavigationExtras;
        onBeginAttackLockout = beginAttackLockout;
        onClearAttackLockout = clearAttackLockout;
        onRestoreDefaultComboWindow = restoreDefaultComboWindow;
    }

    public void SetDamageNumberPrefab(DamageNumberVisual? numberPrefab)
    {
        damageNumberPrefab = numberPrefab;
    }

    public void ArmCameraShakeOnHit()
    {
        cameraShakeArmed = true;
    }

    public void ClearCameraShakeOnHit()
    {
        cameraShakeArmed = false;
    }

    public void EndCombatCameraZoom()
    {
        GameCameraController.Instance.EndZoom();
    }

    public void PlayImpactFrameParticle()
    {
        if (impactFrameParticleConfig == null)
        {
            return;
        }

        impactFrameParticleConfig.PlayRandomAt(hitbox.GetWorldCenter(), hitbox.GetOwnerRotation());
    }

    public void OpenCancelWindow()
    {
        var result = phaseMachine.TryOpenCancel();
        if (result == CancelOpenResult.Ignored)
        {
            return;
        }

        hitbox.DisableHitbox();
        ClearCameraShakeOnHit();
        EndCombatCameraZoom();

        if (result == CancelOpenResult.HardBreakAcknowledged)
        {
            inputBuffer.SetOpen(false);
            return;
        }

        if (activeAttack != null)
        {
            onApplyComboInputWindow?.Invoke(activeAttack);
        }

        inputBuffer.SetOpen(true);
        inputBuffer.Touch(Time.time);
        onTryConsumeQueuedFollowUp?.Invoke();
    }

    public void InterruptFromHit()
    {
        CancelAttackToken();
        attackGeneration++;

        sequencer.CancelPendingChase();
        onClearAttackLockout?.Invoke();
        ResetToNavigation();
    }

    public void Dispose()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = null;
    }

    public void HandleHitConfirmed(IHitable hitable, HitPayload payload, Vector3 hitDirection)
    {
        if (cameraShakeArmed)
        {
            GameCameraController.Instance.Shake(
                cameraShakeDuration,
                cameraShakeStrength,
                cameraShakeFrequency);
        }

        if (damageNumberPrefab != null)
        {
            var spawnPos = hitable is Component component
                ? component.transform.position + Vector3.up
                : owner.position + owner.forward;
            var damageNumber = UnityEngine.Object.Instantiate(damageNumberPrefab);
            damageNumber.Initialize(spawnPos, payload.Damage);
        }

        if (payload.KnockbackType != KnockbackType.KnockbackToDistance)
        {
            return;
        }

        if (hitable is ICombatTarget launchTarget)
        {
            autoLock.ForceLock(launchTarget);
        }

        ResolveLaunch(hitable, hitDirection, payload.LaunchDistance).Forget();
    }

    public async UniTaskVoid ExecuteAttack(AttackDefinition definition, AttackId attackId)
    {
        RestartAttackToken(out var token, out var generation);

        phaseMachine.EnterStartup();
        activeAttack = definition;
        inputBuffer.SetOpen(false);
        playerController.SetMovementEnabled(false);

        onClearAttackLockout?.Invoke();
        onBeginAttackLockout?.Invoke(definition.attackLockoutDuration);

        var alignToLock = false;
        Vector3 dashDirection;
        if (definition.useMoveInputDirection)
        {
            dashDirection = facing.ResolveMoveInputDirection();
            facing.FaceDirection(dashDirection);
        }
        else if (autoLock.TryGetLockDirection(out var lockDirection))
        {
            dashDirection = lockDirection;
            alignToLock = true;
        }
        else
        {
            facing.FaceAimDirection();
            dashDirection = CombatFacing.Flatten(owner.forward);
            if (dashDirection.sqrMagnitude <= 0.0001f)
            {
                dashDirection = Vector3.forward;
            }
        }

        if (definition.triggersChaseSequence)
        {
            if (definition.payload.knockbackType == KnockbackType.Standard)
            {
                Debug.LogError(
                    $"{attackId}: triggersChaseSequence requires KnockbackToDistance, but knockbackType is Standard. Chase will not be armed.",
                    logContext);
            }
            else
            {
                sequencer.ArmChaseOnNextLaunch();
            }
        }

        animationController.PlayAttack(attackId);
        onAttackExecuted?.Invoke(attackId);

        ClearCameraShakeOnHit();
        hitbox.EndSwing();
        if (!definition.skipHitbox)
        {
            hitbox.ConfigureShape(definition.hitboxRadius, definition.hitboxLocalOffset);
            hitbox.BeginSwing(definition.payload.ToPayload());
        }

        var recoveryHold = definition.recoveryHoldDuration > 0f
            ? definition.recoveryHoldDuration
            : 0.55f;

        try
        {
            if (alignToLock)
            {
                await UniTask.WhenAll(
                    attackDash.DashAsync(dashDirection, definition.dashDistance, definition.dashDuration, token),
                    facing.AlignToLockDuringStartupAsync(autoLock, definition.dashDuration, token));
            }
            else
            {
                await attackDash.DashAsync(dashDirection, definition.dashDistance, definition.dashDuration, token);
            }

            phaseMachine.TryEnterActiveAfterStartup();

            // Hitbox enable/disable and cancel open are driven by Animation Events
            // (or AttackStateBehavior). Mobility-only moves open cancel after the dash.
            if (definition.skipHitbox)
            {
                OpenCancelWindow();
            }
            else
            {
                await WaitForCancelWindowAsync(attackId, token);
            }

            phaseMachine.TryEnterRecovery();

            await UniTask.Delay(TimeSpan.FromSeconds(recoveryHold), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            if (generation != attackGeneration)
            {
                return;
            }

            sequencer.CancelPendingChase();
        }
        finally
        {
            if (generation == attackGeneration)
            {
                hitbox.EndSwing();

                if (!definition.triggersChaseSequence)
                {
                    ResetToNavigation();
                }
                else
                {
                    AwaitFinisherFallback(token, generation).Forget();
                }
            }
        }
    }

    async UniTask WaitForCancelWindowAsync(AttackId attackId, CancellationToken token)
    {
        var clipDuration = animationController.GetClipDuration(attackId);
        var safetyTimeout = clipDuration > 0f ? clipDuration : 1.5f;
        var gate = phaseMachine.CaptureCancelGate();

        var cancelOpened = UniTask.WaitUntil(
            () => phaseMachine.HasCancelOpenedSince(gate),
            cancellationToken: token);
        var timedOut = UniTask.Delay(TimeSpan.FromSeconds(safetyTimeout), cancellationToken: token);
        await UniTask.WhenAny(cancelOpened, timedOut);

        if (phaseMachine.NeedsCancelFallback(gate))
        {
            Debug.LogWarning(
                $"No OpenCancelWindow Animation Event for {attackId} within {safetyTimeout:0.##}s — opening cancel as fallback.",
                logContext);
            OpenCancelWindow();
        }
    }

    async UniTaskVoid AwaitFinisherFallback(CancellationToken token, int generation)
    {
        phaseMachine.TryEnterChaseAwait();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), cancellationToken: token);
            if (generation != attackGeneration)
            {
                return;
            }

            if (phaseMachine.IsAttackPlaying)
            {
                sequencer.CancelPendingChase();
                ResetToNavigation();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async UniTaskVoid ResolveLaunch(IHitable hitable, Vector3 hitDirection, float launchDistance)
    {
        RestartAttackToken(out var token, out var generation);

        var recoveryHold = activeAttack != null && activeAttack.recoveryHoldDuration > 0f
            ? activeAttack.recoveryHoldDuration
            : 0.55f;
        var lockoutDuration = activeAttack?.attackLockoutDuration ?? 0f;

        phaseMachine.EnterLaunch();
        inputBuffer.SetOpen(false);
        onClearQueuedFollowUp?.Invoke();
        playerController.SetMovementEnabled(false);
        onBeginAttackLockout?.Invoke(lockoutDuration);

        try
        {
            await sequencer.HandleLaunchAndChaseAsync(hitable, hitDirection, launchDistance, token);

            if (recoveryHold > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(recoveryHold), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (generation == attackGeneration)
            {
                ResetToNavigation();
            }
        }
    }

    public void ResetToNavigation()
    {
        phaseMachine.EnterIdle();
        onResetNavigationExtras?.Invoke();
        onClearQueuedFollowUp?.Invoke();
        activeAttack = null;
        ClearCameraShakeOnHit();
        EndCombatCameraZoom();
        autoLock.Clear();
        inputBuffer.Clear();
        hitbox.EndSwing();
        animationController.ResetAttack();
        inputBuffer.SetOpen(true);
        onRestoreDefaultComboWindow?.Invoke();
        playerController.SetMovementEnabled(true);
    }

    void RestartAttackToken(out CancellationToken token, out int generation)
    {
        CancelAttackToken();
        attackCts = new CancellationTokenSource();
        token = attackCts.Token;
        generation = ++attackGeneration;
    }

    void CancelAttackToken()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = null;
    }
}
