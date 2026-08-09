#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IHitable
{
    void TryDamage(int damage);
    void TryStun(float stunDuration);
    UniTask TryKnockback(
        KnockbackType knockbackType,
        float launchDistance,
        Vector3 hitDirection,
        CancellationToken cancellationToken = default);

    void ApplyHit(in HitPayload payload, Vector3 hitDirection)
    {
        TryDamage(payload.Damage);
        TryStun(payload.HitStunDuration);

        // Distance launches are awaited by the attacker for chase / finisher sequencing.
        if (payload.KnockbackType == KnockbackType.KnockbackToDistance)
        {
            return;
        }

        TryKnockback(payload.KnockbackType, payload.LaunchDistance, hitDirection).Forget();
    }
}
