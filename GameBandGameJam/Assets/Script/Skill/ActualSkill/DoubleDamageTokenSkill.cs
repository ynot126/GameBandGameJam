using UnityEngine;

public class DoubleDamageTokenSkill : BaseSkill
{
    public override string SkillName=> "Fragile";
    public override string SkillDescription => "Player will take double amount of damage";

    public override void ApplySkill()
    {
        player.DoubleTakenDamage();
    }
}
