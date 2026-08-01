#nullable enable
using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] PlayerController playerController = null!;
    [SerializeField] DamageAreaHandler damageAreaHandler = null!;
    
    [Header("Attack Config")]
    [SerializeField] float attackDistance;
    [SerializeField] float attackArea;
    [SerializeField] LayerMask layerMask;
    public event Action? OnHealthChanged;
    
    PlayerData playerData = null!;
    int currentHealth;
    int maxHealth;
    public int CurrentHealth=> currentHealth;
    public int MaxHealth=> maxHealth;
    
    public void Initialize(PlayerData aPlayerData)
    {
        playerData = aPlayerData;  
        playerController.Initialize(playerData.speed);
        playerController.OnFloorClicked += HandlePlayerClickInput;
        currentHealth = playerData.maxHealth;
    }

    void HandlePlayerClickInput(Vector3 worldPosition)
    {
        var damageData = new DamageDetectionData();
        var direction = worldPosition - transform.position;
        direction.Normalize();
        var attackCenter = transform.position+ (direction*attackDistance);
        
        damageData.detectionCenter = attackCenter;
        damageData.detectionRadius = attackArea;
        damageData.layerMask = layerMask;
        
        var damageables = damageAreaHandler.GetDamageables(damageData);
        foreach (var dam in damageables)
        {
            dam.Damage(20);
        }
    }
    public void Damage(int damage)
    {
        currentHealth -=damage;
        OnHealthChanged?.Invoke();
        if (currentHealth <= 0)
            Death();
    }

    void Death()
    {
        Debug.Log("Player is dead");
    }
}