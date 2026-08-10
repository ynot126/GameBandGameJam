#nullable enable
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCombatConfig", menuName = "Configs/PlayerCombatConfig")]
public class PlayerCombatConfig : ScriptableObject
{
    public float defaultComboResetWindow = 0.5f;
    public float simultaneousInputWindow = 0.05f;
    public int consecutiveInvalidThreshold = 2;
    public EnumDictionary<ComboType, ComboRecipe> recipes = new();
    public AttackDefinition[] attacks = System.Array.Empty<AttackDefinition>();

    public bool HasAuthoredRecipes()
    {
        foreach (var pair in recipes)
        {
            if (pair.Key == ComboType.None)
            {
                continue;
            }

            if (pair.Value?.sequence is { Length: > 0 })
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!HasAuthoredRecipes() && (attacks == null || attacks.Length == 0))
        {
            PlayerCombatConfigDefaults.ApplyTo(this);
        }
    }

    [ContextMenu("Apply Default Combat Data")]
    void ApplyDefaultsInEditor()
    {
        PlayerCombatConfigDefaults.ApplyTo(this);
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
