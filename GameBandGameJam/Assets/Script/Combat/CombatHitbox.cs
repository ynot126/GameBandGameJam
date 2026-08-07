#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatHitbox : MonoBehaviour
{
    [SerializeField] LayerMask hitMask;
    [SerializeField] float radius = 0.6f;
    [SerializeField] Vector3 localOffset = new(0f, 0.8f, 0.7f);
    [SerializeField] Transform? sphereIndicator;

    readonly HashSet<IDamageable> hitThisSwing = new();
    HitPayload activePayload;
    Transform owner = null!;
    IDamageable? ownerDamageable;
    bool isActive;
    /// <summary>
    /// True between <see cref="BeginSwing"/> and the first <see cref="DisableHitbox"/> / <see cref="EndSwing"/>.
    /// Prevents timed fallbacks from re-enabling after cancel closes the window.
    /// </summary>
    bool hitWindowOpen;

    public event Action<IDamageable, HitPayload, Vector3>? OnHitConfirmed;

    public void Initialize(Transform ownerTransform, LayerMask mask, Transform? indicator = null)
    {
        owner = ownerTransform;
        ownerDamageable = ownerTransform.GetComponent<IDamageable>();
        hitMask = mask;

        if (indicator != null)
        {
            sphereIndicator = indicator;
        }

        if (sphereIndicator == null)
        {
            TryResolveIndicatorFromAttackDetector();
        }

        HideIndicator();
        EndSwing();
    }

    public void ConfigureShape(float hitRadius, Vector3 offset)
    {
        radius = hitRadius;
        localOffset = offset;
        if (isActive)
        {
            RefreshIndicatorTransform();
        }
    }

    public void BeginSwing(in HitPayload payload)
    {
        activePayload = payload;
        hitThisSwing.Clear();
        hitWindowOpen = true;
        isActive = false;
        HideIndicator();
    }

    // Animation Event entry points on attack clips.
    public void EnableHitbox()
    {
        if (!hitWindowOpen)
        {
            return;
        }

        isActive = true;
        ShowIndicator();
    }

    public void DisableHitbox()
    {
        isActive = false;
        hitWindowOpen = false;
        HideIndicator();
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

        RefreshIndicatorTransform();

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

    void TryResolveIndicatorFromAttackDetector()
    {
        var detector = owner != null
            ? owner.GetComponentInChildren<PlayerAttackDetector>(true)
            : GetComponentInChildren<PlayerAttackDetector>(true);
        if (detector == null)
        {
            return;
        }

        sphereIndicator = detector.SphereIndicator;
    }

    void ShowIndicator()
    {
#if UNITY_EDITOR
        if (sphereIndicator == null)
        {
            return;
        }

        RefreshIndicatorTransform();
        sphereIndicator.gameObject.SetActive(true);
#endif
    }

    void HideIndicator()
    {
        if (sphereIndicator == null)
        {
            return;
        }

        sphereIndicator.gameObject.SetActive(false);
    }

    void RefreshIndicatorTransform()
    {
#if UNITY_EDITOR
        if (sphereIndicator == null || owner == null)
        {
            return;
        }

        // Match OverlapSphere center; default Unity sphere mesh radius is 0.5.
        sphereIndicator.position = owner.TransformPoint(localOffset);
        sphereIndicator.localScale = Vector3.one * (radius * 2f);
#endif
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
