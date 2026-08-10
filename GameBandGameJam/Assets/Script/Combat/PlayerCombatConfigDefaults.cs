#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Builds the default combo table for <see cref="PlayerCombatConfig"/>.
/// Editor-only population; runtime reads the ScriptableObject asset.
/// Hitbox / cancel frame windows are authored on Animation Events, not here.
/// Animator state names live in <see cref="PlayerAnimationClips"/>.
/// </summary>
public static class PlayerCombatConfigDefaults
{
    const float NormalComboWindow = 0.5f;
    const float AnchorComboWindow = 0.7f;
    const int LightBaseDamage = 10;
    const int HeavyBaseDamage = 18;

    public static void ApplyTo(PlayerCombatConfig config)
    {
        config.defaultComboResetWindow = NormalComboWindow;
        config.simultaneousInputWindow = 0.05f;
        config.consecutiveInvalidThreshold = 2;
        config.combos = BuildCombos();
    }

    public static EnumDictionary<ComboType, ComboData> BuildCombos()
    {
        var combos = new EnumDictionary<ComboType, ComboData>();

        SetCombo(combos, ComboType.Light, "Light", Repeat(AttackInputType.Light, 1),
            BuildLightAttack(step: 0, anchor: LightAnchorKind.None));
        SetCombo(combos, ComboType.Light1, "Light1", Repeat(AttackInputType.Light, 2),
            BuildLightAttack(step: 1, anchor: LightAnchorKind.None));
        SetCombo(combos, ComboType.Light2, "Light2", Repeat(AttackInputType.Light, 3),
            BuildLightAttack(step: 2, anchor: LightAnchorKind.First));
        SetCombo(combos, ComboType.Light3, "Light3", Repeat(AttackInputType.Light, 4),
            BuildLightAttack(step: 3, anchor: LightAnchorKind.None));
        SetCombo(combos, ComboType.Light4, "Light4", Repeat(AttackInputType.Light, 5),
            BuildLightAttack(step: 4, anchor: LightAnchorKind.None));
        SetCombo(combos, ComboType.Light5, "Light5", Repeat(AttackInputType.Light, 6),
            BuildLightAttack(step: 5, anchor: LightAnchorKind.Big));
        SetCombo(combos, ComboType.LightFinisher, "Light Finisher", Repeat(AttackInputType.Light, 7),
            BuildLightAttack(step: 6, anchor: LightAnchorKind.None, isFinisher: true));

        SetCombo(combos, ComboType.Heavy, "Heavy", Repeat(AttackInputType.Heavy, 1),
            BuildHeavyAttack(step: 0, anchor: HeavyAnchorKind.None));
        SetCombo(combos, ComboType.Heavy1, "Heavy1", Repeat(AttackInputType.Heavy, 2),
            BuildHeavyAttack(step: 1, anchor: HeavyAnchorKind.First));
        SetCombo(combos, ComboType.Heavy2, "Heavy2", Repeat(AttackInputType.Heavy, 3),
            BuildHeavyAttack(step: 2, anchor: HeavyAnchorKind.None));
        SetCombo(combos, ComboType.Heavy3, "Heavy3", Repeat(AttackInputType.Heavy, 4),
            BuildHeavyAttack(step: 3, anchor: HeavyAnchorKind.Big));
        SetCombo(combos, ComboType.HeavyFinisher, "Heavy Finisher", Repeat(AttackInputType.Heavy, 5),
            BuildHeavyAttack(step: 4, anchor: HeavyAnchorKind.None, isFinisher: true));

        SetCombo(combos, ComboType.LightBreakKick, "Light Break Kick",
            new[] { AttackInputType.Light, AttackInputType.Light, AttackInputType.Light, AttackInputType.Heavy },
            BuildLightBreakKick());
        SetCombo(combos, ComboType.LightHeavyFinisher, "Light Heavy Finisher",
            new[]
            {
                AttackInputType.Light, AttackInputType.Light, AttackInputType.Light,
                AttackInputType.Light, AttackInputType.Light, AttackInputType.Light, AttackInputType.Heavy
            },
            BuildLightHeavyFinisher());
        SetCombo(combos, ComboType.HeavySweepKick, "Heavy Sweep Kick",
            new[] { AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Light },
            BuildHeavySweepKick());
        SetCombo(combos, ComboType.HeavyLightEnder, "Heavy Light Ender",
            new[]
            {
                AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Heavy,
                AttackInputType.Heavy, AttackInputType.Light
            },
            BuildHeavyLightEnder());

        SetCombo(combos, ComboType.Dash, "Dash", new[] { AttackInputType.Dash }, BuildDashAttack());

        SetCombo(combos, ComboType.DashLight, "Dash Light", DashThen(AttackInputType.Light, 1),
            BuildDashChainAttack(LightBaseDamage, step: 0, isHeavy: false, isFinisher: false));
        SetCombo(combos, ComboType.DashLight1, "Dash Light1", DashThen(AttackInputType.Light, 2),
            BuildDashChainAttack(LightBaseDamage, step: 1, isHeavy: false, isFinisher: false));
        SetCombo(combos, ComboType.DashLight2, "Dash Light2", DashThen(AttackInputType.Light, 3),
            BuildDashChainAttack(LightBaseDamage, step: 2, isHeavy: false, isFinisher: false));
        SetCombo(combos, ComboType.DashLightFinisher, "Dash Light Finisher", DashThen(AttackInputType.Light, 4),
            BuildDashChainAttack(LightBaseDamage, step: 3, isHeavy: false, isFinisher: true));

        SetCombo(combos, ComboType.DashHeavy, "Dash Heavy", DashThen(AttackInputType.Heavy, 1),
            BuildDashChainAttack(HeavyBaseDamage, step: 0, isHeavy: true, isFinisher: false));
        SetCombo(combos, ComboType.DashHeavy1, "Dash Heavy1", DashThen(AttackInputType.Heavy, 2),
            BuildDashChainAttack(HeavyBaseDamage, step: 1, isHeavy: true, isFinisher: false));
        SetCombo(combos, ComboType.DashHeavy2, "Dash Heavy2", DashThen(AttackInputType.Heavy, 3),
            BuildDashChainAttack(HeavyBaseDamage, step: 2, isHeavy: true, isFinisher: false));
        SetCombo(combos, ComboType.DashHeavyFinisher, "Dash Heavy Finisher", DashThen(AttackInputType.Heavy, 4),
            BuildDashChainAttack(HeavyBaseDamage, step: 3, isHeavy: true, isFinisher: true));

        return combos;
    }

