#nullable enable
using System;
using UnityEngine;

/// <summary>
/// Builds the default recipe and attack tables for <see cref="PlayerCombatConfig"/>.
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
        config.recipes = BuildRecipes();
        config.attacks = BuildAttacks();
    }

    public static ComboRecipe[] BuildRecipes()
    {
        return new[]
        {
            CreateRecipe("Light", AttackId.Light, Repeat(AttackInputType.L, 1)),
            CreateRecipe("Light1", AttackId.Light1, Repeat(AttackInputType.L, 2)),
            CreateRecipe("Light2", AttackId.Light2, Repeat(AttackInputType.L, 3)),
            CreateRecipe("Light3", AttackId.Light3, Repeat(AttackInputType.L, 4)),
            CreateRecipe("Light4", AttackId.Light4, Repeat(AttackInputType.L, 5)),
            CreateRecipe("Light5", AttackId.Light5, Repeat(AttackInputType.L, 6)),
            CreateRecipe("Light Finisher", AttackId.LightFinisher, Repeat(AttackInputType.L, 7)),

            CreateRecipe("Heavy", AttackId.Heavy, Repeat(AttackInputType.H, 1)),
            CreateRecipe("Heavy1", AttackId.Heavy1, Repeat(AttackInputType.H, 2)),
            CreateRecipe("Heavy2", AttackId.Heavy2, Repeat(AttackInputType.H, 3)),
            CreateRecipe("Heavy3", AttackId.Heavy3, Repeat(AttackInputType.H, 4)),
            CreateRecipe("Heavy Finisher", AttackId.HeavyFinisher, Repeat(AttackInputType.H, 5)),

            CreateRecipe("Light Break Kick", AttackId.LightBreakKick,
                AttackInputType.L, AttackInputType.L, AttackInputType.L, AttackInputType.H),
            CreateRecipe("Light Heavy Finisher", AttackId.LightHeavyFinisher,
                AttackInputType.L, AttackInputType.L, AttackInputType.L,
                AttackInputType.L, AttackInputType.L, AttackInputType.L, AttackInputType.H),
            CreateRecipe("Heavy Sweep Kick", AttackId.HeavySweepKick,
                AttackInputType.H, AttackInputType.H, AttackInputType.L),
            CreateRecipe("Heavy Light Ender", AttackId.HeavyLightEnder,
                AttackInputType.H, AttackInputType.H, AttackInputType.H, AttackInputType.H, AttackInputType.L),

            CreateRecipe("Dash", AttackId.Dash, AttackInputType.D),

            CreateRecipe("Dash Light", AttackId.DashLight, DashThen(AttackInputType.L, 1)),
            CreateRecipe("Dash Light1", AttackId.DashLight1, DashThen(AttackInputType.L, 2)),
            CreateRecipe("Dash Light2", AttackId.DashLight2, DashThen(AttackInputType.L, 3)),
            CreateRecipe("Dash Light Finisher", AttackId.DashLightFinisher, DashThen(AttackInputType.L, 4)),

            CreateRecipe("Dash Heavy", AttackId.DashHeavy, DashThen(AttackInputType.H, 1)),
            CreateRecipe("Dash Heavy1", AttackId.DashHeavy1, DashThen(AttackInputType.H, 2)),
            CreateRecipe("Dash Heavy2", AttackId.DashHeavy2, DashThen(AttackInputType.H, 3)),
            CreateRecipe("Dash Heavy Finisher", AttackId.DashHeavyFinisher, DashThen(AttackInputType.H, 4))
        };
    }

    public static AttackDefinition[] BuildAttacks()
    {
        return new[]
        {
            BuildLightAttack(AttackId.Light, step: 0, anchor: LightAnchorKind.None),
            BuildLightAttack(AttackId.Light1, step: 1, anchor: LightAnchorKind.None),
            BuildLightAttack(AttackId.Light2, step: 2, anchor: LightAnchorKind.First),
            BuildLightAttack(AttackId.Light3, step: 3, anchor: LightAnchorKind.None),
            BuildLightAttack(AttackId.Light4, step: 4, anchor: LightAnchorKind.None),
            BuildLightAttack(AttackId.Light5, step: 5, anchor: LightAnchorKind.Big),
            BuildLightAttack(AttackId.LightFinisher, step: 6, anchor: LightAnchorKind.None, isFinisher: true),

            BuildHeavyAttack(AttackId.Heavy, step: 0, anchor: HeavyAnchorKind.None),
            BuildHeavyAttack(AttackId.Heavy1, step: 1, anchor: HeavyAnchorKind.First),
            BuildHeavyAttack(AttackId.Heavy2, step: 2, anchor: HeavyAnchorKind.None),
            BuildHeavyAttack(AttackId.Heavy3, step: 3, anchor: HeavyAnchorKind.Big),
            BuildHeavyAttack(AttackId.HeavyFinisher, step: 4, anchor: HeavyAnchorKind.None, isFinisher: true),

            BuildLightBreakKick(),
            BuildLightHeavyFinisher(),
            BuildHeavySweepKick(),
            BuildHeavyLightEnder(),

            BuildDashAttack(),

            BuildDashChainAttack(AttackId.DashLight, LightBaseDamage, step: 0, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(AttackId.DashLight1, LightBaseDamage, step: 1, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(AttackId.DashLight2, LightBaseDamage, step: 2, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(AttackId.DashLightFinisher, LightBaseDamage, step: 3, isHeavy: false, isFinisher: true),

            BuildDashChainAttack(AttackId.DashHeavy, HeavyBaseDamage, step: 0, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(AttackId.DashHeavy1, HeavyBaseDamage, step: 1, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(AttackId.DashHeavy2, HeavyBaseDamage, step: 2, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(AttackId.DashHeavyFinisher, HeavyBaseDamage, step: 3, isHeavy: true, isFinisher: true)
        };
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

    static AttackDefinition BuildLightAttack(
        AttackId attackId,
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

        return new AttackDefinition
        {
            attackId = attackId,
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

    static AttackDefinition BuildHeavyAttack(
        AttackId attackId,
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

        return new AttackDefinition
        {
            attackId = attackId,
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

    static AttackDefinition BuildLightBreakKick()
    {
        return new AttackDefinition
        {
            attackId = AttackId.LightBreakKick,
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

    static AttackDefinition BuildLightHeavyFinisher()
    {
        return new AttackDefinition
        {
            attackId = AttackId.LightHeavyFinisher,
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

    static AttackDefinition BuildHeavySweepKick()
    {
        return new AttackDefinition
        {
            attackId = AttackId.HeavySweepKick,
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

    static AttackDefinition BuildHeavyLightEnder()
    {
        return new AttackDefinition
        {
            attackId = AttackId.HeavyLightEnder,
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

    static AttackDefinition BuildDashAttack()
    {
        return new AttackDefinition
        {
            attackId = AttackId.Dash,
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

    static AttackDefinition BuildDashChainAttack(
        AttackId attackId,
        int baseDamage,
        int step,
        bool isHeavy,
        bool isFinisher)
    {
        var damageMultiplier = 1f + step * 0.05f;
        var damage = Mathf.Max(1, (int)Math.Round(baseDamage * damageMultiplier, MidpointRounding.AwayFromZero));
        var hitStun = (isHeavy ? 0.2f : 0.12f) + step * 0.02f;
        var launch = (isHeavy ? 1.5f : 0.75f) + step * 0.15f;

        return new AttackDefinition
        {
            attackId = attackId,
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

    static ComboRecipe CreateRecipe(string name, AttackId attackId, params AttackInputType[] sequence)
    {
        return new ComboRecipe
        {
            name = name,
            sequence = sequence,
            attackId = attackId
        };
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
        sequence[0] = AttackInputType.D;
        for (var i = 0; i < count; i++)
        {
            sequence[i + 1] = input;
        }

        return sequence;
    }
}
