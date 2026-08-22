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
    [SerializeField] PlayerStamina playerStamina = null!;
    [SerializeField] CombatHitbox combatHitbox = null!;
    [SerializeField] PlayerAnimationController animationController = null!;
    [SerializeField] CollisionDetector collisionDetector = null!;
    [SerializeField] DamageNumberVisual damageNumberPrefab = null!;
    [SerializeField] int playerDefaultSpeed = 5;

    [Header("Combat Config")]
    [SerializeField] PlayerCombatConfig combatConfig = null!;
    [SerializeField] LayerMask layerMask;

    public event Action? OnHealthChanged;
    public event Action? OnDeath;

    PlayerData playerData = null!;
    int currentHealth;
    int maxHealth;
    int damageTokenMultipler = 1;
    float attackSpeedMultiplier = 1f;
    float damageMultiplier = 1f;
    float dashDistanceMultiplier = 1f;
    float playerSpeedMultiplier = 1f;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public PlayerStamina Stamina => playerStamina;

    public void Initialize(PlayerData aPlayerData)
    {
        playerData = aPlayerData;
        currentHealth = playerData.currentHealth;
        maxHealth = playerData.maxHealth;

        body.isKinematic = false;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        var animator = GetComponentInChildren<Animator>();
        animationController.Initialize(animator);

        playerController.Initialize(GetEffectiveMovementSpeed(), body);
        playerController.OnLocomotionStarted += animationController.PlayRun;
        playerController.OnLocomotionStopped += animationController.PlayIdle;

        playerStamina.Initialize();

        playerCombat.Initialize(
            combatConfig,
            playerController,
            body,
            combatHitbox,
            animationController,
            damageNumberPrefab,
            layerMask,
            playerStamina);

        UpdateSkillMultipliersAndEffects();
    }

    void UpdateSkillMultipliersAndEffects()
    {
        playerCombat.SetAttackSpeedMultiplier(attackSpeedMultiplier);
        playerCombat.SetDamageMultiplier(damageMultiplier);
        playerCombat.SetDashDistanceMultiplier(dashDistanceMultiplier);
        playerController.SetMovementSpeed(GetEffectiveMovementSpeed());
    }

    float GetEffectiveMovementSpeed()
    {
        return playerDefaultSpeed * playerSpeedMultiplier;
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

    public void MultiplyAttackSpeed(float factor)
    {
        attackSpeedMultiplier *= factor;
        UpdateSkillMultipliersAndEffects();
    }

    public void MultiplyDamage(float factor)
    {
        damageMultiplier *= factor;
        playerCombat.SetDamageMultiplier(damageMultiplier);
    }

    public void InvertMovementAxes()
    {
        playerController.InvertMovementAxes();
    }

    public void MultiplyDashDistance(float factor)
    {
        dashDistanceMultiplier *= factor;
        playerCombat.SetDashDistanceMultiplier(dashDistanceMultiplier);
    }

    public void MultiplySpeed(float factor)
    {
        playerSpeedMultiplier *= factor;
        playerController.SetMovementSpeed(GetEffectiveMovementSpeed());
    }

    public void MultiplyMaxHealth(float factor)
    {
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * factor));
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void ScaleCurrentHealth(float factor)
    {
        currentHealth = Mathf.Clamp(Mathf.RoundToInt(currentHealth * factor), 1, maxHealth);
        OnHealthChanged?.Invoke();
    }

    public void RestoreHealthToMax()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke();
    }

    void Death()
    {
        OnDeath?.Invoke();
    }
}