    enum LightAnchorKind
    {
        None,
        First,
        Big
    }

    enum HeavyAnchorKind
    {
        None,
        First,
        Big
    }

    static ComboData BuildLightAttack(
        int step,
        LightAnchorKind anchor,
        bool isFinisher = false)
    {
        var damageMultiplier = 1f + step * 0.05f;
        if (anchor == LightAnchorKind.First)
        {
            damageMultiplier = 1.25f;
        }
        else if (anchor == LightAnchorKind.Big)
        {
            damageMultiplier = 1.4f;
        }

        var damage = Mathf.Max(1, (int)Math.Round(LightBaseDamage * damageMultiplier, MidpointRounding.AwayFromZero));
        var hitStun = 0.12f + step * 0.02f;
        if (anchor == LightAnchorKind.First)
        {
            hitStun += 0.04f;
        }
        else if (anchor == LightAnchorKind.Big)
        {
            hitStun += 0.06f;
        }

        var launch = 0.75f + step * 0.15f;
        var dashDuration = 0.07f;
        var recovery = isFinisher ? 1f : 0.55f;
        var comboWindow = NormalComboWindow;

        if (anchor == LightAnchorKind.First)
        {
            dashDuration *= 1.15f;
            recovery += 0.15f;
            comboWindow = AnchorComboWindow;
        }
        else if (anchor == LightAnchorKind.Big)
        {
            dashDuration *= 1.2f;
            recovery += 0.2f;
            comboWindow = AnchorComboWindow;
        }

        return new ComboData
        {
            dashDistance = 0.5f,
            dashDuration = dashDuration,
            recoveryHoldDuration = recovery,
            attackLockoutDuration = isFinisher ? 0.65f : 0f,
            comboInputWindow = isFinisher ? NormalComboWindow : comboWindow,
            hitboxRadius = isFinisher ? 1.5f : 1f,
            hitboxLocalOffset = isFinisher
                ? new Vector3(0f, 0.8f, 1.1f)
                : new Vector3(0f, 0.8f, 0.7f),
            triggersChaseSequence = isFinisher,
            payload = new HitPayloadData
            {
                damage = damage,
                hitStunDuration = isFinisher ? Mathf.Max(hitStun, 0.35f) : hitStun,
                knockbackType = isFinisher ? KnockbackType.KnockbackToDistance : KnockbackType.Standard,
                launchDistance = isFinisher ? 6f : launch
            }
        };
    }

