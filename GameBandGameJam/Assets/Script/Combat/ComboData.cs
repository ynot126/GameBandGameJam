#nullable enable
using System;
using UnityEngine;

[Serializable]
public class ComboData
{
    public string name = string.Empty;
    public AttackInputType[] sequence = Array.Empty<AttackInputType>();
    public float dashDistance = 0.5f;
    public float dashDuration = 0.08f;
    
    [Tooltip("Time after the cancel window opens to keep the attack alive for cancel-into combo. " +
             "Hitbox and cancel timing come from Animation Events, not this field.")]
    public float recoveryHoldDuration = 0.55f;
    
    [Tooltip("After active frames / cancel, block Light/Heavy for this long. " +
             "Dash is still allowed. Use on finishers so spam cannot skip recovery.")]
    public float attackLockoutDuration;
    
    [Tooltip("Per-attack combo input window after cancel opens. <= 0 uses config defaultComboResetWindow.")]
    public float comboInputWindow;
    public float hitboxRadius = 1f;
    public Vector3 hitboxLocalOffset = new(0f, 0.8f, 0.7f);
    public bool triggersChaseSequence;
    
    [Tooltip("Dash along current WASD / camera-planar move input instead of facing forward.")]
    public bool useMoveInputDirection;
    
    [Tooltip("Mobility-only move: no hitbox / damage window.")]
    public bool skipHitbox;

    [Tooltip("When enabled, hits near the map edge ring out using config boundary proximity and launch distance.")]
    public bool killNearBoundary;

    public HitPayloadData payload = new()
    {
        damage = 10,
        hitStunDuration = 1f,
        knockbackType = KnockbackType.Standard,
        launchDistance = 1f
    };
}
