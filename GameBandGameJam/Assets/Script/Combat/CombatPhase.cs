#nullable enable

public enum CombatPhase
{
    Idle,
    Startup,
    Active,
    CancelWindow,
    Recovery,
    HardBreak,
    ChaseAwait,
    /// <summary>Launch / chase resolution after a confirmed knockback hit.</summary>
    Launch,
}