    static ComboData BuildHeavyAttack(
        int step,
        HeavyAnchorKind anchor,
        bool isFinisher = false)
    {
        var damageMultiplier = 1f + step * 0.05f;
        var damage = Mathf.Max(1, (int)Math.Round(HeavyBaseDamage * damageMultiplier, MidpointRounding.AwayFromZero));
        var hitStun = 0.2f + step * 0.02f;
        var launch = 1.5f + step * 0.15f;
        var dashDuration = 0.1f;
        var recovery = isFinisher ? 1f : 0.55f;
        var comboWindow = NormalComboWindow;

        if (anchor == HeavyAnchorKind.First)
        {
            recovery += 0.1f;
            comboWindow = AnchorComboWindow;
        }
        else if (anchor == HeavyAnchorKind.Big)
        {
            dashDuration *= 1.1f;
            recovery += 0.15f;
            comboWindow = AnchorComboWindow;
        }

        return new ComboData
        {
            dashDistance = 2f,
            dashDuration = dashDuration,
            recoveryHoldDuration = recovery,
            attackLockoutDuration = isFinisher ? 0.65f : 0f,
            comboInputWindow = comboWindow,
            hitboxRadius = isFinisher ? 1.5f : 1f,
            hitboxLocalOffset = isFinisher
                ? new Vector3(0f, 0.8f, 1.1f)
                : new Vector3(0f, 0.8f, 0.3f),
            triggersChaseSequence = isFinisher,
            payload = new HitPayloadData
            {
                damage = damage,
                hitStunDuration = isFinisher ? Mathf.Max(hitStun, 0.35f) : hitStun,
                knockbackType = isFinisher ? KnockbackType.KnockbackToDistance : KnockbackType.Standard,
                launchDistance = isFinisher ? 6f : launch
            }
        };
    }

    static ComboData BuildLightBreakKick()
    {
        return new ComboData
        {
            dashDistance = 0.75f,
            dashDuration = 0.09f,
            recoveryHoldDuration = 1f,
            attackLockoutDuration = 0.65f,
            comboInputWindow = NormalComboWindow,
            hitboxRadius = 1f,
            hitboxLocalOffset = new Vector3(0f, 0.75f, 0.9f),
            triggersChaseSequence = true,
            payload = new HitPayloadData
            {
                damage = 22,
                hitStunDuration = 0.35f,
                knockbackType = KnockbackType.KnockbackToDistance,
                launchDistance = 5f
            }
        };
    }

    static ComboData BuildLightHeavyFinisher()
    {
        return new ComboData
        {
            dashDistance = 1f,
            dashDuration = 0.1f,
            recoveryHoldDuration = 1f,
            attackLockoutDuration = 0.65f,
            comboInputWindow = NormalComboWindow,
            hitboxRadius = 1.5f,
            hitboxLocalOffset = new Vector3(0f, 0.85f, 1.15f),
            triggersChaseSequence = true,
            payload = new HitPayloadData
            {
                damage = 18,
                hitStunDuration = 0.4f,
                knockbackType = KnockbackType.KnockbackToDistance,
                launchDistance = 6.5f
            }
        };
    }

