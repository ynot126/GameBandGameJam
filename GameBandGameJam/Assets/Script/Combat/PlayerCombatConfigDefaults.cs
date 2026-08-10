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

    public static EnumDictionary<ComboType, ComboRecipe> BuildRecipes()
    {
        var recipes = new EnumDictionary<ComboType, ComboRecipe>();
        SetRecipe(recipes, ComboType.Light, "Light", Repeat(AttackInputType.Light, 1));
        SetRecipe(recipes, ComboType.Light1, "Light1", Repeat(AttackInputType.Light, 2));
        SetRecipe(recipes, ComboType.Light2, "Light2", Repeat(AttackInputType.Light, 3));
        SetRecipe(recipes, ComboType.Light3, "Light3", Repeat(AttackInputType.Light, 4));
        SetRecipe(recipes, ComboType.Light4, "Light4", Repeat(AttackInputType.Light, 5));
        SetRecipe(recipes, ComboType.Light5, "Light5", Repeat(AttackInputType.Light, 6));
        SetRecipe(recipes, ComboType.LightFinisher, "Light Finisher", Repeat(AttackInputType.Light, 7));

        SetRecipe(recipes, ComboType.Heavy, "Heavy", Repeat(AttackInputType.Heavy, 1));
        SetRecipe(recipes, ComboType.Heavy1, "Heavy1", Repeat(AttackInputType.Heavy, 2));
        SetRecipe(recipes, ComboType.Heavy2, "Heavy2", Repeat(AttackInputType.Heavy, 3));
        SetRecipe(recipes, ComboType.Heavy3, "Heavy3", Repeat(AttackInputType.Heavy, 4));
        SetRecipe(recipes, ComboType.HeavyFinisher, "Heavy Finisher", Repeat(AttackInputType.Heavy, 5));

        SetRecipe(recipes, ComboType.LightBreakKick, "Light Break Kick",
            AttackInputType.Light, AttackInputType.Light, AttackInputType.Light, AttackInputType.Heavy);
        SetRecipe(recipes, ComboType.LightHeavyFinisher, "Light Heavy Finisher",
            AttackInputType.Light, AttackInputType.Light, AttackInputType.Light,
            AttackInputType.Light, AttackInputType.Light, AttackInputType.Light, AttackInputType.Heavy);
        SetRecipe(recipes, ComboType.HeavySweepKick, "Heavy Sweep Kick",
            AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Light);
        SetRecipe(recipes, ComboType.HeavyLightEnder, "Heavy Light Ender",
            AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Heavy, AttackInputType.Light);

        SetRecipe(recipes, ComboType.Dash, "Dash", AttackInputType.Dash);

        SetRecipe(recipes, ComboType.DashLight, "Dash Light", DashThen(AttackInputType.Light, 1));
        SetRecipe(recipes, ComboType.DashLight1, "Dash Light1", DashThen(AttackInputType.Light, 2));
        SetRecipe(recipes, ComboType.DashLight2, "Dash Light2", DashThen(AttackInputType.Light, 3));
        SetRecipe(recipes, ComboType.DashLightFinisher, "Dash Light Finisher", DashThen(AttackInputType.Light, 4));

        SetRecipe(recipes, ComboType.DashHeavy, "Dash Heavy", DashThen(AttackInputType.Heavy, 1));
        SetRecipe(recipes, ComboType.DashHeavy1, "Dash Heavy1", DashThen(AttackInputType.Heavy, 2));
        SetRecipe(recipes, ComboType.DashHeavy2, "Dash Heavy2", DashThen(AttackInputType.Heavy, 3));
        SetRecipe(recipes, ComboType.DashHeavyFinisher, "Dash Heavy Finisher", DashThen(AttackInputType.Heavy, 4));
        return recipes;
    }

    public static AttackDefinition[] BuildAttacks()
    {
        return new[]
        {
            BuildLightAttack(ComboType.Light, step: 0, anchor: LightAnchorKind.None),
            BuildLightAttack(ComboType.Light1, step: 1, anchor: LightAnchorKind.None),
            BuildLightAttack(ComboType.Light2, step: 2, anchor: LightAnchorKind.First),
            BuildLightAttack(ComboType.Light3, step: 3, anchor: LightAnchorKind.None),
            BuildLightAttack(ComboType.Light4, step: 4, anchor: LightAnchorKind.None),
            BuildLightAttack(ComboType.Light5, step: 5, anchor: LightAnchorKind.Big),
            BuildLightAttack(ComboType.LightFinisher, step: 6, anchor: LightAnchorKind.None, isFinisher: true),

            BuildHeavyAttack(ComboType.Heavy, step: 0, anchor: HeavyAnchorKind.None),
            BuildHeavyAttack(ComboType.Heavy1, step: 1, anchor: HeavyAnchorKind.First),
            BuildHeavyAttack(ComboType.Heavy2, step: 2, anchor: HeavyAnchorKind.None),
            BuildHeavyAttack(ComboType.Heavy3, step: 3, anchor: HeavyAnchorKind.Big),
            BuildHeavyAttack(ComboType.HeavyFinisher, step: 4, anchor: HeavyAnchorKind.None, isFinisher: true),

            BuildLightBreakKick(),
            BuildLightHeavyFinisher(),
            BuildHeavySweepKick(),
            BuildHeavyLightEnder(),

            BuildDashAttack(),

            BuildDashChainAttack(ComboType.DashLight, LightBaseDamage, step: 0, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(ComboType.DashLight1, LightBaseDamage, step: 1, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(ComboType.DashLight2, LightBaseDamage, step: 2, isHeavy: false, isFinisher: false),
            BuildDashChainAttack(ComboType.DashLightFinisher, LightBaseDamage, step: 3, isHeavy: false, isFinisher: true),

            BuildDashChainAttack(ComboType.DashHeavy, HeavyBaseDamage, step: 0, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(ComboType.DashHeavy1, HeavyBaseDamage, step: 1, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(ComboType.DashHeavy2, HeavyBaseDamage, step: 2, isHeavy: true, isFinisher: false),
            BuildDashChainAttack(ComboType.DashHeavyFinisher, HeavyBaseDamage, step: 3, isHeavy: true, isFinisher: true)
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
        ComboType comboType,
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
            comboType = comboType,
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
        ComboType comboType,
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
            comboType = comboType,
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
            comboType = ComboType.LightBreakKick,
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
            comboType = ComboType.LightHeavyFinisher,
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
            comboType = ComboType.HeavySweepKick,
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
            comboType = ComboType.HeavyLightEnder,
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
            comboType = ComboType.Dash,
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
        ComboType comboType,
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
            comboType = comboType,
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

    static void SetRecipe(
        EnumDictionary<ComboType, ComboRecipe> recipes,
        ComboType comboType,
        string name,
        params AttackInputType[] sequence)
    {
        recipes[comboType] = new ComboRecipe
        {
            name = name,
            sequence = sequence
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
        sequence[0] = AttackInputType.Dash;
        for (var i = 0; i < count; i++)
        {
            sequence[i + 1] = input;
        }

        return sequence;
    }
}
