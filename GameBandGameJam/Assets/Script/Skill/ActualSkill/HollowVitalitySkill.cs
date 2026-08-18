#nullable enable
using UnityEngine;

public class HollowVitalitySkill : BaseSkill
{
    public override string SkillName => "Hollow Vitality";
    public override string SkillDescription => "Restore health to full, but max stamina is halved.";

    public override void ApplySkill()
    {
        player.RestoreHealthToMax();
        player.Stamina.MultiplyMaxStamina(0.5f);
    }
}
