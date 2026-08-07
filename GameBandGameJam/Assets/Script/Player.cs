#nullable enable
using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [Header("Components")]
    [SerializeField] PlayerController playerController = null!;
    [SerializeField] PlayerCombat playerCombat = null!;
    [SerializeField] CombatHitbox combatHitbox = null!;
    [SerializeField] PlayerAnimatorDriver animatorDriver = null!;
    [SerializeField] CollisionDetector collisionDetector = null!;
    [SerializeField] DamageNumberVisual damageNumberPrefab = null!;

    [Header("Combat Config")]
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

        var body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
        }

        EnsureCombatComponents();

        playerController.Initialize(playerData.speed);

        playerCombat.Initialize(
            playerController,
            combatHitbox,
            animatorDriver,
            damageNumberPrefab,
            layerMask);
    }

    public void Damage(int damage)
    {
        damage *= damageTokenMultipler;
        currentHealth -= damage;
        OnHealthChanged?.Invoke();
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    public void ApplyHit(in HitPayload payload, Vector3 hitDirection)
    {
        Damage(payload.Damage);
    }

    public void DoubleTakenDamage()
    {
        damageTokenMultipler++;
    }

    void EnsureCombatComponents()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerCombat == null)
        {
            playerCombat = GetComponent<PlayerCombat>();
            if (playerCombat == null)
            {
                playerCombat = gameObject.AddComponent<PlayerCombat>();
            }
        }

        if (combatHitbox == null)
        {
            combatHitbox = GetComponent<CombatHitbox>();
            if (combatHitbox == null)
            {
                combatHitbox = gameObject.AddComponent<CombatHitbox>();
            }
        }

        if (animatorDriver == null)
        {
            animatorDriver = GetComponent<PlayerAnimatorDriver>();
            if (animatorDriver == null)
            {
                animatorDriver = gameObject.AddComponent<PlayerAnimatorDriver>();
            }
        }
    }

    void Death()
    {
        Debug.Log("Player is dead");
    }
}
