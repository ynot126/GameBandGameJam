#nullable enable
using UnityEngine;

public class MeleeEnemyAI : BaseEnemyAI
{
    [SerializeField] float speed = 3f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] int damage = 10;
    [SerializeField, Min(0.01f)] float attackRate = 1f;

    Rigidbody body = null!;
    IHitable playerHitable = null!;
    float nextAttackTime;

    public override void Initialize(Transform playerTransform)
    {
        base.Initialize(playerTransform);
        body = GetComponentInParent<Rigidbody>();
        playerHitable = playerTransform.GetComponent<IHitable>();
    }

    public override void UpdateAIMovement()
    {
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
        playerHitable.TryDamage(damage);
    }
}
