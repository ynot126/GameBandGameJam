#nullable enable
using UnityEngine;

public class ColossusSkill : BaseSkill
{
    public override string SkillName => "Colossus";
    public override string SkillDescription => "Double max health, but move at half speed.";

    public override void ApplySkill()
    {
        player.MultiplyMaxHealth(2f);
        player.MultiplySpeed(0.5f);
    }
}
