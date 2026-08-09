#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour, IHitable, ICombatTarget
{
    [SerializeField] int maxHealth = 100;
    [SerializeField] LayerMask wallMask;
    [SerializeField] float standardKnockbackDuration = 0.08f;

    readonly LaunchMotor launchMotor = new();
    CancellationTokenSource? launchCts;
    Rigidbody body = null!;
    int currentHealth;
    float hitStunUntil;
    bool aiEnabled = true;
    bool isDead;

    public Transform Transform => transform;
    public bool IsAiEnabled => aiEnabled && Time.time >= hitStunUntil;
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
        body.isKinematic = true;

        if (wallMask.value == 0)
        {
            // Everything except Entity (layer 6).
            wallMask = ~(1 << 6);
        }

        launchMotor.Initialize(body, wallMask);
        aiEnabled = true;
        hitStunUntil = 0f;
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
        SetAiEnabled(false);
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
        SetAiEnabled(false);
        launchCts?.Cancel();
        launchCts?.Dispose();
        launchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = launchCts.Token;

        try
        {
            await launchMotor.LaunchAsync(direction, distance, token);
        }
        finally
        {
            // AI stays off until stun expires; navigation resumes via IsAiEnabled.
            if (Time.time >= hitStunUntil)
            {
                SetAiEnabled(true);
            }
        }
    }

    async UniTaskVoid ApplyStandardKnockback(Vector3 direction, float distance)
    {
        launchCts?.Cancel();
        launchCts?.Dispose();
        launchCts = new CancellationTokenSource();
        var token = launchCts.Token;

        try
        {
            await KinematicMover.MoveByAsync(body, Flatten(direction) * distance, standardKnockbackDuration, token);
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            if (Time.time >= hitStunUntil)
            {
                SetAiEnabled(true);
            }
        }
    }

    void SetAiEnabled(bool val)
    {
        aiEnabled = val;
    }

    void Death()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        launchCts?.Cancel();
        launchCts?.Dispose();
        Destroy(gameObject);
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
