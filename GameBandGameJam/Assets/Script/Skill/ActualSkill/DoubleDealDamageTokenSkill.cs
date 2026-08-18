using UnityEngine;

public class DoubleDealDamageTokenSkill : BaseSkill
{
    public override string SkillName=> "Fragile";
    public override string SkillDescription => "Player will take double amount of damage";

    public override void ApplySkill()
    {
        player.DoubleTakenDamage();
        player.MultiplyDamage(2);
    }
}
