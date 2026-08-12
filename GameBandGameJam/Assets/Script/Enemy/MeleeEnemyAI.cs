#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MeleeEnemyAI : BaseEnemyAI
{
    [SerializeField] float speed = 3f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] int damage = 10;
    [SerializeField, Min(0.01f)] float attackRate = 1f;
    [SerializeField, Min(0f)] float attackLungeDistance = 0.35f;
    [SerializeField, Min(0.01f)] float attackLungeDuration = 0.08f;
    [SerializeField, Min(0.01f)] float attackReturnDuration = 0.12f;
    [SerializeField] DamageNumberVisual damageNumberPrefab = null!;

    Rigidbody body = null!;
    IHitable playerHitable = null!;
    float nextAttackTime;
    bool isAttacking;
    CancellationTokenSource? attackCts;

    public override void Initialize(Transform playerTransform)
    {
        base.Initialize(playerTransform);
        body = GetComponentInParent<Rigidbody>();
        playerHitable = playerTransform.GetComponent<IHitable>();
    }

    public override void CancelPendingActions()
    {
        CancelAttack();
    }

    public override void UpdateAIMovement()
    {
        if (isAttacking)
        {
            return;
        }

        var toPlayer = PlayerTransform.position - body.position;
        toPlayer.y = 0f;
        var distance = toPlayer.magnitude;

        if (distance <= attackRange)
        {
            TryAttack();
            return;
        }

        if (distance <= 0.0001f)
        {
            return;
        }

        var direction = toPlayer / distance;
        body.MovePosition(body.position + direction * (speed * Time.deltaTime));
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + 1f / attackRate;
        PerformAttackLungeAsync().Forget();
    }

    async UniTaskVoid PerformAttackLungeAsync()
    {
        CancelAttack();
        isAttacking = true;
        attackCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = attackCts.Token;

        try
        {
            var toPlayer = PlayerTransform.position - body.position;
            toPlayer.y = 0f;
            var direction = toPlayer.sqrMagnitude > 0.0001f
                ? toPlayer.normalized
                : Flatten(body.transform.forward);

            await KinematicMover.MoveByAsync(
                body,
                direction * attackLungeDistance,
                attackLungeDuration,
                token);

            playerHitable.TryDamage(damage);
            ShowDamageNumber();

            await KinematicMover.MoveByAsync(
                body,
                -direction * attackLungeDistance,
                attackReturnDuration,
                token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (attackCts != null)
            {
                attackCts.Dispose();
                attackCts = null;
            }

            isAttacking = false;
        }
    }

    void CancelAttack()
    {
        if (attackCts == null)
        {
            return;
        }

        attackCts.Cancel();
        attackCts.Dispose();
        attackCts = null;
        isAttacking = false;
    }

    void ShowDamageNumber()
    {
        var spawnPos = PlayerTransform.position + Vector3.up;
        var damageNumber = Instantiate(damageNumberPrefab);
        damageNumber.Initialize(spawnPos, damage);
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
