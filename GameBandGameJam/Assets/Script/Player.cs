#nullable enable
using System;
using UnityEngine;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] PlayerController playerController = null!;

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
        currentHealth = playerData.maxHealth;
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