#nullable enable
using UnityEngine;

public class FrenzySkill : BaseSkill
{
    public override string SkillName => "Frenzy";
    public override string SkillDescription => "Attack twice as fast, but take double damage.";

    public override void ApplySkill()
    {
        player.MultiplyAttackSpeed(2f);
        player.DoubleTakenDamage();
    }
}