    static ComboData BuildHeavySweepKick()
    {
        return new ComboData
        {
            dashDistance = 1.5f,
            dashDuration = 0.09f,
            recoveryHoldDuration = 1f,
            attackLockoutDuration = 0.65f,
            comboInputWindow = NormalComboWindow,
            hitboxRadius = 1f,
            hitboxLocalOffset = new Vector3(0f, 0.5f, 1f),
            triggersChaseSequence = true,
            payload = new HitPayloadData
            {
                damage = 24,
                hitStunDuration = 0.38f,
                knockbackType = KnockbackType.KnockbackToDistance,
                launchDistance = 5.5f
            }
        };
    }

    static ComboData BuildHeavyLightEnder()
    {
        return new ComboData
        {
            dashDistance = 0.6f,
            dashDuration = 0.06f,
            recoveryHoldDuration = 0.3f,
            attackLockoutDuration = 0f,
            comboInputWindow = NormalComboWindow,
            hitboxRadius = 1f,
            hitboxLocalOffset = new Vector3(0f, 0.8f, 0.75f),
            triggersChaseSequence = false,
            payload = new HitPayloadData
            {
                damage = 12,
                hitStunDuration = 0.18f,
                knockbackType = KnockbackType.Standard,
                launchDistance = 1.2f
            }
        };
    }

    static ComboData BuildDashAttack()
    {
        return new ComboData
        {
            dashDistance = 3f,
            dashDuration = 0.12f,
            recoveryHoldDuration = 0.35f,
            comboInputWindow = NormalComboWindow,
            triggersChaseSequence = false,
            useMoveInputDirection = true,
            skipHitbox = true,
            payload = new HitPayloadData
            {
                damage = 0,
                hitStunDuration = 0f,
                knockbackType = KnockbackType.Standard,
                launchDistance = 0f
            }
        };
    }

    static ComboData BuildDashChainAttack(
        int baseDamage,
        int step,
        bool isHeavy,
        bool isFinisher)
    {
        var damageMultiplier = 1f + step * 0.05f;
        var damage = Mathf.Max(1, (int)Math.Round(baseDamage * damageMultiplier, MidpointRounding.AwayFromZero));
        var hitStun = (isHeavy ? 0.2f : 0.12f) + step * 0.02f;
        var launch = (isHeavy ? 1.5f : 0.75f) + step * 0.15f;

        return new ComboData
        {
            dashDistance = isHeavy ? 2f : 0.5f,
            dashDuration = isHeavy ? 0.1f : 0.07f,
            recoveryHoldDuration = isFinisher ? 1f : 0.55f,
            attackLockoutDuration = isFinisher ? 0.65f : 0f,
            comboInputWindow = NormalComboWindow,
            hitboxRadius = isFinisher ? 1.5f : 1f,
            hitboxLocalOffset = isFinisher
                ? new Vector3(0f, 0.8f, 1.1f)
                : isHeavy
                    ? new Vector3(0f, 0.8f, 0.3f)
                    : new Vector3(0f, 0.8f, 0.7f),
            triggersChaseSequence = isFinisher,
            payload = new HitPayloadData
            {
                damage = damage,
                hitStunDuration = isFinisher ? Mathf.Max(hitStun, 0.35f) : hitStun,
                knockbackType = isFinisher ? KnockbackType.KnockbackToDistance : KnockbackType.Standard,
                launchDistance = isFinisher ? 6f : launch
            }
        };
    }

    static void SetCombo(
        EnumDictionary<ComboType, ComboData> combos,
        ComboType comboType,
        string name,
        AttackInputType[] sequence,
        ComboData data)
    {
        data.name = name;
        data.sequence = sequence;
        combos[comboType] = data;
    }

    static AttackInputType[] Repeat(AttackInputType input, int count)
    {
        var sequence = new AttackInputType[count];
        for (var i = 0; i < count; i++)
        {
            sequence[i] = input;
        }

        return sequence;
    }

    static AttackInputType[] DashThen(AttackInputType input, int count)
    {
        var sequence = new AttackInputType[count + 1];
        sequence[0] = AttackInputType.Dash;
        for (var i = 0; i < count; i++)
        {
            sequence[i + 1] = input;
        }

        return sequence;
    }
}
