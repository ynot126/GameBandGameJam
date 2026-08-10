#nullable enable
using System;
using UnityEngine;

[Serializable]
public class AttackDefinition
{
    public ComboType comboType = ComboType.None;
    public float dashDistance = 0.5f;
    public float dashDuration = 0.08f;
    /// <summary>
    /// Time after the cancel window opens to keep the attack alive for cancel-into combo.
    /// Hitbox and cancel timing come from Animation Events, not this field.
    /// </summary>
    public float recoveryHoldDuration = 0.55f;
    /// <summary>
    /// After active frames / cancel, block Light/Heavy for this long. Dash is still allowed.
    /// Use on finishers so spam cannot skip recovery.
    /// </summary>
    public float attackLockoutDuration;
    /// <summary>
    /// Per-attack combo input window after cancel opens. &lt;= 0 uses config defaultComboResetWindow.
    /// </summary>
    public float comboInputWindow;
    public float hitboxRadius = 1f;
    public Vector3 hitboxLocalOffset = new(0f, 0.8f, 0.7f);
    public bool triggersChaseSequence;
    /// <summary>Dash along current WASD / camera-planar move input instead of facing forward.</summary>
    public bool useMoveInputDirection;
    /// <summary>Mobility-only move: no hitbox / damage window.</summary>
    public bool skipHitbox;
    public HitPayloadData payload = new()
    {
        damage = 10,
        hitStunDuration = 0.15f,
        knockbackType = KnockbackType.Standard,
        launchDistance = 1f
    };
}
