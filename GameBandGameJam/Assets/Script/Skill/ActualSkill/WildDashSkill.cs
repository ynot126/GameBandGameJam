#nullable enable
using UnityEngine;

public class WildDashSkill : BaseSkill
{
    public override string SkillName => "Wild Dash";
    public override string SkillDescription => "Dash twice as far, but movement axes are inverted.";

    public override void ApplySkill()
    {
        player.MultiplyDashDistance(2f);
        player.InvertMovementAxes();
    }
}
