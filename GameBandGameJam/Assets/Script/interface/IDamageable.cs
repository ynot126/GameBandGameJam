#nullable enable
using UnityEngine;

public interface IDamageable
{
    void Damage(int damage);
    void ApplyHit(in HitPayload payload, Vector3 hitDirection);
}
