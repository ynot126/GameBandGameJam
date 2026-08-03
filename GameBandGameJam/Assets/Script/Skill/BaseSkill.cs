#nullable enable
using UnityEngine;

public abstract class BaseSkill
{
    protected Player player = null!;
    public abstract string SkillName { get; }
    public abstract string SkillDescription { get; }
    public void Initialize(Player aPlayer)
    {
        player = aPlayer;
    }
    public abstract void ApplySkill();
}
