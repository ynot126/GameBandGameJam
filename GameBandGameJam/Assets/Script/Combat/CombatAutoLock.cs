#nullable enable
using UnityEngine;

public sealed class CombatAutoLock
{
    Transform owner = null!;
    LayerMask hitMask;
    LayerMask wallMask;
    float coneDegrees = 90f;
    float snapMargin = 0.75f;
    float persistenceRange = 4f;
    float alignmentWeight = 1f;
    float distanceWeight = 1f;
    float threatWeight = 0.35f;
    float lowHealthWeight = 0.2f;
    ICombatTarget? ownerTarget;
    readonly Collider[] overlapBuffer = new Collider[32];

    public ICombatTarget? LockedTarget { get; private set; }
    public bool HasLock => IsTargetUsable(LockedTarget);

    public void Initialize(
        Transform ownerTransform,
        LayerMask entityMask,
        LayerMask walls,
        float lockConeDegrees,
        float lockSnapMargin,
        float lockPersistenceRange,
        float scoreAlignmentWeight,
        float scoreDistanceWeight,
        float scoreThreatWeight,
        float scoreLowHealthWeight)
    {
        owner = ownerTransform;
        hitMask = entityMask;
        wallMask = walls;
        coneDegrees = lockConeDegrees;
        snapMargin = lockSnapMargin;
        persistenceRange = lockPersistenceRange;
        alignmentWeight = scoreAlignmentWeight;
        distanceWeight = scoreDistanceWeight;
        threatWeight = scoreThreatWeight;
        lowHealthWeight = scoreLowHealthWeight;
        ownerTarget = ownerTransform.GetComponent<ICombatTarget>();
        Clear();
    }

    public void Clear()
    {
        LockedTarget = null;
    }

    public void ForceLock(ICombatTarget target)
    {
        if (!IsTargetUsable(target))
        {
            return;
        }

        LockedTarget = target;
    }

    public static float ComputeAttackReach(ComboData comboData)
    {
        return Mathf.Max(0f, comboData.hitboxLocalOffset.z) + Mathf.Max(0f, comboData.hitboxRadius);
    }

    /// <summary>
    /// Keeps the current lock if still valid for combo retention; otherwise acquires a new target.
    /// Returns false when a previously locked target was lost to range+LOS (combo should break).
    /// </summary>
    public bool TrySelectOrRetain(float attackReach, out bool lostPersistedTarget)
    {
        lostPersistedTarget = false;

        if (HasLock)
        {
            if (IsPersistedLockValid())
            {
                return true;
            }

            // Destroyed / untargetable mid-combo → reacquire without breaking.
            if (!IsTargetUsable(LockedTarget))
            {
                Clear();
                return TryAcquire(attackReach);
            }

            // Still exists but left persistence range and lost LOS → break.
            lostPersistedTarget = true;
            Clear();
            return false;
        }

        return TryAcquire(attackReach);
    }

    public bool TryAcquire(float attackReach)
    {
        var maxRange = attackReach + snapMargin;
        if (maxRange <= 0.0001f)
        {
            Clear();
            return false;
        }

        var origin = owner.position + Vector3.up * 0.8f;
        var facing = Flatten(owner.forward);
        var halfCone = Mathf.Max(1f, coneDegrees) * 0.5f;

        var hitCount = Physics.OverlapSphereNonAlloc(
            owner.position,
            maxRange,
            overlapBuffer,
            hitMask,
            QueryTriggerInteraction.Ignore);

        ICombatTarget? best = null;
        var bestScore = float.MinValue;

        for (var i = 0; i < hitCount; i++)
        {
            var col = overlapBuffer[i];
            if (col == null)
            {
                continue;
            }

            var target = col.GetComponentInParent<ICombatTarget>();
            if (target == null || ReferenceEquals(target, ownerTarget))
            {
                continue;
            }

            if (!IsTargetUsable(target))
            {
                continue;
            }

            var targetPos = target.Transform.position;
            var toTarget = targetPos - owner.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;
            if (distance > maxRange || distance <= 0.0001f)
            {
                continue;
            }

            var dir = toTarget / distance;
            var angle = Vector3.Angle(facing, dir);
            if (angle > halfCone)
            {
                continue;
            }

            if (!HasLineOfSight(origin, targetPos + Vector3.up * 0.8f))
            {
                continue;
            }

            var score = ScoreCandidate(target, angle, halfCone, distance, maxRange);
            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        LockedTarget = best;
        return best != null;
    }

    public bool IsPersistedLockValid()
    {
        if (!IsTargetUsable(LockedTarget))
        {
            return false;
        }

        var target = LockedTarget!;
        var toTarget = target.Transform.position - owner.position;
        toTarget.y = 0f;
        var distance = toTarget.magnitude;
        var inRange = distance <= persistenceRange;
        var origin = owner.position + Vector3.up * 0.8f;
        var los = HasLineOfSight(origin, target.Transform.position + Vector3.up * 0.8f);

        // Spec: break only when outside range AND no LOS.
        if (!inRange && !los)
        {
            return false;
        }

        return true;
    }

    public bool TryGetLockDirection(out Vector3 planarDirection)
    {
        planarDirection = default;
        if (!HasLock)
        {
            return false;
        }

        var toTarget = LockedTarget!.Transform.position - owner.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        planarDirection = toTarget.normalized;
        return true;
    }

    float ScoreCandidate(
        ICombatTarget target,
        float angleDegrees,
        float halfCone,
        float distance,
        float maxRange)
    {
        var alignment = 1f - Mathf.Clamp01(angleDegrees / halfCone);
        var proximity = 1f - Mathf.Clamp01(distance / maxRange);
        var threat = target.IsThreatening ? 1f : 0f;
        var lowHealth = 1f - Mathf.Clamp01(target.RemainingHealthNormalized);

        return alignment * alignmentWeight
            + proximity * distanceWeight
            + threat * threatWeight
            + lowHealth * lowHealthWeight;
    }

    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var delta = to - from;
        var distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            return true;
        }

        return !Physics.Linecast(from, to, wallMask, QueryTriggerInteraction.Ignore);
    }

    static bool IsTargetUsable(ICombatTarget? target)
    {
        if (target == null)
        {
            return false;
        }

        // Unity destroyed objects compare equal to null via overloaded ==.
        if (target is Object unityObj && unityObj == null)
        {
            return false;
        }

        if (target.Transform == null)
        {
            return false;
        }

        return target.IsLockable;
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        return direction.normalized;
    }
}
