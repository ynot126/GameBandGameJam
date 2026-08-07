#nullable enable
using System;

[Serializable]
public class ComboRecipe
{
    public string name = string.Empty;
    public AttackInputType[] sequence = Array.Empty<AttackInputType>();
    public AttackId attackId = AttackId.None;
}
