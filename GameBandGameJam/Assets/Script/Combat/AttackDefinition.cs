#nullable enable
using System;
using UnityEngine;

[Serializable]
public class AttackDefinition
{
    public AttackId attackId = AttackId.None;
    public float dashDistance = 0.5f;
    public float dashDuration = 0.08f;
    public float hitboxEnableDelay = 0.05f;
    public float hitboxActiveDuration = 0.12f;
    public float hitboxRadius = 0.6f;
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
