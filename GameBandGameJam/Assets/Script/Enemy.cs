#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour, IHitable, ICombatTarget
{
    [SerializeField] int maxHealth = 100;
    [SerializeField] LayerMask wallMask;
    [SerializeField] BaseEnemyAI enemyAiPrefab = null!;

    [Header("Knockback")]
    [SerializeField, Min(0.01f)] float standardKnockbackDuration = 0.08f;
    [SerializeField, Min(0f)] float standardKnockbackArcHeight = 0.35f;
    [SerializeField, Min(0.01f)] float launchKnockbackDuration = 0.18f;
    [SerializeField, Min(0f)] float launchKnockbackArcHeight = 1.25f;

    readonly LaunchMotor launchMotor = new();
    CancellationTokenSource? launchCts;
    Rigidbody body = null!;
    BaseEnemyAI enemyAi = null!;
    int currentHealth;
    float hitStunUntil;
    float groundedY;
    int knockbackGeneration;
    bool isKnockbackActive;
    bool isDead;

    public Transform Transform => transform;
    public bool IsLockable => !isDead && currentHealth > 0;
    public float RemainingHealthNormalized =>
        maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
    /// <summary>Stub until enemy attack AI exposes mid-attack state.</summary>
    public bool IsThreatening => false;

    public void Initialize()
    {
        currentHealth = maxHealth;
        isDead = false;
        body = GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        groundedY = body.position.y;

        if (wallMask.value == 0)
        {
            // Everything except Entity (layer 6).
            wallMask = ~(1 << 6);
        }

        launchMotor.Initialize(body, wallMask, groundedY, launchKnockbackDuration, launchKnockbackArcHeight);
        knockbackGeneration = 0;
        hitStunUntil = 0f;
        isKnockbackActive = false;

        enemyAi = Instantiate(enemyAiPrefab, transform);
        enemyAi.Initialize();
    }

    void Update()
    {
        if (isDead|| !isKnockbackActive && Time.time >= hitStunUntil)
        {
            return;
        }

        enemyAi.UpdateAIMovement();
    }
    
    public async UniTask SpawnAnimation()
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(1f, 1f);
    }

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
        if (isDead)
        {
            return;
        }

        hitStunUntil = Time.time + stunDuration;
    }

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
        var generation = ++knockbackGeneration;
        CancelActiveKnockback();
        isKnockbackActive = true;
        launchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = launchCts.Token;

        try
        {
            await launchMotor.LaunchAsync(direction, distance, token);
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            if (generation == knockbackGeneration)
            {
                StopBodyMotion();
                isKnockbackActive = false;
            }
        }
    }

    async UniTaskVoid ApplyStandardKnockback(Vector3 direction, float distance)
    {
        var generation = ++knockbackGeneration;
        CancelActiveKnockback();
        isKnockbackActive = true;
        launchCts = new CancellationTokenSource();
        var token = launchCts.Token;

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
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            if (generation == knockbackGeneration)
            {
                StopBodyMotion();
                isKnockbackActive = false;
            }
        }
    }

    void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        CancelActiveKnockback();
        Destroy(gameObject);
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
}
