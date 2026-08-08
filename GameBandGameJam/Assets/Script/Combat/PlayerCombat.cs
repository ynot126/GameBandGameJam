#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerController playerController = null!;
    [SerializeField] CombatHitbox hitbox = null!;
    [SerializeField] PlayerAnimationController animationController = null!;
    [SerializeField] DamageNumberVisual? damageNumberPrefab;
    [SerializeField] Rigidbody? body;
    [SerializeField] Collider[] playerColliders = Array.Empty<Collider>();

    [Header("Masks")]
    [SerializeField] LayerMask hitMask;
    [SerializeField] LayerMask wallMask;

    [Header("Combo")]
    [SerializeField] float comboResetWindow = 0.5f;
    [SerializeField] float simultaneousInputWindow = 0.05f;
    [SerializeField] int consecutiveInvalidThreshold = 2;
    [SerializeField] float chaseOffset = 1.5f;
    [SerializeField] KeyCode lightKey = KeyCode.J;
    [SerializeField] KeyCode heavyKey = KeyCode.K;
    [SerializeField] KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] ComboRecipe[] recipes = Array.Empty<ComboRecipe>();
    [SerializeField] AttackDefinition[] attacks = Array.Empty<AttackDefinition>();

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
    readonly Dictionary<AttackId, AttackDefinition> attackMap = new();

    CancellationTokenSource? attackCts;
    int attackGeneration;
    bool isBusy;
    bool isAttackPlaying;
    bool isKinematicMotionActive;
    bool hardBreakRecovery;
    AttackInputType? queuedFollowUp;
    float queuedFollowUpTime;
    float attackLockoutUntil;
    int consecutiveInvalidCount;
    AttackDefinition? activeAttack;

    float? pendingLightTime;
    float? pendingHeavyTime;
    float? pendingDashTime;

    public bool IsBusy => isBusy;
    public bool IsKinematicMotionActive => isKinematicMotionActive;
    public bool IsAttackLockedOut => Time.time < attackLockoutUntil;
    public bool IsHardBreakRecovery => hardBreakRecovery;

    public event Action<AttackId>? OnAttackExecuted;
    public event Action? OnCombatReset;
    public event Action? OnHardComboBreak;

    public void Initialize(
        PlayerController controller,
        CombatHitbox combatHitbox,
        PlayerAnimationController animController,
        DamageNumberVisual? numberPrefab,
        LayerMask entityMask)
    {
        playerController = controller;
        hitbox = combatHitbox;
        animationController = animController;
        damageNumberPrefab = numberPrefab;
        hitMask = entityMask;
        if (wallMask.value == 0)
        {
            wallMask = ~entityMask;
        }

        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
        }

        if (playerColliders == null || playerColliders.Length == 0)
        {
            playerColliders = GetComponentsInChildren<Collider>();
        }

        EnsureDefaultConfig();

        inputBuffer.Initialize(comboResetWindow);
        comboEvaluator.Initialize(recipes);
        attackDash.Initialize(transform);
        chaseTeleport.Initialize(transform, playerColliders, chaseOffset);
        sequencer.Initialize(chaseTeleport);
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

        Transform? indicator = null;
        var attackDetector = GetComponentInChildren<PlayerAttackDetector>(true);
        if (attackDetector != null)
        {
            indicator = attackDetector.SphereIndicator;
        }

        hitbox.Initialize(transform, hitMask, indicator);

        var animator = GetComponentInChildren<Animator>();
        EnsureAnimationEventReceiver(animator);

        attackMap.Clear();
        for (var i = 0; i < attacks.Length; i++)
        {
            attackMap[attacks[i].attackId] = attacks[i];
        }

        hitbox.OnHitConfirmed += HandleHitConfirmed;
        sequencer.OnSequenceReset += HandleSequenceReset;
        playerController.SetMovementEnabled(true);
    }

    void EnsureAnimationEventReceiver(Animator? animator)
    {
        if (animator == null)
        {
            return;
        }

        var receiver = animator.GetComponent<CombatAnimationEventReceiver>();
        if (receiver == null)
        {
            receiver = animator.gameObject.AddComponent<CombatAnimationEventReceiver>();
        }

        receiver.Initialize(this, hitbox);
    }

    void OnDestroy()
    {
        if (hitbox != null)
        {
            hitbox.OnHitConfirmed -= HandleHitConfirmed;
        }

        sequencer.OnSequenceReset -= HandleSequenceReset;
        attackCts?.Cancel();
        attackCts?.Dispose();
    }

    void Update()
    {
        CaptureRawAttackPresses();
        ExpireStaleQueuedFollowUp();
        CheckComboTimeout();
        PollAttackInput();
    }

    void CaptureRawAttackPresses()
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

    void PollAttackInput()
    {
        if (!TryResolvePendingAttackInput(out var input))
        {
            return;
        }

        HandleResolvedAttackInput(input);
    }

    void HandleResolvedAttackInput(AttackInputType input)
    {
        if (!CanAcceptCombatInput(input))
        {
            if (input != AttackInputType.D)
            {
                RegisterInvalidInput();
            }

            return;
        }

        // Dodge always commits immediately — highest global priority.
        if (input == AttackInputType.D)
        {
            TryCommitComboInput(input);
            return;
        }

        // During startup/active frames: queue follow-up for the cancel window.
        if (isAttackPlaying && isBusy)
        {
            QueueFollowUp(input);
            return;
        }

        // Cancel window open, or idle between strings.
        if (isAttackPlaying && !isBusy)
        {
            TryCommitComboInput(input);
            return;
        }

        if (isBusy || !inputBuffer.IsOpen)
        {
            return;
        }

        TryCommitComboInput(input);
    }

    bool CanAcceptCombatInput(AttackInputType input)
    {
        if (input == AttackInputType.D)
        {
            return true;
        }

        if (hardBreakRecovery || IsAttackLockedOut)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Simultaneous window: D immediate; H commits immediately; L waits up to the window for a possible H.
    /// Priority: D &gt; H &gt; L. At most one attack input is returned per resolve.
    /// </summary>
    bool TryResolvePendingAttackInput(out AttackInputType input)
    {
        input = default;
        var now = Time.time;

        if (pendingDashTime.HasValue)
        {
            input = AttackInputType.D;
            ClearPendingAttackInputs();
            return true;
        }

        if (pendingHeavyTime.HasValue)
        {
            // H outranks L; consume any pending light so only one attack resolves.
            input = AttackInputType.H;
            ClearPendingAttackInputs();
            return true;
        }

        if (pendingLightTime.HasValue)
        {
            if (now - pendingLightTime.Value < simultaneousInputWindow)
            {
                // Wait briefly so a near-simultaneous H can take priority.
                return false;
            }

            input = AttackInputType.L;
            pendingLightTime = null;
            return true;
        }

        return false;
    }

    void ClearPendingAttackInputs()
    {
        pendingDashTime = null;
        pendingHeavyTime = null;
        pendingLightTime = null;
    }

    void QueueFollowUp(AttackInputType input)
    {
        if (!CanAcceptCombatInput(input))
        {
            if (input != AttackInputType.D)
            {
                RegisterInvalidInput();
            }

            return;
        }

        queuedFollowUp = input;
        queuedFollowUpTime = Time.time;
    }

    void ExpireStaleQueuedFollowUp()
    {
        if (!queuedFollowUp.HasValue)
        {
            return;
        }

        if (Time.time - queuedFollowUpTime > comboResetWindow)
        {
            queuedFollowUp = null;
            return;
        }

        if (!CanAcceptCombatInput(queuedFollowUp.Value))
        {
            queuedFollowUp = null;
        }
    }

    void CheckComboTimeout()
    {
        // Continuation is only available once the cancel window opens (not during startup/active).
        if (isBusy)
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
            if (input != AttackInputType.D)
            {
                RegisterInvalidInput();
            }

            return;
        }

        if (!TryResolveRecipe(input, out var attackId))
        {
            RegisterInvalidInput();
            return;
        }

        if (input == AttackInputType.D)
        {
            autoLock.Clear();
        }
        else if (!PrepareAutoLockForAttack(attackId))
        {
            return;
        }

        consecutiveInvalidCount = 0;
        queuedFollowUp = null;
        ExecuteAttack(attackId).Forget();
    }

    /// <summary>
    /// Runs L/H auto-lock at attack commit. Returns false when a persisted combo target
    /// was lost (range + LOS) and the combo was broken.
    /// </summary>
    bool PrepareAutoLockForAttack(AttackId attackId)
    {
        if (!attackMap.TryGetValue(attackId, out var definition))
        {
            return true;
        }

        // Mobility dash never acquires; lock was cleared on D input.
        if (definition.useMoveInputDirection || definition.skipHitbox)
        {
            return true;
        }

        var reach = CombatAutoLock.ComputeAttackReach(definition);
        if (!autoLock.TrySelectOrRetain(reach, out var lostPersistedTarget) && lostPersistedTarget)
        {
            TriggerHardComboBreak();
            return false;
        }

        return true;
    }

    bool TryResolveRecipe(AttackInputType input, out AttackId attackId)
    {
        attackId = AttackId.None;
        var time = Time.time;

        // Dash always starts a fresh mobility branch.
        if (input == AttackInputType.D)
        {
            inputBuffer.ReplaceWith(input, time);
            return comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out attackId);
        }

        inputBuffer.Append(input, time);
        if (comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out attackId))
        {
            return true;
        }

        // No recipe for the extended sequence — intentional branch restart from this input.
        inputBuffer.ReplaceWith(input, time);
        return comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out attackId);
    }

    bool TryConsumeQueuedFollowUp()
    {
        if (!queuedFollowUp.HasValue)
        {
            return false;
        }

        if (Time.time - queuedFollowUpTime > comboResetWindow)
        {
            queuedFollowUp = null;
            return false;
        }

        var input = queuedFollowUp.Value;
        queuedFollowUp = null;
        if (!CanAcceptCombatInput(input))
        {
            return false;
        }

        TryCommitComboInput(input);
        return true;
    }

    void RegisterInvalidInput()
    {
        if (hardBreakRecovery)
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
        if (hardBreakRecovery)
        {
            consecutiveInvalidCount = 0;
            queuedFollowUp = null;
            inputBuffer.Clear();
            inputBuffer.SetOpen(false);
            autoLock.Clear();
            return;
        }

        var shouldNotify = inputBuffer.Count > 0 || consecutiveInvalidCount > 0 || isAttackPlaying;
        consecutiveInvalidCount = 0;
        queuedFollowUp = null;
        inputBuffer.Clear();
        inputBuffer.SetOpen(false);
        autoLock.Clear();

        if (isAttackPlaying)
        {
            hardBreakRecovery = true;
        }

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

    /// <summary>
    /// Opens the cancel window so the next combo input can be accepted.
    /// Called by Animation Event <c>OpenCancelWindow</c> (frame-accurate),
    /// <see cref="AttackStateBehavior"/>, or automatically after active frames.
    /// Closes the hit window so a cancel-into next attack cannot leave the hitbox active.
    /// </summary>
    public void OpenCancelWindow()
    {
        if (!isAttackPlaying || !isBusy)
        {
            return;
        }

        isBusy = false;
        hitbox.DisableHitbox();

        if (hardBreakRecovery)
        {
            inputBuffer.SetOpen(false);
            return;
        }

        inputBuffer.SetOpen(true);
        inputBuffer.Touch(Time.time);
        TryConsumeQueuedFollowUp();
    }

    /// <summary>
    /// Immediately cancels all active combat state when the player is hit.
    /// </summary>
    public void InterruptFromHit()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = null;
        attackGeneration++;

        sequencer.CancelPendingChase();
        hardBreakRecovery = false;
        consecutiveInvalidCount = 0;
        ClearAttackLockout();
        ClearPendingAttackInputs();
        ResetToNavigation();
    }

    async UniTaskVoid ExecuteAttack(AttackId attackId)
    {
        if (!attackMap.TryGetValue(attackId, out var definition))
        {
            Debug.LogWarning($"No AttackDefinition for {attackId}");
            return;
        }

        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = new CancellationTokenSource();
        var token = attackCts.Token;
        var generation = ++attackGeneration;

        hardBreakRecovery = false;
        isBusy = true;
        isAttackPlaying = true;
        activeAttack = definition;
        inputBuffer.SetOpen(false);
        playerController.SetMovementEnabled(false);

        // Committing any attack clears a prior lockout; finishers re-arm immediately.
        ClearAttackLockout();
        BeginAttackLockout(definition.attackLockoutDuration);

        var alignToLock = false;
        Vector3 dashDirection;
        if (definition.useMoveInputDirection)
        {
            dashDirection = ResolveMoveInputDirection();
            FaceDirection(dashDirection);
        }
        else if (autoLock.TryGetLockDirection(out var lockDirection))
        {
            dashDirection = lockDirection;
            alignToLock = true;
        }
        else
        {
            FaceAimDirection();
            dashDirection = Flatten(transform.forward);
            if (dashDirection.sqrMagnitude <= 0.0001f)
            {
                dashDirection = Vector3.forward;
            }
        }

        if (definition.triggersChaseSequence)
        {
            sequencer.ArmChaseOnNextLaunch();
        }

        animationController.PlayAttack(attackId);
        OnAttackExecuted?.Invoke(attackId);

        // Always clear any prior swing: cancel-into (especially skipHitbox dash) skips the
        // superseded attack's finally EndSwing, which would otherwise leave the hitbox live.
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
            isKinematicMotionActive = true;
            if (alignToLock)
            {
                await UniTask.WhenAll(
                    attackDash.DashAsync(dashDirection, definition.dashDistance, definition.dashDuration, token),
                    AlignToLockDuringStartupAsync(definition.dashDuration, token));
            }
            else
            {
                await attackDash.DashAsync(dashDirection, definition.dashDistance, definition.dashDuration, token);
            }

            isKinematicMotionActive = false;

            if (!definition.skipHitbox)
            {
                // Remaining startup: keep aligning until active frames begin, then freeze facing.
                if (alignToLock && definition.hitboxEnableDelay > 0f)
                {
                    await UniTask.WhenAll(
                        UniTask.Delay(TimeSpan.FromSeconds(definition.hitboxEnableDelay), cancellationToken: token),
                        AlignToLockDuringStartupAsync(definition.hitboxEnableDelay, token));
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(definition.hitboxEnableDelay), cancellationToken: token);
                }

                hitbox.EnableHitbox();
                await UniTask.Delay(TimeSpan.FromSeconds(definition.hitboxActiveDuration), cancellationToken: token);
                hitbox.DisableHitbox();
            }

            // Keep the string alive so cancel-into next attack can fire.
            OpenCancelWindow();
            await UniTask.Delay(TimeSpan.FromSeconds(recoveryHold), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            isKinematicMotionActive = false;
            // Superseded by a cancel-into next attack, or teardown — do not reset owner state.
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

    async UniTaskVoid AwaitFinisherFallback(CancellationToken token, int generation)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.75f), cancellationToken: token);
            if (generation != attackGeneration)
            {
                return;
            }

            if (isAttackPlaying)
            {
                sequencer.CancelPendingChase();
                ResetToNavigation();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    void HandleHitConfirmed(IDamageable damageable, HitPayload payload, Vector3 hitDirection)
    {
        if (damageNumberPrefab != null)
        {
            var spawnPos = damageable is Component component
                ? component.transform.position + Vector3.up
                : transform.position + transform.forward;
            var damageNumber = Instantiate(damageNumberPrefab);
            damageNumber.Initialize(spawnPos, payload.Damage);
        }

        if (payload.KnockbackType != KnockbackType.KnockbackToDistance)
        {
            return;
        }

        if (damageable is not ICombatLaunchable launchable)
        {
            return;
        }

        // Retain launched target through chase recovery for optional follow-ups.
        if (damageable is ICombatTarget launchTarget)
        {
            autoLock.ForceLock(launchTarget);
        }

        ResolveLaunch(launchable, hitDirection, payload.LaunchDistance).Forget();
    }

    async UniTaskVoid ResolveLaunch(ICombatLaunchable launchable, Vector3 hitDirection, float launchDistance)
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = new CancellationTokenSource();
        var token = attackCts.Token;
        var generation = ++attackGeneration;

        // Launch cancels ExecuteAttack's recovery wait — keep the committing attack's
        // recovery/lockout so a successful finisher cannot skip end-lag.
        var recoveryHold = activeAttack != null && activeAttack.recoveryHoldDuration > 0f
            ? activeAttack.recoveryHoldDuration
            : 0.55f;
        var lockoutDuration = activeAttack?.attackLockoutDuration ?? 0f;

        isBusy = true;
        isAttackPlaying = true;
        inputBuffer.SetOpen(false);
        queuedFollowUp = null;
        playerController.SetMovementEnabled(false);
        BeginAttackLockout(lockoutDuration);

        try
        {
            isKinematicMotionActive = true;
            await sequencer.HandleLaunchAndChaseAsync(launchable, hitDirection, launchDistance, token);
            isKinematicMotionActive = false;

            // Forced recovery: do not open cancel — L/H must wait out finisher end-lag.
            if (recoveryHold > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(recoveryHold), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            isKinematicMotionActive = false;
        }
        finally
        {
            isKinematicMotionActive = false;
            if (generation == attackGeneration)
            {
                ResetToNavigation();
            }
        }
    }

    void HandleSequenceReset()
    {
        OnCombatReset?.Invoke();
    }

    void ResetToNavigation()
    {
        isBusy = false;
        isAttackPlaying = false;
        isKinematicMotionActive = false;
        hardBreakRecovery = false;
        consecutiveInvalidCount = 0;
        queuedFollowUp = null;
        activeAttack = null;
        autoLock.Clear();
        inputBuffer.Clear();
        hitbox.EndSwing();
        animationController.ResetAttack();
        inputBuffer.SetOpen(true);
        playerController.SetMovementEnabled(true);
        if (body != null)
        {
            body.isKinematic = true;
        }
    }

    async UniTask AlignToLockDuringStartupAsync(float duration, CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            if (autoLock.TryGetLockDirection(out var instantDir))
            {
                FaceDirection(instantDir);
            }

            return;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!autoLock.TryGetLockDirection(out var lockDirection))
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(lockDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lockRotationSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    void FaceAimDirection()
    {
        if (TryGetHeldMoveInput(out var held))
        {
            FaceDirection(held);
            return;
        }

        if (TryGetFloorAim(out var aimPoint))
        {
            var dir = aimPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                FaceDirection(dir);
            }
        }
    }

    void FaceDirection(Vector3 worldDirection)
    {
        var flat = Flatten(worldDirection);
        if (flat.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
    }

    Vector3 ResolveMoveInputDirection()
    {
        if (TryGetHeldMoveInput(out var move) && move.sqrMagnitude > 0.001f)
        {
            return move;
        }

        return Flatten(transform.forward);
    }

    bool TryGetHeldMoveInput(out Vector3 planarDirection)
    {
        planarDirection = default;
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
        {
            return false;
        }

        planarDirection = GetCameraPlanarDirection(horizontal, vertical);
        return planarDirection.sqrMagnitude > 0.001f;
    }

    static Vector3 GetCameraPlanarDirection(float horizontal, float vertical)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            return Flatten(new Vector3(horizontal, 0f, vertical));
        }

        var forward = Flatten(cam.transform.forward);
        var right = Flatten(cam.transform.right);
        return Flatten(forward * vertical + right * horizontal);
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    bool TryGetFloorAim(out Vector3 point)
    {
        point = default;
        var cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var floor = new Plane(Vector3.up, 0f);
        if (!floor.Raycast(ray, out var enter))
        {
            return false;
        }

        point = ray.GetPoint(enter);
        return true;
    }

    void EnsureDefaultConfig()
    {
        if (recipes == null || recipes.Length == 0)
        {
            recipes = CreateDefaultRecipes();
        }
        else
        {
            EnsureDashRecipePresent();
        }

        if (attacks == null || attacks.Length == 0)
        {
            attacks = CreateDefaultAttacks();
            return;
        }

        EnsureDashAttackPresent();
    }

    void EnsureDashRecipePresent()
    {
        for (var i = 0; i < recipes.Length; i++)
        {
            if (recipes[i].attackId == AttackId.Dash)
            {
                return;
            }
        }

        var expanded = new ComboRecipe[recipes.Length + 1];
        Array.Copy(recipes, expanded, recipes.Length);
        expanded[recipes.Length] = CreateRecipe("Dash", AttackId.Dash, AttackInputType.D);
        recipes = expanded;
    }

    void EnsureDashAttackPresent()
    {
        for (var i = 0; i < attacks.Length; i++)
        {
            if (attacks[i].attackId == AttackId.Dash)
            {
                return;
            }
        }

        var expanded = new AttackDefinition[attacks.Length + 1];
        Array.Copy(attacks, expanded, attacks.Length);
        expanded[attacks.Length] = CreateDashAttackDefinition();
        attacks = expanded;
    }

    static ComboRecipe[] CreateDefaultRecipes()
    {
        return new[]
        {
            CreateRecipe("Light", AttackId.Light, Repeat(AttackInputType.L, 1)),
            CreateRecipe("Light1", AttackId.Light1, Repeat(AttackInputType.L, 2)),
            CreateRecipe("Light2", AttackId.Light2, Repeat(AttackInputType.L, 3)),
            CreateRecipe("Light3", AttackId.Light3, Repeat(AttackInputType.L, 4)),
            CreateRecipe("Light4", AttackId.Light4, Repeat(AttackInputType.L, 5)),
            CreateRecipe("Light5", AttackId.Light5, Repeat(AttackInputType.L, 6)),
            CreateRecipe("Light Finisher", AttackId.LightFinisher, Repeat(AttackInputType.L, 7)),

            CreateRecipe("Heavy", AttackId.Heavy, Repeat(AttackInputType.H, 1)),
            CreateRecipe("Heavy1", AttackId.Heavy1, Repeat(AttackInputType.H, 2)),
            CreateRecipe("Heavy2", AttackId.Heavy2, Repeat(AttackInputType.H, 3)),
            CreateRecipe("Heavy3", AttackId.Heavy3, Repeat(AttackInputType.H, 4)),
            CreateRecipe("Heavy4", AttackId.Heavy4, Repeat(AttackInputType.H, 5)),
            CreateRecipe("Heavy5", AttackId.Heavy5, Repeat(AttackInputType.H, 6)),
            CreateRecipe("Heavy Finisher", AttackId.HeavyFinisher, Repeat(AttackInputType.H, 7)),

            CreateRecipe("Dash", AttackId.Dash, AttackInputType.D),

            CreateRecipe("Dash Light", AttackId.DashLight, DashThen(AttackInputType.L, 1)),
            CreateRecipe("Dash Light1", AttackId.DashLight1, DashThen(AttackInputType.L, 2)),
            CreateRecipe("Dash Light2", AttackId.DashLight2, DashThen(AttackInputType.L, 3)),
            CreateRecipe("Dash Light Finisher", AttackId.DashLightFinisher, DashThen(AttackInputType.L, 4)),

            CreateRecipe("Dash Heavy", AttackId.DashHeavy, DashThen(AttackInputType.H, 1)),
            CreateRecipe("Dash Heavy1", AttackId.DashHeavy1, DashThen(AttackInputType.H, 2)),
            CreateRecipe("Dash Heavy2", AttackId.DashHeavy2, DashThen(AttackInputType.H, 3)),
            CreateRecipe("Dash Heavy Finisher", AttackId.DashHeavyFinisher, DashThen(AttackInputType.H, 4))
        };
    }

    static AttackDefinition[] CreateDefaultAttacks()
    {
        const int lightBaseDamage = 10;
        const int heavyBaseDamage = 18;

        return new[]
        {
            CreateScaledAttack(AttackId.Light, lightBaseDamage, step: 0, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.Light1, lightBaseDamage, step: 1, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.Light2, lightBaseDamage, step: 2, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.Light3, lightBaseDamage, step: 3, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.Light4, lightBaseDamage, step: 4, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.Light5, lightBaseDamage, step: 5, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.LightFinisher, lightBaseDamage, step: 6, isHeavy: false, isFinisher: true),

            CreateScaledAttack(AttackId.Heavy, heavyBaseDamage, step: 0, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.Heavy1, heavyBaseDamage, step: 1, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.Heavy2, heavyBaseDamage, step: 2, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.Heavy3, heavyBaseDamage, step: 3, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.Heavy4, heavyBaseDamage, step: 4, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.Heavy5, heavyBaseDamage, step: 5, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.HeavyFinisher, heavyBaseDamage, step: 6, isHeavy: true, isFinisher: true),

            CreateDashAttackDefinition(),

            CreateScaledAttack(AttackId.DashLight, lightBaseDamage, step: 0, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.DashLight1, lightBaseDamage, step: 1, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.DashLight2, lightBaseDamage, step: 2, isHeavy: false, isFinisher: false),
            CreateScaledAttack(AttackId.DashLightFinisher, lightBaseDamage, step: 3, isHeavy: false, isFinisher: true),

            CreateScaledAttack(AttackId.DashHeavy, heavyBaseDamage, step: 0, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.DashHeavy1, heavyBaseDamage, step: 1, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.DashHeavy2, heavyBaseDamage, step: 2, isHeavy: true, isFinisher: false),
            CreateScaledAttack(AttackId.DashHeavyFinisher, heavyBaseDamage, step: 3, isHeavy: true, isFinisher: true)
        };
    }

    static ComboRecipe CreateRecipe(string name, AttackId attackId, params AttackInputType[] sequence)
    {
        return new ComboRecipe
        {
            name = name,
            sequence = sequence,
            attackId = attackId
        };
    }

    static AttackInputType[] Repeat(AttackInputType input, int count)
    {
        var sequence = new AttackInputType[count];
        for (var i = 0; i < count; i++)
        {
            sequence[i] = input;
        }

        return sequence;
    }

    static AttackInputType[] DashThen(AttackInputType input, int count)
    {
        var sequence = new AttackInputType[count + 1];
        sequence[0] = AttackInputType.D;
        for (var i = 0; i < count; i++)
        {
            sequence[i + 1] = input;
        }

        return sequence;
    }

    static AttackDefinition CreateScaledAttack(
        AttackId attackId,
        int baseDamage,
        int step,
        bool isHeavy,
        bool isFinisher)
    {
        var damageMultiplier = 1f + step * 0.05f;
        var damage = Mathf.Max(1, (int)Math.Round(baseDamage * damageMultiplier, MidpointRounding.AwayFromZero));
        var hitStun = (isHeavy ? 0.2f : 0.12f) + step * 0.02f;
        var launch = (isHeavy ? 1.5f : 0.75f) + step * 0.15f;

        return new AttackDefinition
        {
            attackId = attackId,
            dashDistance = isHeavy ? 2f : 0.5f,
            dashDuration = isHeavy ? 0.1f : 0.07f,
            hitboxEnableDelay = isHeavy ? 0.06f : 0.04f,
            hitboxActiveDuration = isHeavy ? 0.12f : 0.1f,
            recoveryHoldDuration = isFinisher ? 1f : 0.55f,
            attackLockoutDuration = isFinisher ? 0.65f : 0f,
            hitboxRadius = isFinisher ? 0.9f : isHeavy ? 0.75f : 0.6f,
            hitboxLocalOffset = isFinisher
                ? new Vector3(0f, 0.8f, 1.1f)
                : isHeavy
                    ? new Vector3(0f, 0.8f, 0.3f)
                    : new Vector3(0f, 0.8f, 0.7f),
            triggersChaseSequence = isFinisher,
            payload = new HitPayloadData
            {
                damage = damage,
                hitStunDuration = isFinisher ? Mathf.Max(hitStun, 0.35f) : hitStun,
                knockbackType = isFinisher ? KnockbackType.KnockbackToDistance : KnockbackType.Standard,
                launchDistance = isFinisher ? 6f : launch
            }
        };
    }

    static AttackDefinition CreateDashAttackDefinition()
    {
        return new AttackDefinition
        {
            attackId = AttackId.Dash,
            dashDistance = 3f,
            dashDuration = 0.12f,
            hitboxEnableDelay = 0f,
            hitboxActiveDuration = 0f,
            recoveryHoldDuration = 0.35f,
            triggersChaseSequence = false,
            useMoveInputDirection = true,
            skipHitbox = true,
            payload = new HitPayloadData
            {
                damage = 0,
                hitStunDuration = 0f,
                knockbackType = KnockbackType.Standard,
                launchDistance = 0f
            }
        };
    }
}
