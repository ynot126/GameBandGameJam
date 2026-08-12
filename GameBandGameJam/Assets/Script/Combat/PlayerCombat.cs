#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerController playerController = null!;
    [SerializeField] CombatHitbox hitbox = null!;
    [SerializeField] PlayerAnimationController animationController = null!;
    [SerializeField] CombatAnimationEventReceiver animationEventReceiver = null!;
    [SerializeField] PlayerAttackDetector? attackDetector;
    [SerializeField] DamageNumberVisual? damageNumberPrefab;
    [SerializeField] Collider[] playerColliders = Array.Empty<Collider>();

    [Header("Masks")]
    [SerializeField] LayerMask hitMask;
    [SerializeField] LayerMask wallMask;

    [Header("Chase")]
    [SerializeField] float chaseOffset = 1.5f;
    [SerializeField, Min(0.01f)] float chaseDuration = 0.35f;
    [SerializeField, Min(0f)] float chaseArcHeight = 1.75f;
    [SerializeField, Min(0f)] float chaseDelay = 0.5f;

    [Header("Input")]
    [SerializeField] KeyCode lightKey = KeyCode.J;
    [SerializeField] KeyCode heavyKey = KeyCode.K;
    [SerializeField] KeyCode dashKey = KeyCode.LeftShift;

    [Header("Camera")]
    [SerializeField, Min(0f)] float cameraShakeDuration = 0.25f;
    [SerializeField, Min(0f)] float cameraShakeStrength = 0.3f;
    [SerializeField, Min(0f)] float cameraShakeFrequency = 25f;

    [Header("VFX")]
    [SerializeField] ImpactFrameParticleConfig? impactFrameParticleConfig;

    [Header("Auto Lock")]
    [SerializeField] float lockConeDegrees = 90f;
    [SerializeField] float lockSnapMargin = 0.75f;
    [SerializeField] float lockPersistenceRange = 4f;
    [SerializeField] float lockRotationSpeed = 720f;
    [SerializeField] float lockScoreAlignmentWeight = 1f;
    [SerializeField] float lockScoreDistanceWeight = 1f;
    [SerializeField] float lockScoreThreatWeight = 0.35f;
    [SerializeField] float lockScoreLowHealthWeight = 0.2f;

    readonly InputBuffer inputBuffer = new();
    readonly ComboEvaluator comboEvaluator = new();
    readonly AttackDash attackDash = new();
    readonly ChaseTeleport chaseTeleport = new();
    readonly CombatSequencer sequencer = new();
    readonly CombatAutoLock autoLock = new();
    readonly CombatPhaseMachine phaseMachine = new();
    readonly CombatFacing facing = new();
    readonly CombatAttackInput attackInput = new();
    readonly CombatAttackExecutor attackExecutor = new();
    PlayerCombatConfig? combatConfig;
    PlayerStamina? stamina;
    float activeComboInputWindow = 0.5f;
    int consecutiveInvalidThreshold = 2;
    float attackLockoutUntil;
    int consecutiveInvalidCount;

    public CombatPhase Phase => phaseMachine.Current;
    public bool IsBusy => phaseMachine.IsBusy;
    public bool IsAttackLockedOut => Time.time < attackLockoutUntil;
    public bool IsHardBreakRecovery => phaseMachine.IsHardBreak;

    public event Action<ComboType>? OnAttackExecuted;
    public event Action? OnCombatReset;
    public event Action? OnHardComboBreak;

    public void Initialize(
        PlayerCombatConfig config,
        PlayerController controller,
        Rigidbody ownerBody,
        CombatHitbox combatHitbox,
        PlayerAnimationController animController,
        DamageNumberVisual? numberPrefab,
        LayerMask entityMask,
        PlayerStamina playerStamina)
    {
        if (!config.HasAuthoredCombos())
        {
            Debug.LogError("PlayerCombat.Initialize: PlayerCombatConfig has no authored combos.", config);
            return;
        }

        combatConfig = config;
        stamina = playerStamina;
        playerController = controller;
        hitbox = combatHitbox;
        animationController = animController;
        damageNumberPrefab = numberPrefab;
        hitMask = entityMask;
        if (wallMask.value == 0)
        {
            wallMask = ~entityMask;
        }

        activeComboInputWindow = config.defaultComboResetWindow;
        consecutiveInvalidThreshold = config.consecutiveInvalidThreshold;

        inputBuffer.Initialize(activeComboInputWindow);
        comboEvaluator.Initialize(config.combos);
        attackDash.Initialize(ownerBody);
        chaseTeleport.Initialize(ownerBody, playerColliders, chaseOffset, chaseDuration, chaseArcHeight);
        sequencer.Initialize(chaseTeleport, chaseDelay);
        autoLock.Initialize(
            transform,
            hitMask,
            wallMask,
            lockConeDegrees,
            lockSnapMargin,
            lockPersistenceRange,
            lockScoreAlignmentWeight,
            lockScoreDistanceWeight,
            lockScoreThreatWeight,
            lockScoreLowHealthWeight);
        facing.Initialize(transform, playerController, lockRotationSpeed);
        attackInput.Initialize(
            lightKey,
            heavyKey,
            dashKey,
            config.simultaneousInputWindow,
            activeComboInputWindow);
        attackExecutor.Initialize(
            transform,
            this,
            playerController,
            hitbox,
            animationController,
            attackDash,
            autoLock,
            sequencer,
            phaseMachine,
            facing,
            inputBuffer,
            damageNumberPrefab,
            impactFrameParticleConfig,
            cameraShakeDuration,
            cameraShakeStrength,
            cameraShakeFrequency,
            attackId => OnAttackExecuted?.Invoke(attackId),
            ApplyComboInputWindow,
            () => TryConsumeQueuedFollowUp(),
            attackInput.ClearQueuedFollowUp,
            () => consecutiveInvalidCount = 0,
            BeginAttackLockout,
            ClearAttackLockout,
            RestoreDefaultComboWindow);

        var indicator = attackDetector != null ? attackDetector.SphereIndicator : null;
        hitbox.Initialize(transform, hitMask, indicator);
        animationEventReceiver.Initialize(this, hitbox);

        hitbox.OnHitConfirmed += attackExecutor.HandleHitConfirmed;
        sequencer.OnSequenceReset += HandleSequenceReset;
        playerController.SetMovementEnabled(true);
    }

    void OnDestroy()
    {
        if (hitbox != null)
        {
            hitbox.OnHitConfirmed -= attackExecutor.HandleHitConfirmed;
        }

        sequencer.OnSequenceReset -= HandleSequenceReset;
        attackExecutor.Dispose();
    }

    void Update()
    {
        attackInput.CaptureRawAttackPresses();
        attackInput.ExpireStaleQueuedFollowUp(CanAcceptCombatInput);
        CheckComboTimeout();
        PollAttackInput();
    }

    void PollAttackInput()
    {
        if (!attackInput.TryResolvePendingAttackInput(out var input))
        {
            return;
        }

        HandleResolvedAttackInput(input);
    }

    void HandleResolvedAttackInput(AttackInputType input)
    {
        if (!CanAcceptCombatInput(input))
        {
            if (input != AttackInputType.Dash)
            {
                RegisterInvalidInput();
            }

            return;
        }

        if (input == AttackInputType.Dash)
        {
            TryCommitComboInput(input);
            return;
        }

        switch (phaseMachine.ResolveAttackInputRouting())
        {
            case CombatAttackInputRouting.QueueFollowUp:
                QueueFollowUp(input);
                return;
            case CombatAttackInputRouting.Commit:
                TryCommitComboInput(input);
                return;
            case CombatAttackInputRouting.CommitIfBufferOpen:
                if (!inputBuffer.IsOpen)
                {
                    return;
                }

                TryCommitComboInput(input);
                return;
            case CombatAttackInputRouting.Ignore:
            default:
                return;
        }
    }

    bool CanAcceptCombatInput(AttackInputType input)
    {
        if (stamina != null && !stamina.CanAfford(input))
        {
            return false;
        }

        if (input == AttackInputType.Dash)
        {
            return true;
        }

        if (phaseMachine.IsHardBreak || IsAttackLockedOut)
        {
            return false;
        }

        return true;
    }

    void QueueFollowUp(AttackInputType input)
    {
        if (!CanAcceptCombatInput(input))
        {
            if (input != AttackInputType.Dash)
            {
                RegisterInvalidInput();
            }

            return;
        }

        attackInput.QueueFollowUp(input);
    }

    void CheckComboTimeout()
    {
        if (phaseMachine.BlocksComboTimeout)
        {
            return;
        }

        if (!inputBuffer.HasTimedOut(Time.time))
        {
            return;
        }

        TriggerHardComboBreak();
    }

    void TryCommitComboInput(AttackInputType input)
    {
        if (!CanAcceptCombatInput(input))
        {
            return;
        }

        if (!TryResolveRecipe(input, out var attackId))
        {
            RegisterInvalidInput();
            return;
        }

        if (input == AttackInputType.Dash)
        {
            autoLock.Clear();
        }
        else if (!PrepareAutoLockForAttack(attackId))
        {
            return;
        }

        consecutiveInvalidCount = 0;
        attackInput.ClearQueuedFollowUp();

        if (combatConfig == null || !combatConfig.combos.TryGetValue(attackId, out var comboData) || comboData == null)
        {
            Debug.LogWarning($"No ComboData for {attackId}");
            return;
        }

        stamina?.NotifySpend(input);
        attackExecutor.ExecuteAttack(comboData, attackId).Forget();
    }

    bool PrepareAutoLockForAttack(ComboType comboType)
    {
        if (combatConfig == null || !combatConfig.combos.TryGetValue(comboType, out var comboData) || comboData == null)
        {
            return true;
        }

        if (comboData.useMoveInputDirection || comboData.skipHitbox)
        {
            return true;
        }

        var reach = CombatAutoLock.ComputeAttackReach(comboData);
        if (!autoLock.TrySelectOrRetain(reach, out var lostPersistedTarget) && lostPersistedTarget)
        {
            TriggerHardComboBreak();
            return false;
        }

        return true;
    }

    bool TryResolveRecipe(AttackInputType input, out ComboType comboType)
    {
        comboType = ComboType.None;
        var time = Time.time;

        if (input == AttackInputType.Dash)
        {
            inputBuffer.ReplaceWith(input, time);
            return comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out comboType);
        }

        inputBuffer.Append(input, time);
        if (comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out comboType))
        {
            return true;
        }

        inputBuffer.ReplaceWith(input, time);
        return comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out comboType);
    }

    bool TryConsumeQueuedFollowUp()
    {
        if (!attackInput.TryTakeQueuedFollowUp(out var input))
        {
            return false;
        }

        if (!CanAcceptCombatInput(input))
        {
            return false;
        }

        TryCommitComboInput(input);
        return true;
    }

    void RegisterInvalidInput()
    {
        if (phaseMachine.IsHardBreak)
        {
            return;
        }

        consecutiveInvalidCount++;
        if (consecutiveInvalidCount >= consecutiveInvalidThreshold)
        {
            TriggerHardComboBreak();
        }
    }

    void TriggerHardComboBreak()
    {
        if (phaseMachine.IsHardBreak)
        {
            consecutiveInvalidCount = 0;
            attackInput.ClearQueuedFollowUp();
            inputBuffer.Clear();
            inputBuffer.SetOpen(false);
            autoLock.Clear();
            return;
        }

        var shouldNotify = inputBuffer.Count > 0
            || consecutiveInvalidCount > 0
            || phaseMachine.IsAttackPlaying;
        consecutiveInvalidCount = 0;
        attackInput.ClearQueuedFollowUp();
        inputBuffer.Clear();
        inputBuffer.SetOpen(false);
        autoLock.Clear();
        phaseMachine.TryEnterHardBreak();

        if (!shouldNotify)
        {
            return;
        }

        OnHardComboBreak?.Invoke();
        SfxManager.Instance.Play(SfxType.HardComboBreak);
    }

    void BeginAttackLockout(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        attackLockoutUntil = Mathf.Max(attackLockoutUntil, Time.time + duration);
    }

    void ClearAttackLockout()
    {
        attackLockoutUntil = 0f;
    }

    float ResolveComboInputWindow(ComboData comboData)
    {
        if (comboData.comboInputWindow > 0f)
        {
            return comboData.comboInputWindow;
        }

        return combatConfig != null ? combatConfig.defaultComboResetWindow : 0.5f;
    }

    void ApplyComboInputWindow(ComboData comboData)
    {
        activeComboInputWindow = ResolveComboInputWindow(comboData);
        attackInput.SetComboInputWindow(activeComboInputWindow);
        inputBuffer.SetResetWindow(activeComboInputWindow);
    }

    void RestoreDefaultComboWindow()
    {
        if (combatConfig == null)
        {
            return;
        }

        activeComboInputWindow = combatConfig.defaultComboResetWindow;
        attackInput.SetComboInputWindow(activeComboInputWindow);
        inputBuffer.SetResetWindow(activeComboInputWindow);
    }

    public void ArmCameraShakeOnHit() => attackExecutor.ArmCameraShakeOnHit();

    public void ClearCameraShakeOnHit() => attackExecutor.ClearCameraShakeOnHit();

    public void EndCombatCameraZoom() => attackExecutor.EndCombatCameraZoom();

    public void PlayImpactFrameParticle() => attackExecutor.PlayImpactFrameParticle();

    public void OpenCancelWindow() => attackExecutor.OpenCancelWindow();

    public void InterruptFromHit()
    {
        consecutiveInvalidCount = 0;
        attackInput.ClearPending();
        attackExecutor.InterruptFromHit();
    }

    public void SetAttackSpeedMultiplier(float multiplier)
    {
        attackExecutor.SetAttackSpeedMultiplier(multiplier);
        animationController.SetPlaybackSpeed(multiplier);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        attackExecutor.SetDamageMultiplier(multiplier);
    }

    public void SetDashDistanceMultiplier(float multiplier)
    {
        attackExecutor.SetDashDistanceMultiplier(multiplier);
    }

    void HandleSequenceReset()
    {
        OnCombatReset?.Invoke();
    }
}








