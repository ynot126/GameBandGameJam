#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatHitbox : MonoBehaviour
{
    [SerializeField] LayerMask hitMask;
    [SerializeField] float radius = 0.6f;
    [SerializeField] Vector3 localOffset = new(0f, 0.8f, 0.7f);

    readonly HashSet<IDamageable> hitThisSwing = new();
    HitPayload activePayload;
    Transform owner = null!;
    IDamageable? ownerDamageable;
    bool isActive;

    public event Action<IDamageable, HitPayload, Vector3>? OnHitConfirmed;

    public void Initialize(Transform ownerTransform, LayerMask mask)
    {
        owner = ownerTransform;
        ownerDamageable = ownerTransform.GetComponent<IDamageable>();
        hitMask = mask;
        DisableHitbox();
    }

    public void ConfigureShape(float hitRadius, Vector3 offset)
    {
        radius = hitRadius;
        localOffset = offset;
    }

    public void BeginSwing(in HitPayload payload)
    {
        activePayload = payload;
        hitThisSwing.Clear();
    }

    // Animation Event entry points on attack clips.
    public void EnableHitbox()
    {
        isActive = true;
    }

    public void DisableHitbox()
    {
        isActive = false;
    }

    public void EndSwing()
    {
        DisableHitbox();
        hitThisSwing.Clear();
    }

    void FixedUpdate()
    {
        if (!isActive || owner == null)
        {
            return;
        }

        var center = owner.TransformPoint(localOffset);
        var colliders = Physics.OverlapSphere(center, radius, hitMask, QueryTriggerInteraction.Ignore);
        var hitDirection = owner.forward;
        hitDirection.y = 0f;
        if (hitDirection.sqrMagnitude <= 0.0001f)
        {
            hitDirection = Vector3.forward;
        }

        hitDirection.Normalize();

        for (var i = 0; i < colliders.Length; i++)
        {
            var damageable = colliders[i].GetComponentInParent<IDamageable>();
            if (damageable == null || ReferenceEquals(damageable, ownerDamageable))
            {
                continue;
            }

            if (!hitThisSwing.Add(damageable))
            {
                continue;
            }

            damageable.ApplyHit(in activePayload, hitDirection);
            OnHitConfirmed?.Invoke(damageable, activePayload, hitDirection);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var origin = owner != null ? owner : transform;
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(origin.TransformPoint(localOffset), radius);
    }
#endif
}
