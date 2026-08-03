using UnityEngine;

public class SkillLibrary: Singleton<SkillLibrary>
{
    readonly EnumDictionary<SkillType , BaseSkill> skillFactory = new EnumDictionary<SkillType , BaseSkill>();

    void Start()
    {
        skillFactory[SkillType.DoubleDamageToken] = new DoubleDamageTokenSkill();
    }

    public BaseSkill GetSkill(SkillType skillType)
    {
        return skillFactory[skillType];
    }
}
