#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour, IHitable, ICombatTarget
{
    [Header("Config")]
    [SerializeField] int maxHealth = 100;
    
    [Header("Components")]
    [SerializeField] LayerMask wallMask;
    [SerializeField] BaseEnemyAI enemyAiPrefab = null!;

    [Header("Knockback")]
    [SerializeField, Min(0.01f)] float standardKnockbackDuration = 0.08f;
    [SerializeField, Min(0f)] float standardKnockbackArcHeight = 0.35f;
    [SerializeField, Min(0.01f)] float launchKnockbackDuration = 0.18f;
    [SerializeField, Min(0f)] float launchKnockbackArcHeight = 1.25f;

    const float RingOutDuration = 0.35f;
    const float RingOutArcHeight = 8f;
    const float RingOutWaterY = -2f;
    
    // Reference
    readonly LaunchMotor launchMotor = new();
    Rigidbody body = null!;
    BaseEnemyAI enemyAi = null!;
    
    // States
    int currentHealth;
    float hitStunUntil;
    float groundedY;
    int knockbackGeneration;
    bool isKnockbackActive;
    bool isDead;
    
    // public fields reference
    public Transform Transform => transform;
    public bool IsLockable => !isDead && currentHealth > 0;
    public float RemainingHealthNormalized =>
        maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);

    public bool IsThreatening => false;
    
    // CTS
    CancellationTokenSource? launchCts;

    public event Action? OnDeath;

    #region Life Cycle

    public void Initialize(Transform playerTransform)
    {
        currentHealth = maxHealth;
        isDead = false;

        InitializeBody();
        InitializeWallMask();
        InitializeKnockback();
        InitializeAI(playerTransform);
    }
    
    void InitializeBody()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        groundedY = body.position.y;
    }

    void InitializeWallMask()
    {
        if (wallMask.value == 0)
        {
            // Everything except Entity (layer 6).
            wallMask = ~(1 << 6);
        }
    }

    void InitializeKnockback()
    {
        launchMotor.Initialize(body, wallMask, groundedY, launchKnockbackDuration, launchKnockbackArcHeight);
        knockbackGeneration = 0;
        hitStunUntil = 5f;
        isKnockbackActive = false;
    }

    void InitializeAI(Transform playerTransform)
    {
        enemyAi = Instantiate(enemyAiPrefab, transform);
        enemyAi.Initialize(playerTransform);
    }
    
    void Update()
    {
        if (isDead || isKnockbackActive || Time.time < hitStunUntil)
        {
            return;
        }

        enemyAi.UpdateAIMovement();
    }

    #endregion

    #region Presentation

    public async UniTask SpawnAnimation()
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(1f, 1f);
    }

    #endregion

    #region Damage And Stun

    public void TryDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    public void TryStun(float stunDuration)
    {
        if (isDead || stunDuration <= 0f)
        {
            return;
        }

        hitStunUntil = Mathf.Max(hitStunUntil, Time.time + stunDuration);
        enemyAi.CancelPendingActions();
    }

    void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        CancelActiveKnockback();
        enemyAi.CancelPendingActions();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    #endregion

    #region Knockback

    public UniTask TryKnockback(
        KnockbackType knockbackType,
        float launchDistance,
        Vector3 hitDirection,
        CancellationToken cancellationToken = default)
    {
        if (isDead)
        {
            return UniTask.CompletedTask;
        }

        if (knockbackType == KnockbackType.KnockbackToDistance)
        {
            return LaunchAsync(hitDirection, launchDistance, cancellationToken);
        }

        ApplyStandardKnockback(hitDirection, launchDistance).Forget();
        return UniTask.CompletedTask;
    }

    async UniTask LaunchAsync(Vector3 direction, float distance, CancellationToken cancellationToken)
    {
        var (generation, token) = BeginKnockback(cancellationToken, launchKnockbackDuration);

        try
        {
            await launchMotor.LaunchAsync(direction, distance, token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CompleteKnockback(generation);
        }
    }

    async UniTaskVoid ApplyStandardKnockback(Vector3 direction, float distance)
    {
        var (generation, token) = BeginKnockback(CancellationToken.None, standardKnockbackDuration);

        try
        {
            var flat = Flatten(direction);
            var origin = body.position;
            var travel = ClampTravelAgainstWalls(origin, flat, distance);
            var end = origin + flat * travel;
            end.y = groundedY;
            await KinematicMover.MoveAlongArcAsync(
                body,
                end,
                standardKnockbackDuration,
                standardKnockbackArcHeight,
                token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            CompleteKnockback(generation);
        }
    }

    (int Generation, CancellationToken Token) BeginKnockback(
        CancellationToken cancellationToken,
        float stunDuration)
    {
        var generation = ++knockbackGeneration;
        CancelActiveKnockback();
        TryStun(stunDuration);
        isKnockbackActive = true;
        launchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return (generation, launchCts.Token);
    }

    void CompleteKnockback(int generation)
    {
        if (generation != knockbackGeneration)
        {
            return;
        }

        StopBodyMotion();
        isKnockbackActive = false;
    }

    void CancelActiveKnockback()
    {
        launchCts?.Cancel();
        launchCts?.Dispose();
        launchCts = null;
    }

    void StopBodyMotion()
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    #endregion

    #region Boundary Ring Out

    public void RingOutAndKill(Vector3 outwardDirection, float launchDistance)
    {
        RingOutAndKillAsync(outwardDirection, launchDistance).Forget();
    }

    async UniTaskVoid RingOutAndKillAsync(Vector3 outwardDirection, float launchDistance)
    {
        if (isDead)
        {
            return;
        }

        var (generation, token) = BeginKnockback(CancellationToken.None, RingOutDuration);
        enemyAi.CancelPendingActions();

        body.isKinematic = true;
        body.detectCollisions = false;
        StopBodyMotion();
        DisableAllColliders();

        try
        {
            var flat = Flatten(outwardDirection);
            var origin = body.position;
            var end = origin + flat * launchDistance;
            end.y = RingOutWaterY;

            await MoveRingOutArcAsync(end, token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (generation == knockbackGeneration)
            {
                Death();
            }
        }
    }

    void DisableAllColliders()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    async UniTask MoveRingOutArcAsync(Vector3 worldEnd, CancellationToken cancellationToken)
    {
        // Direct position writes so walls cannot stop the fly-off even if a collider remains.
        if (RingOutDuration <= 0f)
        {
            body.position = worldEnd;
            return;
        }

        var start = body.position;
        var elapsed = 0f;

        while (elapsed < RingOutDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            elapsed += Time.fixedDeltaTime;
            var t = Mathf.Clamp01(elapsed / RingOutDuration);
            var grounded = Vector3.Lerp(start, worldEnd, t);
            var peak = RingOutArcHeight * 4f * t * (1f - t);
            grounded.y += peak;
            body.position = grounded;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken);
        }

        body.position = worldEnd;
    }

    #endregion

    #region Physics Helpers

    float ClampTravelAgainstWalls(Vector3 origin, Vector3 direction, float desiredDistance)
    {
        const float skinWidth = 0.2f;
        if (Physics.Raycast(
                origin + Vector3.up * 0.5f,
                direction,
                out var hit,
                desiredDistance + skinWidth,
                wallMask,
                QueryTriggerInteraction.Ignore))
        {
            return Mathf.Max(0f, hit.distance - skinWidth);
        }

        return desiredDistance;
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }

    #endregion
}
