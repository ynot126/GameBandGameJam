#nullable enable
using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField, Min(1)] int maxStamina = 100;

    [Header("Costs")]
    [SerializeField, Min(0)] int lightCost = 10;
    [SerializeField, Min(0)] int heavyCost = 20;
    [SerializeField, Min(0)] int dashCost = 15;

    [Header("Regen")]
    [SerializeField, Min(0f)] float regenDelay = 1f;
    [SerializeField, Min(0f)] float regenPerSecond = 25f;

    float currentStamina;
    float lastSpendTime = float.NegativeInfinity;
    int displayedStamina;

    public event Action? OnStaminaChanged;

    public int CurrentStamina => displayedStamina;
    public int MaxStamina => maxStamina;

    public void Initialize()
    {
        currentStamina = maxStamina;
        displayedStamina = maxStamina;
        lastSpendTime = float.NegativeInfinity;
        OnStaminaChanged?.Invoke();
    }

    public void MultiplyMaxStamina(float factor)
    {
        maxStamina = Mathf.Max(1, Mathf.RoundToInt(maxStamina * factor));
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        displayedStamina = Mathf.RoundToInt(currentStamina);
        OnStaminaChanged?.Invoke();
    }

    public int GetCost(AttackInputType input)
    {
        return input switch
        {
            AttackInputType.Light => lightCost,
            AttackInputType.Heavy => heavyCost,
            AttackInputType.Dash => dashCost,
            _ => 0
        };
    }

    public bool CanAfford(AttackInputType input)
    {
        return currentStamina >= GetCost(input);
    }

    public void NotifySpend(AttackInputType input)
    {
        var cost = GetCost(input);
        if (cost <= 0)
        {
            return;
        }

        currentStamina = Mathf.Max(0f, currentStamina - cost);
        lastSpendTime = Time.time;
        NotifyIfDisplayChanged();
    }

    void Update()
    {
        if (currentStamina >= maxStamina)
        {
            return;
        }

        if (Time.time - lastSpendTime < regenDelay)
        {
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
        NotifyIfDisplayChanged();
    }

    void NotifyIfDisplayChanged()
    {
        var nextDisplay = Mathf.RoundToInt(currentStamina);
        if (nextDisplay == displayedStamina)
        {
            return;
        }

        displayedStamina = nextDisplay;
        OnStaminaChanged?.Invoke();
    }
}
