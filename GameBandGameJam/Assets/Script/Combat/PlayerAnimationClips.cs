#nullable enable
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animator state names and provisional clip durations for script-driven playback.
/// State names must match entries in the Player Animator controller.
/// </summary>
public static class PlayerAnimationClips
{
    public const string Idle = "Idle";

    public const string Light = "Light";
    public const string Light1 = "Light1";
    public const string Light2 = "Light2";
    public const string Light3 = "Light3";
    public const string Light4 = "Light4";
    public const string Light5 = "Light5";
    public const string LightFinisher = "LightFinisher";

    public const string Heavy = "Heavy";
    public const string Heavy1 = "Heavy1";
    public const string Heavy2 = "Heavy2";
    public const string Heavy3 = "Heavy3";
    public const string HeavyFinisher = "HeavyFinisher";

    public const string KickPause = "KickPause";
    public const string PunchPause = "PunchPause";
    public const string PunchPause2 = "PunchPause2";

    public const string Dash = "Dash";
    public const string DashLight = "DashLight";
    public const string DashLight1 = "DashLight1";
    public const string DashLight2 = "DashLight2";
    public const string DashLightFinisher = "DashLightFinisher";
    public const string DashHeavy = "DashHeavy";
    public const string DashHeavy1 = "DashHeavy1";
    public const string DashHeavy2 = "DashHeavy2";
    public const string DashHeavyFinisher = "DashHeavyFinisher";

    public const float IdleDuration = 2.9666667f;

    public const float LightDuration = 0.55f;
    public const float Light1Duration = 0.58f;
    public const float Light2Duration = 0.72f;
    public const float Light3Duration = 0.62f;
    public const float Light4Duration = 0.64f;
    public const float Light5Duration = 0.88f;
    public const float LightFinisherDuration = 1f;

    public const float HeavyDuration = 0.7f;
    public const float Heavy1Duration = 0.9f;
    public const float Heavy2Duration = 0.78f;
    public const float Heavy3Duration = 0.95f;
    public const float HeavyFinisherDuration = 1.1f;

    public const float KickPauseDuration = 0.72f;
    public const float PunchPauseDuration = 0.88f;
    public const float PunchPause2Duration = 0.9f;

    public const float DashDuration = 0.35f;
    public const float DashLightDuration = 0.55f;
    public const float DashLight1Duration = 0.58f;
    public const float DashLight2Duration = 0.62f;
    public const float DashLightFinisherDuration = 1f;
    public const float DashHeavyDuration = 0.7f;
    public const float DashHeavy1Duration = 0.78f;
    public const float DashHeavy2Duration = 0.86f;
    public const float DashHeavyFinisherDuration = 1.1f;

    static readonly Dictionary<AttackId, (string Name, float Duration)> ClipsByAttackId = new()
    {
        { AttackId.Light, (Light, LightDuration) },
        { AttackId.Light1, (Light1, Light1Duration) },
        // Rhythm anchors: state names must match the Player Animator controller.
        { AttackId.Light2, (KickPause, KickPauseDuration) },
        { AttackId.Light3, (Light3, Light3Duration) },
        { AttackId.Light4, (Light4, Light4Duration) },
        { AttackId.Light5, (PunchPause, PunchPauseDuration) },
        { AttackId.LightFinisher, (LightFinisher, LightFinisherDuration) },
        { AttackId.Heavy, (Heavy, HeavyDuration) },
        { AttackId.Heavy1, (PunchPause2, PunchPause2Duration) },
        { AttackId.Heavy2, (Heavy2, Heavy2Duration) },
        { AttackId.Heavy3, (Heavy3, Heavy3Duration) },
        { AttackId.HeavyFinisher, (HeavyFinisher, HeavyFinisherDuration) },
        { AttackId.LightBreakKick, (KickPause, KickPauseDuration) },
        { AttackId.LightHeavyFinisher, (PunchPause, PunchPauseDuration) },
        { AttackId.HeavySweepKick, (KickPause, KickPauseDuration) },
        { AttackId.HeavyLightEnder, (Light, LightDuration) },
        { AttackId.Dash, (Dash, DashDuration) },
        { AttackId.DashLight, (DashLight, DashLightDuration) },
        { AttackId.DashLight1, (DashLight1, DashLight1Duration) },
        { AttackId.DashLight2, (DashLight2, DashLight2Duration) },
        { AttackId.DashLightFinisher, (DashLightFinisher, DashLightFinisherDuration) },
        { AttackId.DashHeavy, (DashHeavy, DashHeavyDuration) },
        { AttackId.DashHeavy1, (DashHeavy1, DashHeavy1Duration) },
        { AttackId.DashHeavy2, (DashHeavy2, DashHeavy2Duration) },
        { AttackId.DashHeavyFinisher, (DashHeavyFinisher, DashHeavyFinisherDuration) },
    };

    public static bool TryGetClip(AttackId attackId, out string name, out float duration)
    {
        if (ClipsByAttackId.TryGetValue(attackId, out var clip))
        {
            name = clip.Name;
            duration = clip.Duration;
            return true;
        }

        name = string.Empty;
        duration = 0f;
        Debug.LogWarning($"No animation clip mapping for AttackId.{attackId}");
        return false;
    }
}
