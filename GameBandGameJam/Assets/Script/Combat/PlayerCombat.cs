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
    [SerializeField] PlayerAnimatorDriver animatorDriver = null!;
    [SerializeField] DamageNumberVisual? damageNumberPrefab;
    [SerializeField] Rigidbody? body;
    [SerializeField] Collider[] playerColliders = Array.Empty<Collider>();

    [Header("Masks")]
    [SerializeField] LayerMask hitMask;
    [SerializeField] LayerMask wallMask;

    [Header("Combo")]
    [SerializeField] float comboResetWindow = 0.45f;
    [SerializeField] float prefixCommitDelay = 0.16f;
    [SerializeField] float chaseOffset = 1.5f;
    [SerializeField] KeyCode lightKey = KeyCode.J;
    [SerializeField] KeyCode heavyKey = KeyCode.K;
    [SerializeField] KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] ComboRecipe[] recipes = Array.Empty<ComboRecipe>();
    [SerializeField] AttackDefinition[] attacks = Array.Empty<AttackDefinition>();

    readonly InputBuffer inputBuffer = new();
    readonly ComboEvaluator comboEvaluator = new();
    readonly AttackDash attackDash = new();
    readonly ChaseTeleport chaseTeleport = new();
    readonly CombatSequencer sequencer = new();
    readonly Dictionary<AttackId, AttackDefinition> attackMap = new();

    CancellationTokenSource? attackCts;
    int attackGeneration;
    bool isBusy;
    bool isAttackPlaying;
    bool isKinematicMotionActive;
    float pendingCommitAt;

    public bool IsBusy => isBusy;
    public bool IsKinematicMotionActive => isKinematicMotionActive;

    public event Action<AttackId>? OnAttackExecuted;
    public event Action? OnCombatReset;

    public void Initialize(
        PlayerController controller,
        CombatHitbox combatHitbox,
        PlayerAnimatorDriver driver,
        DamageNumberVisual? numberPrefab,
        LayerMask entityMask)
    {
        playerController = controller;
        hitbox = combatHitbox;
        animatorDriver = driver;
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

        Transform? indicator = null;
        var attackDetector = GetComponentInChildren<PlayerAttackDetector>(true);
        if (attackDetector != null)
        {
            indicator = attackDetector.SphereIndicator;
        }

        hitbox.Initialize(transform, hitMask, indicator);

        var animator = GetComponentInChildren<Animator>();
        animatorDriver.Initialize(animator);
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
        PollAttackInput();

        // Ambiguous prefix timer: force-commit the short recipe when the delay elapses.
        if (pendingCommitAt > 0f && Time.time >= pendingCommitAt)
        {
            pendingCommitAt = 0f;
            ForceCommitBufferedRecipe();
        }

        TryCommitTimedOutBuffer();
    }

    void PollAttackInput()
    {
        if (isBusy || !inputBuffer.IsOpen)
        {
            return;
        }

        if (!TryReadAttackInput(out var input))
        {
            return;
        }

        if (!inputBuffer.TryRegister(input, Time.time))
        {
            return;
        }

        // Unambiguous match (not a prefix of a longer recipe) → execute immediately.
        if (comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: false, out var attackId))
        {
            pendingCommitAt = 0f;
            inputBuffer.Clear();
            ExecuteAttack(attackId).Forget();
            return;
        }

        // Exact match that is also a longer-recipe prefix (e.g. H vs HHLH) → wait.
        if (comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out _))
        {
            pendingCommitAt = Time.time + prefixCommitDelay;
            return;
        }

        pendingCommitAt = 0f;
    }

    bool TryReadAttackInput(out AttackInputType input)
    {
        input = default;
        var pressedDash = Input.GetKeyDown(dashKey);
        var pressedHeavy = Input.GetKeyDown(heavyKey) || Input.GetMouseButtonDown(1);
        var pressedLight = Input.GetKeyDown(lightKey) || Input.GetMouseButtonDown(0);

        if (pressedDash)
        {
            input = AttackInputType.D;
            return true;
        }

        if (pressedHeavy)
        {
            input = AttackInputType.H;
            return true;
        }

        if (pressedLight)
        {
            input = AttackInputType.L;
            return true;
        }

        return false;
    }

    void ForceCommitBufferedRecipe()
    {
        if (isBusy || !inputBuffer.IsOpen)
        {
            return;
        }

        if (!comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out var attackId))
        {
            return;
        }

        inputBuffer.Clear();
        ExecuteAttack(attackId).Forget();
    }

    void TryCommitTimedOutBuffer()
    {
        if (isBusy || !inputBuffer.HasTimedOut(Time.time))
        {
            return;
        }

        pendingCommitAt = 0f;
        if (comboEvaluator.TryResolve(inputBuffer.Sequence, forceCommit: true, out var attackId))
        {
            inputBuffer.Clear();
            ExecuteAttack(attackId).Forget();
            return;
        }

        inputBuffer.Clear();
    }

    /// <summary>
    /// Opens the cancel window so the next combo input can be accepted.
    /// Called by Animation Event <c>OpenCancelWindow</c> (frame-accurate) or
    /// <see cref="AttackStateBehavior"/> (normalized-time fallback).
    /// Does not stop dashes, launches, or re-enable locomotion.
    /// Closes the hit window so a cancel-into next attack cannot leave the hitbox active
    /// (anim <c>DisableHitbox</c> may never fire once the clip is interrupted).
    /// </summary>
    public void OpenCancelWindow()
    {
        if (!isAttackPlaying || !isBusy)
        {
            return;
        }
        isBusy = false;
        inputBuffer.SetOpen(true);
        // Seal before timed EnableHitbox fallback can run again after this frame.
        hitbox.DisableHitbox();
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

        isBusy = true;
        isAttackPlaying = true;
        pendingCommitAt = 0f;
        inputBuffer.SetOpen(false);
        playerController.SetMovementEnabled(false);

        Vector3 dashDirection;
        if (definition.useMoveInputDirection)
        {
            dashDirection = ResolveMoveInputDirection();
            FaceDirection(dashDirection);
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

        animatorDriver.PlayAttack(attackId);
        OnAttackExecuted?.Invoke(attackId);

        // Always clear any prior swing: cancel-into (especially skipHitbox dash) skips the
        // superseded attack's finally EndSwing, which would otherwise leave the hitbox live.
        hitbox.EndSwing();
        if (!definition.skipHitbox)
        {
            hitbox.ConfigureShape(definition.hitboxRadius, definition.hitboxLocalOffset);
            hitbox.BeginSwing(definition.payload.ToPayload());
        }

        try
        {
            isKinematicMotionActive = true;
            await attackDash.DashAsync(dashDirection, definition.dashDistance, definition.dashDuration, token);
            isKinematicMotionActive = false;

            if (definition.skipHitbox)
            {
                return;
            }

            // Timed fallback for projects without Mixamo Animation Events yet.
            // If OpenCancelWindow / anim Disable already sealed the window, EnableHitbox no-ops.
            await UniTask.Delay(TimeSpan.FromSeconds(definition.hitboxEnableDelay), cancellationToken: token);
            hitbox.EnableHitbox();
            await UniTask.Delay(TimeSpan.FromSeconds(definition.hitboxActiveDuration), cancellationToken: token);
            hitbox.DisableHitbox();
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
                animatorDriver.ResetAttack();

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

        ResolveLaunch(launchable, hitDirection, payload.LaunchDistance).Forget();
    }

    async UniTaskVoid ResolveLaunch(ICombatLaunchable launchable, Vector3 hitDirection, float launchDistance)
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = new CancellationTokenSource();
        var token = attackCts.Token;
        var generation = ++attackGeneration;

        isBusy = true;
        isAttackPlaying = true;
        inputBuffer.SetOpen(false);
        playerController.SetMovementEnabled(false);

        try
        {
            isKinematicMotionActive = true;
            await sequencer.HandleLaunchAndChaseAsync(launchable, hitDirection, launchDistance, token);
        }
        catch (OperationCanceledException)
        {
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
        pendingCommitAt = 0f;
        hitbox.EndSwing();
        animatorDriver.ResetAttack();
        inputBuffer.SetOpen(true);
        playerController.SetMovementEnabled(true);
        if (body != null)
        {
            body.isKinematic = true;
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
            recipes = new[]
            {
                new ComboRecipe
                {
                    name = "Light",
                    sequence = new[] { AttackInputType.L },
                    attackId = AttackId.Light
                },
                new ComboRecipe
                {
                    name = "Heavy",
                    sequence = new[] { AttackInputType.H },
                    attackId = AttackId.Heavy
                },
                new ComboRecipe
                {
                    name = "Dash",
                    sequence = new[] { AttackInputType.D },
                    attackId = AttackId.Dash
                },
                new ComboRecipe
                {
                    name = "Dragon Finisher",
                    sequence = new[]
                    {
                        AttackInputType.H,
                        AttackInputType.H,
                        AttackInputType.L,
                        AttackInputType.H
                    },
                    attackId = AttackId.FinisherHHLH
                }
            };
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
        expanded[recipes.Length] = new ComboRecipe
        {
            name = "Dash",
            sequence = new[] { AttackInputType.D },
            attackId = AttackId.Dash
        };
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

    static AttackDefinition[] CreateDefaultAttacks()
    {
        return new[]
        {
            new AttackDefinition
            {
                attackId = AttackId.Light,
                dashDistance = 0.5f,
                dashDuration = 0.07f,
                hitboxEnableDelay = 0.04f,
                hitboxActiveDuration = 0.1f,
                triggersChaseSequence = false,
                payload = new HitPayloadData
                {
                    damage = 10,
                    hitStunDuration = 0.12f,
                    knockbackType = KnockbackType.Standard,
                    launchDistance = 0.75f
                }
            },
            new AttackDefinition
            {
                attackId = AttackId.Heavy,
                dashDistance = 2f,
                dashDuration = 0.1f,
                hitboxEnableDelay = 0.06f,
                hitboxActiveDuration = 0.12f,
                triggersChaseSequence = false,
                payload = new HitPayloadData
                {
                    damage = 18,
                    hitStunDuration = 0.2f,
                    knockbackType = KnockbackType.Standard,
                    launchDistance = 1.5f
                }
            },
            CreateDashAttackDefinition(),
            new AttackDefinition
            {
                attackId = AttackId.FinisherHHLH,
                dashDistance = 2f,
                dashDuration = 0.1f,
                hitboxEnableDelay = 0.05f,
                hitboxActiveDuration = 0.14f,
                triggersChaseSequence = true,
                payload = new HitPayloadData
                {
                    damage = 30,
                    hitStunDuration = 0.35f,
                    knockbackType = KnockbackType.KnockbackToDistance,
                    launchDistance = 6f
                }
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
