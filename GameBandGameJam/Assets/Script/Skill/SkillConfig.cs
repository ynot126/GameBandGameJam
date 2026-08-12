#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "Configs/SkillConfig")]
public class SkillConfig : ScriptableObject
{
    public EnumDictionary<SkillType, BaseSkill> skills = new();

    public BaseSkill? GetSkill(SkillType skillType)
    {
        return skills.TryGetValue(skillType, out var skill) ? skill : null;
    }
}
