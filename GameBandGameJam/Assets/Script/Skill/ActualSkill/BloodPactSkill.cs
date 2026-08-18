#nullable enable
using UnityEngine;

public class BloodPactSkill : BaseSkill
{
    public override string SkillName => "Blood Pact";
    public override string SkillDescription => "Deal double damage, but current health is halved.";

    public override void ApplySkill()
    {
        var missingHealthRatio = 1f - (float)player.CurrentHealth / player.MaxHealth;
        player.ScaleCurrentHealth(0.5f);
        player.MultiplyDamage(2f + missingHealthRatio);
    }
}
