#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Player : MonoBehaviour, IHitable
{
    [Header("Components")]
    [SerializeField] Rigidbody body = null!;
    [SerializeField] PlayerController playerController = null!;
    [SerializeField] PlayerCombat playerCombat = null!;
    [SerializeField] CombatHitbox combatHitbox = null!;
    [SerializeField] PlayerAnimationController animationController = null!;
    [SerializeField] CollisionDetector collisionDetector = null!;
    [SerializeField] DamageNumberVisual damageNumberPrefab = null!;

    [Header("Combat Config")]
    [SerializeField] PlayerCombatConfig combatConfig = null!;
    [SerializeField] LayerMask layerMask;

    public event Action? OnHealthChanged;

    PlayerData playerData = null!;
    int currentHealth;
    int maxHealth;
    int damageTokenMultipler = 1;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void Initialize(PlayerData aPlayerData)
    {
        playerData = aPlayerData;
        currentHealth = playerData.maxHealth;
        maxHealth = playerData.maxHealth;

        body.isKinematic = false;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        var animator = GetComponentInChildren<Animator>();
        animationController.Initialize(animator);

        playerController.Initialize(playerData.speed, body);

        playerCombat.Initialize(
            combatConfig,
            playerController,
            combatHitbox,
            animationController,
            damageNumberPrefab,
            layerMask);
    }

    public void TryDamage(int damage)
    {
        playerCombat.InterruptFromHit();

        damage *= damageTokenMultipler;
        currentHealth -= damage;
        OnHealthChanged?.Invoke();
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    public void TryStun(float stunDuration)
    {
    }

    public UniTask TryKnockback(
        KnockbackType knockbackType,
        float launchDistance,
        Vector3 hitDirection,
        CancellationToken cancellationToken = default)
    {
        return UniTask.CompletedTask;
    }

    public void DoubleTakenDamage()
    {
        damageTokenMultipler++;
    }

    void Death()
    {
        Debug.Log("Player is dead");
    }
}
