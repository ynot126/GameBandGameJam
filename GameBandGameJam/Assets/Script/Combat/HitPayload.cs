#nullable enable
using System;
using NaughtyAttributes;

[Serializable]
public struct HitPayloadData
{
    public int damage;
    public float hitStunDuration;
    public KnockbackType knockbackType;
    [ShowIf(nameof(knockbackType), KnockbackType.KnockbackToDistance)]
    [AllowNesting]
    public float launchDistance;

    public HitPayload ToPayload()
    {
        return new HitPayload(damage, hitStunDuration, knockbackType, launchDistance);
    }
}

public readonly struct HitPayload
{
    public int Damage { get; }
    public float HitStunDuration { get; }
    public KnockbackType KnockbackType { get; }
    public float LaunchDistance { get; }

    public HitPayload(int damage, float hitStunDuration, KnockbackType knockbackType, float launchDistance)
    {
        Damage = damage;
        HitStunDuration = hitStunDuration;
        KnockbackType = knockbackType;
        LaunchDistance = launchDistance;
    }
}
