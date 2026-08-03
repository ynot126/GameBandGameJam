using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int maxHealth = 100;
    public int strength = 1;
    public int speed = 5;
    public List<SkillType> skills = new List<SkillType>();
}