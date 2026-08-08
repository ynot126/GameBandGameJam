#nullable enable
using UnityEngine;

public interface ICombatTarget
{
    Transform Transform { get; }
    /// <summary>Alive, not invulnerable, not downed/untargetable.</summary>
    bool IsLockable { get; }
    /// <summary>0 = dead/empty, 1 = full health.</summary>
    float RemainingHealthNormalized { get; }
    /// <summary>True while mid-attack; used for threat scoring bonus.</summary>
    bool IsThreatening { get; }
}
