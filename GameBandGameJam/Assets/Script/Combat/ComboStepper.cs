#nullable enable

/// <summary>
/// Cancel-chain combo state: spam the same button to advance Light → Light1 → Light2…
/// Dash branches into DashLight / DashHeavy on the follow-up button.
/// </summary>
public sealed class ComboStepper
{
    public enum Branch
    {
        None = 0,
        Light,
        Heavy,
        Dash,
        DashLight,
        DashHeavy
    }

    static readonly AttackId[] LightChain =
    {
        AttackId.Light,
        AttackId.Light1,
        AttackId.Light2,
        AttackId.Light3,
        AttackId.Light4,
        AttackId.Light5,
        AttackId.LightFinisher
    };

    static readonly AttackId[] HeavyChain =
    {
        AttackId.Heavy,
        AttackId.Heavy1,
        AttackId.Heavy2,
        AttackId.Heavy3,
        AttackId.Heavy4,
        AttackId.Heavy5,
        AttackId.HeavyFinisher
    };

    static readonly AttackId[] DashLightChain =
    {
        AttackId.DashLight,
        AttackId.DashLight1,
        AttackId.DashLight2,
        AttackId.DashLightFinisher
    };

    static readonly AttackId[] DashHeavyChain =
    {
        AttackId.DashHeavy,
        AttackId.DashHeavy1,
        AttackId.DashHeavy2,
        AttackId.DashHeavyFinisher
    };

    Branch branch = Branch.None;
    int stepIndex = -1;

    public Branch ActiveBranch => branch;
    public int StepIndex => stepIndex;

    public void Reset()
    {
        branch = Branch.None;
        stepIndex = -1;
    }

    public bool TryResolve(AttackInputType input, out AttackId attackId)
    {
        attackId = AttackId.None;

        switch (branch)
        {
            case Branch.None:
                return TryStart(input, out attackId);

            case Branch.Light:
                return TryAdvanceOrRestart(input, AttackInputType.L, Branch.Light, LightChain, out attackId);

            case Branch.Heavy:
                return TryAdvanceOrRestart(input, AttackInputType.H, Branch.Heavy, HeavyChain, out attackId);

            case Branch.Dash:
                if (input == AttackInputType.L)
                {
                    branch = Branch.DashLight;
                    stepIndex = 0;
                    attackId = DashLightChain[0];
                    return true;
                }

                if (input == AttackInputType.H)
                {
                    branch = Branch.DashHeavy;
                    stepIndex = 0;
                    attackId = DashHeavyChain[0];
                    return true;
                }

                if (input == AttackInputType.D)
                {
                    stepIndex = 0;
                    attackId = AttackId.Dash;
                    return true;
                }

                Reset();
                return TryStart(input, out attackId);

            case Branch.DashLight:
                return TryAdvanceOrRestart(input, AttackInputType.L, Branch.DashLight, DashLightChain, out attackId);

            case Branch.DashHeavy:
                return TryAdvanceOrRestart(input, AttackInputType.H, Branch.DashHeavy, DashHeavyChain, out attackId);

            default:
                Reset();
                return TryStart(input, out attackId);
        }
    }

    bool TryStart(AttackInputType input, out AttackId attackId)
    {
        attackId = AttackId.None;
        switch (input)
        {
            case AttackInputType.L:
                branch = Branch.Light;
                stepIndex = 0;
                attackId = LightChain[0];
                return true;
            case AttackInputType.H:
                branch = Branch.Heavy;
                stepIndex = 0;
                attackId = HeavyChain[0];
                return true;
            case AttackInputType.D:
                branch = Branch.Dash;
                stepIndex = 0;
                attackId = AttackId.Dash;
                return true;
            default:
                return false;
        }
    }

    bool TryAdvanceOrRestart(
        AttackInputType input,
        AttackInputType continueInput,
        Branch continueBranch,
        AttackId[] chain,
        out AttackId attackId)
    {
        if (input == continueInput)
        {
            var next = stepIndex + 1;
            if (next < chain.Length)
            {
                branch = continueBranch;
                stepIndex = next;
                attackId = chain[stepIndex];
                return true;
            }

            // Chain finished — start a fresh string on the same button.
            Reset();
            return TryStart(input, out attackId);
        }

        Reset();
        return TryStart(input, out attackId);
    }
}
